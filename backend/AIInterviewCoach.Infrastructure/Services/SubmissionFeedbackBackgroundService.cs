using System.Collections.Concurrent;
using System.Threading.Channels;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIInterviewCoach.Infrastructure.Services
{
    public class SubmissionFeedbackBackgroundService : BackgroundService, ISubmissionFeedbackQueue
    {
        private readonly Channel<Guid> _pendingSubmissionIds = Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        private readonly ConcurrentDictionary<Guid, byte> _queuedSubmissionIds = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubmissionFeedbackBackgroundService> _logger;

        public SubmissionFeedbackBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubmissionFeedbackBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public ValueTask QueueAsync(Guid submissionId, CancellationToken cancellationToken = default)
        {
            if (!_queuedSubmissionIds.TryAdd(submissionId, 0))
            {
                return ValueTask.CompletedTask;
            }

            return _pendingSubmissionIds.Writer.WriteAsync(submissionId, cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await EnqueueExistingPendingSubmissionsAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skipping startup AI feedback recovery because pending submissions could not be queried.");
                return;
            }

            while (await _pendingSubmissionIds.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_pendingSubmissionIds.Reader.TryRead(out var submissionId))
                {
                    try
                    {
                        await ProcessSubmissionAsync(submissionId, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(
                            exception,
                            "Background AI feedback generation crashed for submission {SubmissionId}.",
                            submissionId);
                    }
                    finally
                    {
                        _queuedSubmissionIds.TryRemove(submissionId, out _);
                    }
                }
            }
        }

        private async Task EnqueueExistingPendingSubmissionsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var pendingSubmissionIds = await dbContext.Submissions
                .AsNoTracking()
                .Where(submission =>
                    submission.AiFeedbackStatus == SubmissionFeedbackStatuses.Pending)
                .OrderBy(submission => submission.SubmittedAt)
                .Select(submission => submission.Id)
                .ToListAsync(cancellationToken);

            foreach (var submissionId in pendingSubmissionIds)
            {
                await QueueAsync(submissionId, cancellationToken);
            }
        }

        private async Task ProcessSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<SubmissionFeedbackProcessor>();

            await processor.ProcessAsync(submissionId, cancellationToken);
        }
    }
}
