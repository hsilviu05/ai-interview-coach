import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CandidateInterviewResponse, InterviewSessionResponse } from '../models/candidate-interview.models';

@Injectable({ providedIn: 'root' })
export class CandidateApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/interviews`;

  getInterviewByToken(token: string): Observable<CandidateInterviewResponse> {
    return this.http.get<CandidateInterviewResponse>(`${this.baseUrl}/token/${token}`);
  }

  startInterviewSession(token: string): Observable<InterviewSessionResponse> {
    return this.http.post<InterviewSessionResponse>(`${this.baseUrl}/token/${token}/start`, {});
  }

  completeInterviewSession(sessionId: string): Observable<InterviewSessionResponse> {
    return this.http.post<InterviewSessionResponse>(`${this.baseUrl}/sessions/${sessionId}/complete`, {});
  }
}