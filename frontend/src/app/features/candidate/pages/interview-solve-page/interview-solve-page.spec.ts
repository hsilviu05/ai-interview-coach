import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { convertToParamMap, ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { InterviewSolvePage } from './interview-solve-page';
import { CandidateApi } from '../../services/candidate-api.service';
import { SubmissionApi } from '../../services/submission-api.service';

describe('InterviewSolvePage', () => {
  let component: InterviewSolvePage;
  let fixture: ComponentFixture<InterviewSolvePage>;
  let submissionApiMock: {
    getByInterviewSession: ReturnType<typeof vi.fn>;
    createSubmission: ReturnType<typeof vi.fn>;
    resetProblem: ReturnType<typeof vi.fn>;
    resetInterviewSession: ReturnType<typeof vi.fn>;
  };

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  beforeEach(async () => {
    localStorage.clear();

    submissionApiMock = {
      getByInterviewSession: vi.fn().mockReturnValue(of([
        {
          id: 'submission-1',
          candidateId: 'candidate-1',
          problemId: 'problem-1',
          interviewSessionId: 'session-1',
          language: 'csharp',
          sourceCode: 'Console.WriteLine("done");',
          status: 'Accepted',
          passedTests: 3,
          totalTests: 3,
          executionOutput: '',
          submittedAt: new Date().toISOString(),
        },
      ])),
      createSubmission: vi.fn().mockReturnValue(of({
        id: 'submission-2',
        candidateId: 'candidate-1',
        problemId: 'problem-2',
        interviewSessionId: 'session-1',
        language: 'csharp',
        sourceCode: 'Console.WriteLine("done");',
        status: 'Accepted',
        passedTests: 4,
        totalTests: 4,
        executionOutput: '',
        submittedAt: new Date().toISOString(),
      })),
      resetProblem: vi.fn().mockReturnValue(of(void 0)),
      resetInterviewSession: vi.fn().mockReturnValue(of(void 0)),
    };

    await TestBed.configureTestingModule({
      imports: [InterviewSolvePage],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ token: 'session-token' }),
            },
          },
        },
        {
          provide: CandidateApi,
          useValue: {
            getInterviewByToken: () => of({
              id: 'interview-1',
              title: 'Frontend Interview',
              positionName: 'Frontend Engineer',
              description: '',
              durationMinutes: 60,
              accessToken: 'session-token',
              isActive: true,
              interviewerId: 'interviewer-1',
              createdAt: new Date().toISOString(),
              problems: [
                {
                  problemId: 'problem-1',
                  title: 'Solved Problem',
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
                {
                  problemId: 'problem-2',
                  title: 'Open Problem',
                  description: '',
                  difficulty: 'Medium',
                  topic: 'Strings',
                  constraintsText: '',
                  exampleInput: '',
                  exampleOutput: '',
                  executionMode: 'stdin',
                  csharpStarterCode: '',
                  pythonStarterCode: '',
                  cppStarterCode: '',
                  visibleTestCases: [],
                  orderIndex: 2,
                  points: 100,
                },
              ],
            }),
            startInterviewSession: () => of({
              id: 'session-1',
              interviewId: 'interview-1',
              candidateId: 'candidate-1',
              startedAt: new Date().toISOString(),
              submittedAt: null,
              status: 'InProgress',
              totalScore: 0,
            }),
            completeInterviewSession: () => of({
              id: 'session-1',
              interviewId: 'interview-1',
              candidateId: 'candidate-1',
              startedAt: new Date().toISOString(),
              submittedAt: new Date().toISOString(),
              status: 'Completed',
              totalScore: 100,
            }),
          },
        },
        {
          provide: SubmissionApi,
          useValue: submissionApiMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InterviewSolvePage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should focus the first unfinished problem after loading submissions', () => {
    expect(component.selectedProblemId()).toBe('problem-2');
  });

  it('should clear the active problem after accepting the final open problem', () => {
    component.submitSolution();

    expect(component.selectedProblemId()).toBeNull();
    expect(component.allProblemsCompleted()).toBe(true);
  });

  it('should swap starter code when changing to another supported language', () => {
    component.form.controls.language.setValue('python');

    expect(component.form.controls.sourceCode.value).toContain('import sys');
    expect(component.getLanguageHelperText()).toContain('Python 3');
  });

  it('should preserve separate drafts when switching languages', () => {
    component.form.controls.sourceCode.setValue('Console.WriteLine("csharp draft");');
    component.form.controls.language.setValue('python');

    expect(component.form.controls.sourceCode.value).toContain('import sys');

    component.form.controls.sourceCode.setValue('print("python draft")');
    component.form.controls.language.setValue('csharp');

    expect(component.form.controls.sourceCode.value).toBe('Console.WriteLine("csharp draft");');

    component.form.controls.language.setValue('python');

    expect(component.form.controls.sourceCode.value).toBe('print("python draft")');
  });

  it('should ignore a legacy mismatched draft when the saved Python draft contains the C# starter', () => {
    const problemOne = component.interview()!.problems[0];
    const problemTwo = component.interview()!.problems[1];

    problemTwo.executionMode = 'function';
    problemTwo.csharpStarterCode = `public class Solution
{
    public int MaxProfit(int[] prices)
    {
        return 0;
    }
}`;
    problemTwo.pythonStarterCode = `from typing import List


class Solution:
    def maxProfit(self, prices: List[int]) -> int:
        return 0`;
    const legacyCsharpStarter = `public class Solution
{
    public int MaxProfit(int[] prices)
    {
        
    }
}`;

    component.selectProblem(problemOne);

    localStorage.setItem(
      'candidate_workspace_draft:interview:session-1:problem-2',
      JSON.stringify({
        language: 'python',
        sourceCode: legacyCsharpStarter,
        updatedAt: new Date().toISOString(),
      })
    );

    component.selectProblem(problemTwo);

    expect(component.form.controls.language.value).toBe('python');
    expect(component.form.controls.sourceCode.value).toBe(problemTwo.pythonStarterCode);
  });

  it('should reset all interview problems back to the first one', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    component.submitSolution();
    component.resetAllProblems();

    expect(submissionApiMock.resetInterviewSession).toHaveBeenCalledWith('session-1');
    expect(component.selectedProblemId()).toBe('problem-1');
    expect(component.allProblemsCompleted()).toBe(false);
  });

  it('should reset a completed practice problem from the blank state', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    component.isPracticeMode.set(true);
    component.session.set(null);
    component.interview.set({
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
          problemId: 'practice-problem-1',
          title: 'Two Sum',
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
          points: 0,
        },
      ],
    });
    component.submissions.set([
      {
        id: 'practice-submission-1',
        candidateId: 'candidate-1',
        problemId: 'practice-problem-1',
        interviewSessionId: null,
        language: 'csharp',
        sourceCode: 'Console.WriteLine("done");',
        status: 'Accepted',
        passedTests: 1,
        totalTests: 1,
        executionOutput: '',
        submittedAt: new Date().toISOString(),
      },
    ]);
    component.selectedProblemId.set(null);

    component.resetAllProblems();

    expect(submissionApiMock.resetProblem).toHaveBeenCalledWith('practice-problem-1', undefined);
    expect(component.selectedProblemId()).toBe('practice-problem-1');
    expect(component.allProblemsCompleted()).toBe(false);
  });
});
