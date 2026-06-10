import { TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { CandidateWorkspaceFacade } from './candidate-workspace.facade';
import { CandidateApi } from './candidate-api.service';
import { SubmissionApi } from './submission-api.service';
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
});
