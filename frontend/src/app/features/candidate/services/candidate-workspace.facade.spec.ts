import { TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute, Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { CandidateWorkspaceFacade } from './candidate-workspace.facade';
import { CandidateApi } from './candidate-api.service';
import { SubmissionApi } from './submission-api.service';
import { CodeDraftStorageService } from './code-draft-storage.service';
import type { CandidateInterviewResponse, InterviewSessionResponse } from '../models/candidate-interview.models';
import type { SubmissionResponse } from '../models/candidate-submission.models';

// ── Shared stubs ─────────────────────────────────────────────────────────────

const sessionStub: InterviewSessionResponse = {
  id: 'session-1',
  interviewId: 'interview-1',
  candidateId: 'candidate-1',
  startedAt: new Date().toISOString(),
  submittedAt: null,
  status: 'InProgress',
  totalScore: 0,
};

const practiceInterviewStub: CandidateInterviewResponse = {
  id: 'practice-problem-1',
  title: 'Practice Workspace',
  positionName: 'Standalone Problem Practice',
  description: '',
  durationMinutes: 0,
  accessToken: '',
  isActive: true,
  interviewerId: '',
  createdAt: new Date().toISOString(),
  problems: [
    {
      problemId: 'problem-1',
      title: 'Test Problem',
      description: '',
      difficulty: 'Easy',
      topic: 'Arrays',
      constraintsText: '',
      exampleInput: '',
      exampleOutput: '',
      executionMode: 'stdin',
      csharpStarterCode: '',
      pythonStarterCode: '',
      cppStarterCode: '',
      visibleTestCases: [],
      orderIndex: 1,
      points: 100,
    },
  ],
};

function makeSub(
  status: 'Pending' | 'Ready' | 'Failed',
  sessionId: string | null = 'session-1'
): SubmissionResponse {
  return {
    id: 'sub-1',
    candidateId: 'candidate-1',
    problemId: 'problem-1',
    interviewSessionId: sessionId,
    language: 'csharp',
    sourceCode: 'code',
    status: 'Accepted',
    passedTests: 1,
    totalTests: 1,
    aiFeedbackStatus: status,
    aiFeedback: status === 'Ready' ? 'Overall\nLooks good.' : null,
    submittedAt: new Date().toISOString(),
  };
}

// ── Spec ─────────────────────────────────────────────────────────────────────

describe('CandidateWorkspaceFacade — feedback polling', () => {
  let facade: CandidateWorkspaceFacade;
  let submissionApiMock: {
    getByInterviewSession: ReturnType<typeof vi.fn>;
    getMySubmissions: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    vi.useFakeTimers();

    submissionApiMock = {
      getByInterviewSession: vi.fn().mockReturnValue(of([])),
      getMySubmissions: vi.fn().mockReturnValue(of([])),
    };

    const candidateApiMock = {
      getInterviewByToken: vi.fn().mockReturnValue(of(null)),
      startInterviewSession: vi.fn().mockReturnValue(of(null)),
      completeInterviewSession: vi.fn().mockReturnValue(of(null)),
      requestPracticeHint: vi.fn().mockReturnValue(of(null)),
      getPracticeProblemById: vi.fn().mockReturnValue(of(null)),
    };

    TestBed.configureTestingModule({
      providers: [
        CandidateWorkspaceFacade,
        provideRouter([]),
        provideHttpClient(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } },
        },
        { provide: SubmissionApi, useValue: submissionApiMock },
        { provide: CandidateApi, useValue: candidateApiMock },
      ],
    });

    facade = TestBed.inject(CandidateWorkspaceFacade);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
    localStorage.clear();
  });

  // ── Fires initial poll ────────────────────────────────────────────────────

  it('fires a refresh request after the default 1 500 ms delay when a submission is Pending', () => {
    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]);

    expect(submissionApiMock.getByInterviewSession).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1500);

    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledOnce();
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledWith('session-1');
  });

  // ── Continues while Pending, stops on Ready ───────────────────────────────

  it('re-arms after each Pending poll and stops once the status becomes Ready', () => {
    let currentSubs = [makeSub('Pending')];
    submissionApiMock.getByInterviewSession.mockImplementation(() => of(currentSubs));

    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]);

    vi.advanceTimersByTime(1500); // first refresh → still Pending → re-arms
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(1);

    currentSubs = [makeSub('Ready')];
    vi.advanceTimersByTime(1500); // second refresh → Ready → stops
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(2);

    vi.advanceTimersByTime(5000); // verify no further polls
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(2);

    expect(facade.submissions()[0].aiFeedbackStatus).toBe('Ready');
  });

  // ── Stops on Failed ───────────────────────────────────────────────────────

  it('stops polling when the status becomes Failed', () => {
    submissionApiMock.getByInterviewSession.mockReturnValue(of([makeSub('Failed')]));

    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]);

    vi.advanceTimersByTime(1500);
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(5000); // no further polls after Failed
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(1);

    expect(facade.submissions()[0].aiFeedbackStatus).toBe('Failed');
  });

  // ── No duplicate concurrent poll ─────────────────────────────────────────

  it('does not arm a second timer when ensureFeedbackPolling is called while one is already pending', () => {
    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]); // arms timer #1
    (facade as any).ensureFeedbackPolling(); // guard: feedbackRefreshTimeoutId !== null → no-op

    vi.advanceTimersByTime(1500);

    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(1);
  });

  // ── Error retry with 4 000 ms back-off ───────────────────────────────────

  it('retries with a 4 000 ms back-off after an API error, then stops on Ready', () => {
    submissionApiMock.getByInterviewSession
      .mockReturnValueOnce(throwError(() => new Error('Network error')))
      .mockReturnValue(of([makeSub('Ready')]));

    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]);

    vi.advanceTimersByTime(1500); // first poll → error → re-arms with 4 000 ms delay
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(1500); // t = 3 000 ms — back-off (5 500 ms) not yet elapsed
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(2500); // t = 5 500 ms — back-off elapsed → second poll → Ready
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(2);

    vi.advanceTimersByTime(5000); // no further polls
    expect(submissionApiMock.getByInterviewSession).toHaveBeenCalledTimes(2);
  });

  // ── No leaked timer on destroy ────────────────────────────────────────────

  it('cancels the pending poll timer when the facade is destroyed', () => {
    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]); // arms timer

    // Destroying the test module triggers DestroyRef.onDestroy → stopFeedbackPolling().
    TestBed.resetTestingModule();

    vi.advanceTimersByTime(5000); // timer was cleared; no API call

    expect(submissionApiMock.getByInterviewSession).not.toHaveBeenCalled();
  });

  // ── Practice mode uses getMySubmissions ───────────────────────────────────

  it('calls getMySubmissions (not getByInterviewSession) when in practice mode', () => {
    // Practice submissions have no interviewSessionId; they are filtered by problemId.
    submissionApiMock.getMySubmissions.mockReturnValue(
      of([makeSub('Ready', null)])
    );

    facade.isPracticeMode.set(true);
    facade.interview.set(practiceInterviewStub);

    (facade as any).setSubmissions([makeSub('Pending', null)]);

    vi.advanceTimersByTime(1500);

    expect(submissionApiMock.getMySubmissions).toHaveBeenCalledTimes(1);
    expect(submissionApiMock.getByInterviewSession).not.toHaveBeenCalled();

    vi.advanceTimersByTime(5000); // no further polls after Ready
    expect(submissionApiMock.getMySubmissions).toHaveBeenCalledTimes(1);
  });

  // ── Refresh is a no-op when submissions clear before timer fires ──────────

  it('stops polling without calling the API when submissions become non-Pending before the timer fires', () => {
    // refreshPendingFeedback early-return guard: hasPendingAiFeedback returns false
    // when the timer fires even though it was true when the timer was armed.
    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]); // arms timer

    // Directly mutate the signal (bypassing setSubmissions/stopFeedbackPolling) so the
    // timer is still live but submissions are no longer Pending.
    (facade as any).submissions.set([makeSub('Ready')]);

    vi.advanceTimersByTime(1500); // timer fires → no pending → early return

    expect(submissionApiMock.getByInterviewSession).not.toHaveBeenCalled();
    expect(submissionApiMock.getMySubmissions).not.toHaveBeenCalled();
  });

  // ── Interview mode: stops when session is null at poll time ──────────────

  it('stops polling in interview mode when session is cleared before the timer fires', () => {
    // refreshPendingFeedback: no sessionId path → stopFeedbackPolling, no API call.
    facade.session.set(sessionStub);
    (facade as any).setSubmissions([makeSub('Pending')]); // arms timer

    facade.session.set(null); // session gone before the timer fires

    vi.advanceTimersByTime(1500);

    expect(submissionApiMock.getByInterviewSession).not.toHaveBeenCalled();
  });

  // ── Practice mode: stops when no problemId can be resolved ───────────────

  it('stops polling in practice mode when neither interview nor selectedProblemId is available', () => {
    // refreshPendingFeedback practice branch: no practiceProblemId → stopFeedbackPolling.
    facade.isPracticeMode.set(true);
    facade.interview.set(null);       // interview?.problems[0]?.problemId → undefined
    facade.selectedProblemId.set(null); // fallback also null

    (facade as any).submissions.set([makeSub('Pending', null)]);
    (facade as any).ensureFeedbackPolling(); // arm timer manually (bypasses setSubmissions guard)

    vi.advanceTimersByTime(1500);

    expect(submissionApiMock.getMySubmissions).not.toHaveBeenCalled();
  });

  // ── Practice mode error retry with 4 000 ms back-off ─────────────────────

  it('retries with a 4 000 ms back-off after a getMySubmissions error in practice mode', () => {
    submissionApiMock.getMySubmissions
      .mockReturnValueOnce(throwError(() => new Error('Network error')))
      .mockReturnValue(of([makeSub('Ready', null)]));

    facade.isPracticeMode.set(true);
    facade.interview.set(practiceInterviewStub);

    (facade as any).setSubmissions([makeSub('Pending', null)]);

    vi.advanceTimersByTime(1500); // first poll → error → re-arms with 4 000 ms
    expect(submissionApiMock.getMySubmissions).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(1500); // t = 3 000 ms — back-off not yet elapsed
    expect(submissionApiMock.getMySubmissions).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(2500); // t = 5 500 ms — second poll → Ready
    expect(submissionApiMock.getMySubmissions).toHaveBeenCalledTimes(2);

    vi.advanceTimersByTime(5000); // no further polls
    expect(submissionApiMock.getMySubmissions).toHaveBeenCalledTimes(2);
  });
});

// ── Shared provider factory ───────────────────────────────────────────────────

function makeCandidateApiMock() {
  return {
    getInterviewByToken: vi.fn().mockReturnValue(of(null)),
    startInterviewSession: vi.fn().mockReturnValue(of(null)),
    completeInterviewSession: vi.fn().mockReturnValue(of(null)),
    requestPracticeHint: vi.fn().mockReturnValue(of(null)),
    getPracticeProblemById: vi.fn().mockReturnValue(of(null)),
  };
}

function makeSubmissionApiMock() {
  return {
    getByInterviewSession: vi.fn().mockReturnValue(of([])),
    getMySubmissions: vi.fn().mockReturnValue(of([])),
    createSubmission: vi.fn().mockReturnValue(of(null)),
    resetProblem: vi.fn().mockReturnValue(of(void 0)),
    resetInterviewSession: vi.fn().mockReturnValue(of(void 0)),
  };
}

function configureFacadeTestBed(
  paramMap: Record<string, string>,
  candidateApi = makeCandidateApiMock(),
  submissionApi = makeSubmissionApiMock()
): void {
  TestBed.configureTestingModule({
    providers: [
      CandidateWorkspaceFacade,
      provideRouter([]),
      provideHttpClient(),
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(paramMap) } },
      },
      { provide: CandidateApi, useValue: candidateApi },
      { provide: SubmissionApi, useValue: submissionApi },
    ],
  });
}

// ── Draft autosave (debounce + distinctUntilChanged) ─────────────────────────

describe('CandidateWorkspaceFacade — draft autosave', () => {
  let facade: CandidateWorkspaceFacade;
  let saveDraftSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    vi.useFakeTimers();
    configureFacadeTestBed({});
    facade = TestBed.inject(CandidateWorkspaceFacade);
    facade.loading.set(false);
    facade.session.set(sessionStub);
    facade.selectedProblemId.set('problem-1');
    facade.interview.set(practiceInterviewStub);
    saveDraftSpy = vi.spyOn(TestBed.inject(CodeDraftStorageService), 'saveDraft');
  });

  afterEach(() => {
    // Null selectedProblemId before Angular destroys the TestBed so that
    // the onDestroy → persistCurrentDraft call is a no-op and does not
    // re-populate localStorage after we clear it here.
    facade.selectedProblemId.set(null);
    vi.useRealTimers();
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('does not save the draft before the 500 ms debounce window', () => {
    facade.form.controls.sourceCode.setValue('DRAFT_A');
    vi.advanceTimersByTime(499);
    expect(saveDraftSpy).not.toHaveBeenCalled();
  });

  it('saves the draft once the 500 ms debounce window elapses', () => {
    facade.form.controls.sourceCode.setValue('DRAFT_A');
    vi.advanceTimersByTime(500);
    expect(saveDraftSpy).toHaveBeenCalledOnce();
  });

  it('does not re-save when the form value is unchanged (distinctUntilChanged)', () => {
    facade.form.controls.sourceCode.setValue('SAME_CONTENT');
    vi.advanceTimersByTime(500);
    expect(saveDraftSpy).toHaveBeenCalledTimes(1);

    facade.form.controls.sourceCode.setValue('SAME_CONTENT');
    vi.advanceTimersByTime(500);
    // distinctUntilChanged sees the same JSON → no additional save
    expect(saveDraftSpy).toHaveBeenCalledTimes(1);
  });

  it('saves again when the form content changes after a prior debounced save', () => {
    facade.form.controls.sourceCode.setValue('VERSION_1');
    vi.advanceTimersByTime(500);
    expect(saveDraftSpy).toHaveBeenCalledTimes(1);

    facade.form.controls.sourceCode.setValue('VERSION_2');
    vi.advanceTimersByTime(500);
    expect(saveDraftSpy).toHaveBeenCalledTimes(2);
  });
});

// ── Language switch and per-language draft isolation ─────────────────────────

describe('CandidateWorkspaceFacade — language switch and draft isolation', () => {
  let facade: CandidateWorkspaceFacade;

  beforeEach(() => {
    vi.useFakeTimers(); // suppress debounced autosave side-effects
    configureFacadeTestBed({});
    facade = TestBed.inject(CandidateWorkspaceFacade);
    facade.loading.set(false);
    facade.session.set(sessionStub);
    facade.selectedProblemId.set('problem-1');
    facade.interview.set(practiceInterviewStub); // stdin mode, empty starters
  });

  afterEach(() => {
    // Null selectedProblemId before Angular destroys the TestBed so that
    // the onDestroy → persistCurrentDraft call is a no-op and does not
    // re-populate localStorage after we clear it here.
    facade.selectedProblemId.set(null);
    vi.useRealTimers();
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('loads the global stdin starter when switching to a language with no saved draft', () => {
    facade.form.controls.language.setValue('python');
    expect(facade.form.controls.sourceCode.value).toContain('import sys');
  });

  it('preserves separate drafts per language when switching back and forth', () => {
    facade.form.controls.sourceCode.setValue('CSHARP_DRAFT');

    facade.form.controls.language.setValue('python'); // saves C# draft, loads Python starter
    expect(facade.form.controls.sourceCode.value).toContain('import sys');

    facade.form.controls.sourceCode.setValue('PYTHON_DRAFT');

    facade.form.controls.language.setValue('csharp'); // saves Python draft, reloads C# draft
    expect(facade.form.controls.sourceCode.value).toBe('CSHARP_DRAFT');

    facade.form.controls.language.setValue('python'); // reloads saved Python draft
    expect(facade.form.controls.sourceCode.value).toBe('PYTHON_DRAFT');
  });

  it('loads the function-mode per-language starter when the problem uses function execution', () => {
    facade.interview.set({
      ...practiceInterviewStub,
      problems: [{
        ...practiceInterviewStub.problems[0],
        executionMode: 'function',
        csharpStarterCode: 'public int CsharpSolution() { return 0; }',
        pythonStarterCode: 'def python_solution(): return 0',
        cppStarterCode: 'int cpp_solution() { return 0; }',
      }],
    });

    facade.form.controls.language.setValue('python');
    expect(facade.form.controls.sourceCode.value).toBe('def python_solution(): return 0');

    facade.form.controls.language.setValue('cpp');
    expect(facade.form.controls.sourceCode.value).toBe('int cpp_solution() { return 0; }');
  });
});

// ── Hint system (maxHintLevel cap, error path) ────────────────────────────────

describe('CandidateWorkspaceFacade — hint system', () => {
  let facade: CandidateWorkspaceFacade;
  let candidateApiMock: ReturnType<typeof makeCandidateApiMock>;

  beforeEach(() => {
    candidateApiMock = makeCandidateApiMock();
    configureFacadeTestBed({}, candidateApiMock);
    facade = TestBed.inject(CandidateWorkspaceFacade);
    facade.loading.set(false);
    facade.isPracticeMode.set(true);
    facade.interview.set(practiceInterviewStub);
    facade.selectedProblemId.set('problem-1');
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('nextHintLevel returns null once all three hints are unlocked', () => {
    facade.practiceHintsByProblemId.set({
      'problem-1': [
        { level: 1, content: 'H1', source: 'LocalFallback' },
        { level: 2, content: 'H2', source: 'LocalFallback' },
        { level: 3, content: 'H3', source: 'LocalFallback' },
      ],
    });

    expect(facade.nextHintLevel()).toBeNull();
  });

  it('requestNextHint is a no-op when all three hints are already unlocked', () => {
    facade.practiceHintsByProblemId.set({
      'problem-1': [
        { level: 1, content: 'H1', source: 'LocalFallback' },
        { level: 2, content: 'H2', source: 'LocalFallback' },
        { level: 3, content: 'H3', source: 'LocalFallback' },
      ],
    });

    facade.requestNextHint();

    expect(candidateApiMock.requestPracticeHint).not.toHaveBeenCalled();
  });

  it('sets hintErrorMessage and clears requestingHintLevel when the hint API returns an error', () => {
    candidateApiMock.requestPracticeHint.mockReturnValue(
      throwError(() => ({ error: { message: 'Quota exceeded.' } }))
    );

    facade.requestNextHint(); // nextHintLevel() is 1 (no hints yet)

    expect(facade.hintErrorMessage()).toBe('Quota exceeded.');
    expect(facade.requestingHintLevel()).toBeNull();
  });
});

// ── initializeFromRoute ───────────────────────────────────────────────────────

describe('CandidateWorkspaceFacade — initializeFromRoute: missing token', () => {
  let facade: CandidateWorkspaceFacade;

  beforeEach(() => {
    configureFacadeTestBed({});
    facade = TestBed.inject(CandidateWorkspaceFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('sets errorMessage and clears loading when neither token nor problemId is present', () => {
    facade.initializeFromRoute();

    expect(facade.errorMessage()).toBe('Missing interview token.');
    expect(facade.loading()).toBe(false);
  });
});

describe('CandidateWorkspaceFacade — initializeFromRoute: practice mode', () => {
  let facade: CandidateWorkspaceFacade;
  let candidateApiMock: ReturnType<typeof makeCandidateApiMock>;
  let submissionApiMock: ReturnType<typeof makeSubmissionApiMock>;

  beforeEach(() => {
    candidateApiMock = makeCandidateApiMock();
    candidateApiMock.getPracticeProblemById.mockReturnValue(
      of(practiceInterviewStub.problems[0])
    );
    submissionApiMock = makeSubmissionApiMock();
    submissionApiMock.getMySubmissions.mockReturnValue(of([]));
    configureFacadeTestBed({ problemId: 'problem-1' }, candidateApiMock, submissionApiMock);
    facade = TestBed.inject(CandidateWorkspaceFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('sets isPracticeMode and loads the problem by ID', () => {
    facade.initializeFromRoute();

    expect(facade.isPracticeMode()).toBe(true);
    expect(candidateApiMock.getPracticeProblemById).toHaveBeenCalledWith('problem-1');
    expect(facade.loading()).toBe(false);
  });

  it('sets errorMessage and clears loading when the practice problem API fails', () => {
    candidateApiMock.getPracticeProblemById.mockReturnValue(
      throwError(() => ({ error: { message: 'Not found.' } }))
    );

    facade.initializeFromRoute();

    expect(facade.errorMessage()).toBe('Not found.');
    expect(facade.loading()).toBe(false);
  });
});

describe('CandidateWorkspaceFacade — initializeFromRoute: interview token', () => {
  let facade: CandidateWorkspaceFacade;
  let candidateApiMock: ReturnType<typeof makeCandidateApiMock>;
  let submissionApiMock: ReturnType<typeof makeSubmissionApiMock>;

  beforeEach(() => {
    candidateApiMock = makeCandidateApiMock();
    candidateApiMock.getInterviewByToken.mockReturnValue(of(practiceInterviewStub));
    candidateApiMock.startInterviewSession.mockReturnValue(of(sessionStub));
    submissionApiMock = makeSubmissionApiMock();
    submissionApiMock.getByInterviewSession.mockReturnValue(of([]));
    configureFacadeTestBed({ token: 'test-token' }, candidateApiMock, submissionApiMock);
    facade = TestBed.inject(CandidateWorkspaceFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('loads the interview and starts a session when a token is present', () => {
    facade.initializeFromRoute();

    expect(candidateApiMock.getInterviewByToken).toHaveBeenCalledWith('test-token');
    expect(candidateApiMock.startInterviewSession).toHaveBeenCalledWith('test-token');
    expect(facade.session()).toEqual(sessionStub);
    expect(facade.loading()).toBe(false);
  });

  it('sets errorMessage and clears loading when getInterviewByToken fails', () => {
    candidateApiMock.getInterviewByToken.mockReturnValue(
      throwError(() => ({ error: { message: 'Token expired.' } }))
    );

    facade.initializeFromRoute();

    expect(facade.errorMessage()).toBe('Token expired.');
    expect(facade.loading()).toBe(false);
  });

  it('sets the already-completed message and clears loading when startInterviewSession returns 409', () => {
    candidateApiMock.startInterviewSession.mockReturnValue(
      throwError(() => ({ status: 409, error: { message: 'Conflict.' } }))
    );

    facade.initializeFromRoute();

    expect(facade.errorMessage()).toBe(
      'This interview session has already been completed. Contact your interviewer if this is unexpected.'
    );
    expect(facade.loading()).toBe(false);
    expect(facade.startingSession()).toBe(false);
  });

  it('sets the already-completed message when the error message contains "already completed"', () => {
    candidateApiMock.startInterviewSession.mockReturnValue(
      throwError(() => ({ status: 400, error: { message: 'Session has already completed.' } }))
    );

    facade.initializeFromRoute();

    expect(facade.errorMessage()).toBe(
      'This interview session has already been completed. Contact your interviewer if this is unexpected.'
    );
    expect(facade.loading()).toBe(false);
    expect(facade.startingSession()).toBe(false);
  });
});

// ── completeInterview and submitSolution ─────────────────────────────────────

describe('CandidateWorkspaceFacade — completeInterview', () => {
  let facade: CandidateWorkspaceFacade;
  let candidateApiMock: ReturnType<typeof makeCandidateApiMock>;

  beforeEach(() => {
    candidateApiMock = makeCandidateApiMock();
    configureFacadeTestBed({}, candidateApiMock);
    facade = TestBed.inject(CandidateWorkspaceFacade);
    facade.loading.set(false);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('navigates to /candidate/practice without calling the API when in practice mode', () => {
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    facade.isPracticeMode.set(true);

    facade.completeInterview();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/candidate/practice');
    expect(candidateApiMock.completeInterviewSession).not.toHaveBeenCalled();
  });

  it('sets errorMessage when called with no active session in interview mode', () => {
    facade.session.set(null);

    facade.completeInterview();

    expect(facade.errorMessage()).toBe('No active session.');
    expect(candidateApiMock.completeInterviewSession).not.toHaveBeenCalled();
  });

  it('sets errorMessage from the API error when completeInterviewSession fails', () => {
    candidateApiMock.completeInterviewSession.mockReturnValue(
      throwError(() => ({ error: { message: 'Session already completed.' } }))
    );
    facade.session.set(sessionStub);

    facade.completeInterview();

    expect(facade.errorMessage()).toBe('Session already completed.');
    expect(facade.completing()).toBe(false);
  });
});

describe('CandidateWorkspaceFacade — submitSolution', () => {
  let facade: CandidateWorkspaceFacade;
  let submissionApiMock: ReturnType<typeof makeSubmissionApiMock>;

  beforeEach(() => {
    submissionApiMock = makeSubmissionApiMock();
    configureFacadeTestBed({}, makeCandidateApiMock(), submissionApiMock);
    facade = TestBed.inject(CandidateWorkspaceFacade);
    facade.loading.set(false);
    facade.isPracticeMode.set(true);
    facade.interview.set(practiceInterviewStub);
    facade.selectedProblemId.set('problem-1');
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('sets errorMessage immediately when no problem is selected', () => {
    facade.selectedProblemId.set(null);

    facade.submitSolution();

    expect(facade.errorMessage()).toBe('No selected problem.');
    expect(submissionApiMock.createSubmission).not.toHaveBeenCalled();
  });

  it('sets errorMessage from the server error when the submission API fails', () => {
    submissionApiMock.createSubmission.mockReturnValue(
      throwError(() => ({ error: { message: 'Session expired.' } }))
    );

    facade.submitSolution();

    expect(facade.errorMessage()).toBe('Session expired.');
    expect(facade.submitting()).toBe(false);
  });

  it('sets errorMessage with status and output when submission returns a non-Accepted status', () => {
    submissionApiMock.createSubmission.mockReturnValue(of({
      id: 'sub-1',
      candidateId: 'c1',
      problemId: 'problem-1',
      interviewSessionId: null,
      language: 'csharp',
      sourceCode: 'code',
      status: 'WrongAnswer',
      passedTests: 0,
      totalTests: 1,
      executionOutput: 'Expected 5, got 3',
      aiFeedbackStatus: 'Pending',
      submittedAt: new Date().toISOString(),
    }));

    facade.submitSolution();

    expect(facade.errorMessage()).toContain('WrongAnswer');
    expect(facade.errorMessage()).toContain('Expected 5, got 3');
  });
});
