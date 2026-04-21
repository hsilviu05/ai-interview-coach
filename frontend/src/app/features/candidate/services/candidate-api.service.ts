import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
  InterviewProblemDto as InterviewProblemResponseDto,
  InterviewProblemVisibleTestCaseDto,
  InterviewResponseDto,
  InterviewSessionResponseDto,
  ProblemResponseDto,
  ProblemSummaryResponseDto,
} from '../../../core/api/generated/backend-api';
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
import {
  ProblemHintResponse,
  RequestProblemHint,
} from '../models/candidate-problem-hint.models';

interface ProblemHintResponseDto {
  level?: number | null;
  content?: string | null;
  source?: string | null;
}

@Injectable({ providedIn: 'root' })
export class CandidateApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/interviews`;
  private readonly problemsBaseUrl = `${environment.apiBaseUrl}/problems`;

  getInterviewByToken(token: string): Observable<CandidateInterviewResponse> {
    return this.http
      .get<InterviewResponseDto>(`${this.baseUrl}/token/${token}`)
      .pipe(map(interview => this.normalizeInterview(interview)));
  }

  getPracticeProblems(): Observable<CandidatePracticeProblemSummary[]> {
    return this.http
      .get<ProblemSummaryResponseDto[]>(this.problemsBaseUrl)
      .pipe(map(problems => problems.map(problem => this.normalizePracticeProblemSummary(problem))));
  }

  getPracticeProblemById(problemId: string): Observable<CandidateInterviewProblemDto> {
    return this.http
      .get<ProblemResponseDto>(`${this.problemsBaseUrl}/${problemId}`)
      .pipe(map(problem => this.mapPracticeProblem(problem)));
  }

  startInterviewSession(token: string): Observable<InterviewSessionResponse> {
    return this.http
      .post<InterviewSessionResponseDto>(`${this.baseUrl}/token/${token}/start`, {})
      .pipe(map(session => this.normalizeInterviewSession(session)));
  }

  completeInterviewSession(sessionId: string): Observable<InterviewSessionResponse> {
    return this.http
      .post<InterviewSessionResponseDto>(`${this.baseUrl}/sessions/${sessionId}/complete`, {})
      .pipe(map(session => this.normalizeInterviewSession(session)));
  }

  requestPracticeHint(
    problemId: string,
    payload: RequestProblemHint
  ): Observable<ProblemHintResponse> {
    return this.http
      .post<ProblemHintResponseDto>(`${this.problemsBaseUrl}/${problemId}/hints`, payload)
      .pipe(
        map(hint => ({
          level: hint.level ?? payload.level,
          content: hint.content ?? '',
          source: hint.source ?? 'LocalFallback',
        }))
      );
  }

  private normalizeInterview(interview: InterviewResponseDto): CandidateInterviewResponse {
    return {
      id: interview.id ?? '',
      title: interview.title ?? '',
      positionName: interview.positionName ?? '',
      description: interview.description ?? '',
      durationMinutes: interview.durationMinutes ?? 0,
      accessToken: interview.accessToken ?? '',
      isActive: interview.isActive ?? false,
      interviewerId: interview.interviewerId ?? '',
      createdAt: interview.createdAt ?? '',
      problems: (interview.problems ?? []).map(problem => this.normalizeProblem(problem)),
    };
  }

  private normalizeProblem(problem: InterviewProblemResponseDto): CandidateInterviewProblemDto {
    return {
      problemId: problem.problemId ?? '',
      title: problem.title ?? '',
      description: problem.description ?? '',
      difficulty: problem.difficulty ?? '',
      topic: problem.topic ?? '',
      constraintsText: problem.constraintsText ?? '',
      exampleInput: problem.exampleInput ?? '',
      exampleOutput: problem.exampleOutput ?? '',
      executionMode: problem.executionMode ?? 'stdin',
      csharpStarterCode: problem.csharpStarterCode ?? '',
      pythonStarterCode: problem.pythonStarterCode ?? '',
      cppStarterCode: problem.cppStarterCode ?? '',
      visibleTestCases: this.normalizeVisibleTestCases(problem.visibleTestCases),
      orderIndex: problem.orderIndex ?? 0,
      points: problem.points ?? 0,
    };
  }

  private normalizeVisibleTestCases(
    testCases: InterviewProblemVisibleTestCaseDto[] | null | undefined
  ): CandidateInterviewVisibleTestCase[] {
    return (testCases ?? []).map(testCase => ({
      input: testCase.input ?? '',
      expectedOutput: testCase.expectedOutput ?? '',
      orderIndex: testCase.orderIndex ?? 0,
    }));
  }

  private normalizePracticeProblemSummary(problem: ProblemSummaryResponseDto): CandidatePracticeProblemSummary {
    return {
      id: problem.id ?? '',
      title: problem.title ?? '',
      description: problem.description ?? '',
      difficulty: problem.difficulty ?? '',
      topic: problem.topic ?? '',
      constraintsText: problem.constraintsText ?? '',
      exampleInput: problem.exampleInput ?? '',
      exampleOutput: problem.exampleOutput ?? '',
      createdAt: problem.createdAt ?? '',
    };
  }

  private mapPracticeProblem(problem: ProblemResponseDto): CandidateInterviewProblemDto {
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

  private normalizeInterviewSession(session: InterviewSessionResponseDto): InterviewSessionResponse {
    return {
      id: session.id ?? '',
      interviewId: session.interviewId ?? '',
      candidateId: session.candidateId ?? '',
      startedAt: session.startedAt ?? '',
      submittedAt: session.submittedAt ?? null,
      status: session.status ?? '',
      totalScore: session.totalScore ?? 0,
    };
  }
}
