import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CandidatePracticeProblemDetail,
  CandidatePracticeProblemSummary,
} from '../models/candidate-practice.models';
import {
  CandidateInterviewProblemDto,
  CandidateInterviewResponse,
  CandidateInterviewVisibleTestCase,
  InterviewSessionResponse,
} from '../models/candidate-interview.models';

@Injectable({ providedIn: 'root' })
export class CandidateApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/interviews`;
  private readonly problemsBaseUrl = `${environment.apiBaseUrl}/problems`;

  getInterviewByToken(token: string): Observable<CandidateInterviewResponse> {
    return this.http
      .get<CandidateInterviewResponse>(`${this.baseUrl}/token/${token}`)
      .pipe(map(interview => this.normalizeInterview(interview)));
  }

  getPracticeProblems(): Observable<CandidatePracticeProblemSummary[]> {
    return this.http.get<CandidatePracticeProblemSummary[]>(this.problemsBaseUrl);
  }

  getPracticeProblemById(problemId: string): Observable<CandidateInterviewProblemDto> {
    return this.http
      .get<CandidatePracticeProblemDetail>(`${this.problemsBaseUrl}/${problemId}`)
      .pipe(map(problem => this.mapPracticeProblem(problem)));
  }

  startInterviewSession(token: string): Observable<InterviewSessionResponse> {
    return this.http.post<InterviewSessionResponse>(`${this.baseUrl}/token/${token}/start`, {});
  }

  completeInterviewSession(sessionId: string): Observable<InterviewSessionResponse> {
    return this.http.post<InterviewSessionResponse>(`${this.baseUrl}/sessions/${sessionId}/complete`, {});
  }

  private normalizeInterview(interview: CandidateInterviewResponse): CandidateInterviewResponse {
    return {
      ...interview,
      problems: (interview.problems ?? []).map(problem => this.normalizeProblem(problem)),
    };
  }

  private normalizeProblem(problem: CandidateInterviewProblemDto): CandidateInterviewProblemDto {
    return {
      ...problem,
      description: problem.description || '',
      difficulty: problem.difficulty || '',
      topic: problem.topic || '',
      constraintsText: problem.constraintsText || '',
      exampleInput: problem.exampleInput || '',
      exampleOutput: problem.exampleOutput || '',
      executionMode: problem.executionMode || 'stdin',
      csharpStarterCode: problem.csharpStarterCode || '',
      pythonStarterCode: problem.pythonStarterCode || '',
      cppStarterCode: problem.cppStarterCode || '',
      visibleTestCases: this.normalizeVisibleTestCases(problem.visibleTestCases),
    };
  }

  private normalizeVisibleTestCases(
    testCases: CandidateInterviewVisibleTestCase[] | undefined
  ): CandidateInterviewVisibleTestCase[] {
    return (testCases ?? []).map(testCase => ({
      input: testCase.input || '',
      expectedOutput: testCase.expectedOutput || '',
      orderIndex: testCase.orderIndex ?? 0,
    }));
  }

  private mapPracticeProblem(problem: CandidatePracticeProblemDetail): CandidateInterviewProblemDto {
    return this.normalizeProblem({
      problemId: problem.id,
      title: problem.title,
      description: problem.description,
      difficulty: problem.difficulty,
      topic: problem.topic,
      constraintsText: problem.constraintsText,
      exampleInput: problem.exampleInput,
      exampleOutput: problem.exampleOutput,
      executionMode: problem.executionMode,
      csharpStarterCode: problem.csharpStarterCode,
      pythonStarterCode: problem.pythonStarterCode,
      cppStarterCode: problem.cppStarterCode,
      visibleTestCases: [],
      orderIndex: 1,
      points: 0,
    });
  }
}
