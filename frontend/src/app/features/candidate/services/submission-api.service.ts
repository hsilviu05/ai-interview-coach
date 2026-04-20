import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
  CreateSubmissionRequestDto,
  SubmissionResponseDto,
} from '../../../core/api/generated/backend-api';
import { CreateSubmissionRequest, SubmissionResponse } from '../models/candidate-submission.models';

@Injectable({ providedIn: 'root' })
export class SubmissionApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/submissions`;

  createSubmission(payload: CreateSubmissionRequest): Observable<SubmissionResponse> {
    return this.http
      .post<SubmissionResponseDto>(this.baseUrl, payload as CreateSubmissionRequestDto)
      .pipe(map(submission => this.normalizeSubmission(submission)));
  }

  getMySubmissions(): Observable<SubmissionResponse[]> {
    return this.http
      .get<SubmissionResponseDto[]>(`${this.baseUrl}/me`)
      .pipe(map(submissions => submissions.map(submission => this.normalizeSubmission(submission))));
  }

  getByInterviewSession(sessionId: string): Observable<SubmissionResponse[]> {
    return this.http
      .get<SubmissionResponseDto[]>(`${this.baseUrl}/session/${sessionId}`)
      .pipe(map(submissions => submissions.map(submission => this.normalizeSubmission(submission))));
  }

  resetProblem(problemId: string, interviewSessionId?: string | null): Observable<void> {
    const params = interviewSessionId
      ? new HttpParams().set('interviewSessionId', interviewSessionId)
      : undefined;

    return this.http.delete<void>(`${this.baseUrl}/problem/${problemId}`, { params });
  }

  resetInterviewSession(sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/session/${sessionId}`);
  }

  private normalizeSubmission(submission: SubmissionResponseDto): SubmissionResponse {
    return {
      id: submission.id ?? '',
      candidateId: submission.candidateId ?? '',
      problemId: submission.problemId ?? '',
      interviewSessionId: submission.interviewSessionId ?? null,
      language: submission.language ?? '',
      sourceCode: submission.sourceCode ?? '',
      status: submission.status ?? '',
      passedTests: submission.passedTests ?? 0,
      totalTests: submission.totalTests ?? 0,
      executionTimeMs: submission.executionTimeMs ?? null,
      memoryKb: submission.memoryKb ?? null,
      executionOutput: submission.executionOutput ?? null,
      aiFeedback: submission.aiFeedback ?? null,
      aiFeedbackSource: submission.aiFeedbackSource ?? null,
      aiFeedbackStatus: (submission.aiFeedbackStatus as SubmissionResponse['aiFeedbackStatus']) ?? 'Pending',
      submittedAt: submission.submittedAt ?? '',
    };
  }
}
