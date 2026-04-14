import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { convertToParamMap, ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { InterviewSolvePage } from './interview-solve-page';
import { CandidateApi } from '../../services/candidate-api.service';
import { SubmissionApi } from '../../services/submission-api.service';

describe('InterviewSolvePage', () => {
  let component: InterviewSolvePage;
  let fixture: ComponentFixture<InterviewSolvePage>;

  beforeEach(async () => {
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
          useValue: {
            getByInterviewSession: () => of([
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
            ]),
            createSubmission: () => of({
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
            }),
          },
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
});
