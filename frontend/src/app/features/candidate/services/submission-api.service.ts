import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreateSubmissionRequest, SubmissionResponse } from '../models/candidate-submission.models';

@Injectable({ providedIn: 'root' })
export class SubmissionApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/submissions`;

  createSubmission(payload: CreateSubmissionRequest): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(this.baseUrl, payload);
  }

  getMySubmissions(): Observable<SubmissionResponse[]> {
    return this.http.get<SubmissionResponse[]>(`${this.baseUrl}/me`);
  }

  getByInterviewSession(sessionId: string): Observable<SubmissionResponse[]> {
    return this.http.get<SubmissionResponse[]>(`${this.baseUrl}/session/${sessionId}`);
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
}
