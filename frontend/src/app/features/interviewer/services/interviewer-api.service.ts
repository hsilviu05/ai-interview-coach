import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
    AddProblemToInterviewRequest,
    CreateInterviewRequest,
    CreateProblemRequest,
    CreateTestCaseRequest,
    InterviewResponse,
    ProblemListItem,
    ReplaceProblemCatalogResponse,
    TestCaseListItem,
} from '../models/interviewer.models';
import {
    InterviewSessionDetails,
    InterviewSessionSummary,
} from '../models/interviewer-session.models';
import { InterviewListItem } from '../models/interviewer-list.models';

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

    getInterviewSessions(interviewId: string): Observable<InterviewSessionSummary[]> {
        return this.http.get<InterviewSessionSummary[]>(
            `${this.baseUrl}/${interviewId}/sessions`
        );
    }

    getInterviewSessionDetails(sessionId: string): Observable<InterviewSessionDetails> {
        return this.http.get<InterviewSessionDetails>(
            `${this.baseUrl}/sessions/${sessionId}`
        );
    }

    getMyInterviews(): Observable<InterviewListItem[]> {
        return this.http.get<InterviewListItem[]>(this.baseUrl);
    }

    createProblem(payload: CreateProblemRequest): Observable<ProblemListItem> {
        return this.http.post<ProblemListItem>(this.problemsBaseUrl, payload);
    }

    deleteProblem(problemId: string): Observable<void> {
        return this.http.delete<void>(`${this.problemsBaseUrl}/${problemId}`);
    }

    replaceCatalogWithStarterSet(): Observable<ReplaceProblemCatalogResponse> {
        return this.http.post<ReplaceProblemCatalogResponse>(
            `${this.problemsBaseUrl}/catalog/replace-with-starter-set`,
            {}
        );
    }

    getTestCases(problemId: string, includeHidden = true): Observable<TestCaseListItem[]> {
        return this.http.get<TestCaseListItem[]>(
            `${this.problemsBaseUrl}/${problemId}/testcases`,
            { params: { includeHidden } }
        );
    }

    addTestCase(problemId: string, payload: CreateTestCaseRequest): Observable<TestCaseListItem> {
        return this.http.post<TestCaseListItem>(
            `${this.problemsBaseUrl}/${problemId}/testcases`,
            payload
        );
    }
}
