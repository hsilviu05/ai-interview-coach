import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AddProblemToInterviewRequest,
  CreateInterviewRequest,
  InterviewResponse,
  ProblemListItem,
} from '../models/interviewer.models';

@Injectable({ providedIn: 'root' })
export class InterviewerApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/interviews`;
  private readonly problemsBaseUrl = `${environment.apiBaseUrl}/problems`;

  createInterview(payload: CreateInterviewRequest): Observable<InterviewResponse> {
    return this.http.post<InterviewResponse>(this.baseUrl, payload);
  }

  getProblems(): Observable<ProblemListItem[]> {
    return this.http.get<ProblemListItem[]>(this.problemsBaseUrl);
  }

  addProblemToInterview(
    interviewId: string,
    payload: AddProblemToInterviewRequest
  ): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/${interviewId}/problems`,
      payload
    );
  }
}