using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Constants;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Persistence;
using AIInterviewCoach.Infrastructure.Services;
using AIInterviewCoach.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIInterviewCoach.Tests.Services
{
    public class SubmissionFeedbackBackgroundServiceTests
    {
        // ── Test infrastructure ───────────────────────────────────────────────

        // Scope factory whose delegate receives the 1-based creation count, letting tests
        // return a different scope/provider for the startup query scope (call 1) vs each
        // per-submission processing scope (calls 2+).
        private sealed class ControlledScopeFactory : IServiceScopeFactory
        {
            private readonly Func<int, IServiceScope> _factory;
            private int _callCount;

            public ControlledScopeFactory(Func<int, IServiceScope> factory) => _factory = factory;

            public IServiceScope CreateScope() =>
                _factory(Interlocked.Increment(ref _callCount));
        }

        // IServiceScope whose Dispose() fires an optional callback.  Used as a
        // deterministic signal: because the background service uses `using var scope`,
        // the callback runs *after* ProcessAsync (and therefore SaveChangesAsync) return.
        private sealed class SignalingScope : IServiceScope
        {
            private readonly Action? _onDispose;

            public SignalingScope(IServiceProvider provider, Action? onDispose = null)
            {
                ServiceProvider = provider;
                _onDispose = onDispose;
            }

            public IServiceProvider ServiceProvider { get; }
            public void Dispose() => _onDispose?.Invoke();
        }

        private static SubmissionFeedbackProcessor MakeProcessor(
            AppDbContext db,
            ISubmissionFeedbackService? feedbackService = null) =>
            new(db,
                feedbackService ?? new FakeSubmissionFeedbackService(),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

        // ── Startup recovery ──────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteAsync_ProcessesPendingSubmission_RecoveredFromDatabase()
        {
            // EnqueueExistingPendingSubmissionsAsync (scope 1) queries the DB before
            // the drain loop starts and pre-fills the channel with Pending submissions.
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db, candidate.Id, problem.Id, null,
                SubmissionStatus.Accepted, passedTests: 1, totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            var processingComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var scopeFactory = new ControlledScopeFactory(callNum =>
            {
                var provider = new FakeServiceProvider(new Dictionary<Type, Func<object>>
                {
                    [typeof(IAppDbContext)] = () => db,
                    [typeof(SubmissionFeedbackProcessor)] = () => MakeProcessor(db)
                });
                // Scope 1 = startup query; scope 2 = processing scope.
                return new SignalingScope(provider, callNum == 2 ? () => processingComplete.TrySetResult(true) : null);
            });

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);

            await processingComplete.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await svc.StopAsync(CancellationToken.None);

            var stored = db.Submissions.Single(s => s.Id == submission.Id);
            Assert.Equal(SubmissionFeedbackStatuses.Ready, stored.AiFeedbackStatus);
            Assert.NotNull(stored.AiFeedback);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsEarlyWithoutDrainLoop_WhenStartupDbQueryFails()
        {
            // If IAppDbContext cannot be resolved, GetRequiredService throws and the startup
            // catch(Exception) block fires, logs a warning, and returns without entering the
            // drain loop.  Items subsequently enqueued via QueueAsync sit in the channel unread.
            var startupScopeDisposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var processorCallCount = 0;

            var scopeFactory = new ControlledScopeFactory(callNum =>
            {
                // Scope 1 = startup: empty provider → GetRequiredService<IAppDbContext> throws.
                // Any callNum > 1 would mean the drain loop started — fail fast.
                var provider = callNum == 1
                    ? new FakeServiceProvider(new Dictionary<Type, Func<object>>())
                    : new FakeServiceProvider(new Dictionary<Type, Func<object>>
                      {
                          [typeof(SubmissionFeedbackProcessor)] = () =>
                          {
                              Interlocked.Increment(ref processorCallCount);
                              throw new InvalidOperationException("Drain loop must not run after startup failure.");
                          }
                      });
                return new SignalingScope(provider,
                    callNum == 1 ? () => startupScopeDisposed.TrySetResult(true) : null);
            });

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);

            // Wait for startup scope disposal — guarantees ExecuteAsync passed its try/catch and returned.
            await startupScopeDisposed.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // These writes reach the channel but nobody reads them (drain loop never started).
            await svc.QueueAsync(Guid.NewGuid());
            await svc.QueueAsync(Guid.NewGuid());

            await svc.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, processorCallCount);
        }

        // ── Manual queue dispatch ─────────────────────────────────────────────

        [Fact]
        public async Task QueueAsync_DispatchesSubmission_ToProcessor()
        {
            // Manual QueueAsync and startup recovery both attempt to queue the same Pending
            // submission; ConcurrentDictionary deduplication ensures it is processed exactly once.
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db, candidate.Id, problem.Id, null,
                SubmissionStatus.Accepted, passedTests: 1, totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            var processingComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var processorCallCount = 0;

            var scopeFactory = new ControlledScopeFactory(callNum =>
            {
                var provider = new FakeServiceProvider(new Dictionary<Type, Func<object>>
                {
                    [typeof(IAppDbContext)] = () => db,
                    [typeof(SubmissionFeedbackProcessor)] = () =>
                    {
                        Interlocked.Increment(ref processorCallCount);
                        return MakeProcessor(db);
                    }
                });
                // Signal on any processing scope (callNum >= 2).
                return new SignalingScope(provider, callNum >= 2 ? () => processingComplete.TrySetResult(true) : null);
            });

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);
            await svc.QueueAsync(submission.Id); // deduplicated if startup already queued it

            await processingComplete.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await svc.StopAsync(CancellationToken.None);

            Assert.Equal(1, processorCallCount); // processed exactly once despite two enqueue attempts
            var stored = db.Submissions.Single(s => s.Id == submission.Id);
            Assert.Equal(SubmissionFeedbackStatuses.Ready, stored.AiFeedbackStatus);
        }

        // ── Exception resilience ──────────────────────────────────────────────

        [Fact]
        public async Task ExecuteAsync_TreatsOperationCancelledAsError_WhenStoppingTokenIsNotCancelled()
        {
            // The drain loop has: catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested).
            // When stoppingToken is NOT cancelled, the `when` guard is false, the exception falls to
            // catch(Exception), logs an error, and the loop continues to the next item.
            using var db = TestDbContextFactory.CreateContext();

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var processorResolutionCount = 0;
            var secondItemProcessed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var scopeFactory = new ControlledScopeFactory(callNum =>
            {
                var provider = new FakeServiceProvider(new Dictionary<Type, Func<object>>
                {
                    [typeof(IAppDbContext)] = () => db,
                    [typeof(SubmissionFeedbackProcessor)] = () =>
                    {
                        if (Interlocked.Increment(ref processorResolutionCount) == 1)
                            throw new OperationCanceledException("Simulated inner cancellation — not a shutdown signal.");
                        return MakeProcessor(db);
                    }
                });
                return new SignalingScope(provider, callNum == 3 ? () => secondItemProcessed.TrySetResult(true) : null);
            });

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);

            await svc.QueueAsync(id1);
            await svc.QueueAsync(id2);

            await secondItemProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await svc.StopAsync(CancellationToken.None);

            Assert.Equal(2, processorResolutionCount);
        }

        [Fact]
        public async Task ExecuteAsync_ContinuesProcessingLoop_AfterExceptionOnOneItem()
        {
            // An exception thrown while resolving SubmissionFeedbackProcessor for item 1
            // must not terminate the drain loop; item 2 must still be dispatched.
            // Using synthetic IDs (no DB rows) keeps the test DB-free for submissions
            // while still exercising the catch/continue path in ExecuteAsync.
            using var db = TestDbContextFactory.CreateContext(); // empty DB: startup query queues nothing

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var processorResolutionCount = 0;
            var secondItemProcessed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Scope 1 = startup (resolves IAppDbContext only; empty DB → nothing enqueued).
            // Scope 2 = id1 processing; factory throws on the first SubmissionFeedbackProcessor resolve.
            // Scope 3 = id2 processing; factory succeeds; TCS fires on scope dispose.
            var scopeFactory = new ControlledScopeFactory(callNum =>
            {
                var provider = new FakeServiceProvider(new Dictionary<Type, Func<object>>
                {
                    [typeof(IAppDbContext)] = () => db,
                    [typeof(SubmissionFeedbackProcessor)] = () =>
                    {
                        if (Interlocked.Increment(ref processorResolutionCount) == 1)
                            throw new InvalidOperationException("Simulated scope-resolution failure.");
                        return MakeProcessor(db); // id2: submission not found → ProcessAsync returns silently
                    }
                });
                return new SignalingScope(provider, callNum == 3 ? () => secondItemProcessed.TrySetResult(true) : null);
            });

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);

            await svc.QueueAsync(id1);
            await svc.QueueAsync(id2);

            await secondItemProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await svc.StopAsync(CancellationToken.None);

            // Factory was attempted for both items, proving the loop continued after the failure.
            Assert.Equal(2, processorResolutionCount);
        }

        // ── Clean shutdown ────────────────────────────────────────────────────

        [Fact]
        public async Task ExecuteAsync_ShutdownCleanly_WhenCancellationTokenSignaled()
        {
            // Service blocked at WaitToReadAsync (no items queued).
            // StopAsync must cancel the stoppingToken and return promptly without hanging.
            using var db = TestDbContextFactory.CreateContext();

            var scopeFactory = new ControlledScopeFactory(_ =>
                new SignalingScope(new FakeServiceProvider(new Dictionary<Type, Func<object>>
                {
                    [typeof(IAppDbContext)] = () => db
                })));

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);

            await svc.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        }

        // ── Deduplication ─────────────────────────────────────────────────────

        [Fact]
        public async Task QueueAsync_AllowsRequeue_AfterSameIdIsFullyProcessed()
        {
            // _queuedSubmissionIds.TryRemove fires in the finally block of the drain loop
            // *after* ProcessSubmissionAsync returns.  Once removed, a subsequent QueueAsync
            // call for the same ID must succeed (TryAdd returns true) and trigger a second
            // processing round.
            //
            // Sentinel pattern: the sentinel item is queued immediately after the target ID.
            // Because the drain loop is single-reader, by the time the sentinel's scope is
            // disposed (scope 3), TryRemove for the target ID has already completed (it ran
            // between scope 2 disposal and scope 3 creation).  This makes the re-queue safe
            // without requiring timing hacks.
            using var db = TestDbContextFactory.CreateContext();

            var id = Guid.NewGuid();
            var sentinelId = Guid.NewGuid();
            var processorCallCount = 0;
            var sentinelComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requeueueComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Scopes: 1=startup, 2=id, 3=sentinel, 4=id (re-queued)
            var scopeFactory = new ControlledScopeFactory(callNum =>
            {
                var provider = new FakeServiceProvider(new Dictionary<Type, Func<object>>
                {
                    [typeof(IAppDbContext)] = () => db,
                    [typeof(SubmissionFeedbackProcessor)] = () =>
                    {
                        Interlocked.Increment(ref processorCallCount);
                        return MakeProcessor(db);
                    }
                });
                Action? onDispose = callNum == 3 ? () => sentinelComplete.TrySetResult(true)
                                  : callNum == 4 ? () => requeueueComplete.TrySetResult(true)
                                  : null;
                return new SignalingScope(provider, onDispose);
            });

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);

            await svc.QueueAsync(id);
            await svc.QueueAsync(sentinelId);

            // Sentinel processed → TryRemove(id) has already run (sequential drain loop).
            await sentinelComplete.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await svc.QueueAsync(id); // TryAdd must succeed now
            await requeueueComplete.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await svc.StopAsync(CancellationToken.None);

            Assert.Equal(3, processorCallCount); // id, sentinelId, id again
        }

        [Fact]
        public async Task QueueAsync_IsIdempotent_WhenSameIdQueuedTwice()
        {
            // The ConcurrentDictionary guard in QueueAsync must prevent a second channel
            // write for the same ID, resulting in exactly one ProcessSubmissionAsync call.
            using var db = TestDbContextFactory.CreateContext(); // empty DB: startup recovery queues nothing

            var id = Guid.NewGuid();
            var processorCallCount = 0;
            var processedOnce = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Scope 1 = startup; scope 2 = the single processing scope.
            var scopeFactory = new ControlledScopeFactory(callNum =>
            {
                var provider = new FakeServiceProvider(new Dictionary<Type, Func<object>>
                {
                    [typeof(IAppDbContext)] = () => db,
                    [typeof(SubmissionFeedbackProcessor)] = () =>
                    {
                        Interlocked.Increment(ref processorCallCount);
                        return MakeProcessor(db);
                    }
                });
                return new SignalingScope(provider, callNum == 2 ? () => processedOnce.TrySetResult(true) : null);
            });

            var svc = new SubmissionFeedbackBackgroundService(
                scopeFactory, NullLogger<SubmissionFeedbackBackgroundService>.Instance);
            await svc.StartAsync(CancellationToken.None);

            await svc.QueueAsync(id);
            await svc.QueueAsync(id); // TryAdd returns false → no second channel write

            await processedOnce.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await svc.StopAsync(CancellationToken.None);

            Assert.Equal(1, processorCallCount);
        }
    }
}
