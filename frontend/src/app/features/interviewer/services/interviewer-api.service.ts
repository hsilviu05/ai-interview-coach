import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
    AdminAuditLogResponseDto,
    InterviewResponseDto,
    InterviewSessionDetailsDto,
    InterviewSessionResponseDto,
    ProblemSignatureDefinitionDto,
    ProblemSignaturePreviewResponseDto,
    ProblemResponseDto,
    ProblemSummaryResponseDto,
    ProblemTemplateResponseDto,
    ReplaceProblemCatalogResponseDto,
    SubmissionResponseDto,
    TestCaseResponseDto,
} from '../../../core/api/generated/backend-api';
import {
    AdminAuditLogListItem,
    AddProblemToInterviewRequest,
    CreateInterviewRequest,
    CreateProblemRequest,
    ProblemEditorItem,
    ProblemSignatureDefinition,
    ProblemSignaturePreview,
    CreateTestCaseRequest,
    InterviewResponse,
    ProblemListItem,
    ProblemTemplateItem,
    ReplaceProblemCatalogResponse,
    TestCaseListItem,
    UpdateProblemRequest,
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
    private readonly adminAuditBaseUrl = `${environment.apiBaseUrl}/admin/audit-logs`;

    createInterview(payload: CreateInterviewRequest): Observable<InterviewResponse> {
        return this.http
            .post<InterviewResponseDto>(this.baseUrl, payload)
            .pipe(map(interview => this.normalizeInterview(interview)));
    }

    getProblems(): Observable<ProblemListItem[]> {
        return this.http
            .get<ProblemSummaryResponseDto[]>(this.problemsBaseUrl)
            .pipe(map(problems => problems.map(problem => this.normalizeProblemSummary(problem))));
    }

    getProblemById(problemId: string): Observable<ProblemEditorItem> {
        return this.http
            .get<ProblemResponseDto>(`${this.problemsBaseUrl}/${problemId}`)
            .pipe(map(problem => this.normalizeProblemDetail(problem)));
    }

    getProblemTemplates(): Observable<ProblemTemplateItem[]> {
        return this.http
            .get<ProblemTemplateResponseDto[]>(`${this.problemsBaseUrl}/templates`)
            .pipe(map(templates => templates.map(template => this.normalizeProblemTemplate(template))));
    }

    getProblemSignaturePreview(signature: ProblemSignatureDefinition): Observable<ProblemSignaturePreview> {
        return this.http.post<ProblemSignaturePreview>(
            `${this.problemsBaseUrl}/signature-preview`,
            signature
        ).pipe(map(preview => this.normalizeSignaturePreview(preview as ProblemSignaturePreviewResponseDto)));
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
        return this.http.get<InterviewSessionResponseDto[]>(
            `${this.baseUrl}/${interviewId}/sessions`
        ).pipe(map(sessions => sessions.map(session => this.normalizeInterviewSession(session))));
    }

    getInterviewSessionDetails(sessionId: string): Observable<InterviewSessionDetails> {
        return this.http.get<InterviewSessionDetailsDto>(
            `${this.baseUrl}/sessions/${sessionId}`
        ).pipe(map(details => this.normalizeInterviewSessionDetails(details)));
    }

    getMyInterviews(): Observable<InterviewListItem[]> {
        return this.http
            .get<InterviewResponseDto[]>(this.baseUrl)
            .pipe(map(interviews => interviews.map(interview => this.normalizeInterviewListItem(interview))));
    }

    createProblem(payload: CreateProblemRequest): Observable<ProblemListItem> {
        return this.http
            .post<ProblemResponseDto>(this.problemsBaseUrl, payload)
            .pipe(map(problem => this.normalizeProblemResponse(problem)));
    }

    updateProblem(problemId: string, payload: UpdateProblemRequest): Observable<ProblemEditorItem> {
        return this.http
            .put<ProblemResponseDto>(`${this.problemsBaseUrl}/${problemId}`, payload)
            .pipe(map(problem => this.normalizeProblemDetail(problem)));
    }

    deleteProblem(problemId: string): Observable<void> {
        return this.http.delete<void>(`${this.problemsBaseUrl}/${problemId}`);
    }

    replaceCatalogWithStarterSet(): Observable<ReplaceProblemCatalogResponse> {
        return this.http.post<ReplaceProblemCatalogResponseDto>(
            `${this.problemsBaseUrl}/catalog/replace-with-starter-set`,
            {}
        ).pipe(map(result => this.normalizeReplaceProblemCatalogResponse(result)));
    }

    getRecentAdminAuditLogs(take = 20): Observable<AdminAuditLogListItem[]> {
        return this.http.get<AdminAuditLogResponseDto[]>(this.adminAuditBaseUrl, {
            params: { take },
        }).pipe(map(logs => logs.map(log => this.normalizeAdminAuditLog(log))));
    }

    getTestCases(problemId: string, includeHidden = true): Observable<TestCaseListItem[]> {
        return this.http.get<TestCaseResponseDto[]>(
            `${this.problemsBaseUrl}/${problemId}/testcases`,
            { params: { includeHidden } }
        ).pipe(map(testCases => testCases.map(testCase => this.normalizeTestCase(testCase))));
    }

    addTestCase(problemId: string, payload: CreateTestCaseRequest): Observable<TestCaseListItem> {
        return this.http.post<TestCaseResponseDto>(
            `${this.problemsBaseUrl}/${problemId}/testcases`,
            payload
        ).pipe(map(testCase => this.normalizeTestCase(testCase)));
    }

    private normalizeInterview(interview: InterviewResponseDto): InterviewResponse {
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
            problems: (interview.problems ?? []).map(problem => ({
                problemId: problem.problemId ?? '',
                title: problem.title ?? '',
                orderIndex: problem.orderIndex ?? 0,
                points: problem.points ?? 0,
            })),
        };
    }

    private normalizeInterviewListItem(interview: InterviewResponseDto): InterviewListItem {
        return this.normalizeInterview(interview);
    }

    private normalizeProblemSummary(problem: ProblemSummaryResponseDto): ProblemListItem {
        return {
            id: problem.id ?? '',
            title: problem.title ?? '',
            description: problem.description ?? '',
            difficulty: problem.difficulty ?? '',
            topic: problem.topic ?? '',
            constraintsText: problem.constraintsText ?? '',
            exampleInput: problem.exampleInput ?? '',
            exampleOutput: problem.exampleOutput ?? '',
            executionMode: problem.executionMode ?? 'stdin',
            isPublic: problem.isPublic ?? false,
            createdByUserId: problem.createdByUserId ?? '',
            createdAt: problem.createdAt ?? '',
            updatedAt: problem.updatedAt ?? '',
        };
    }

    private normalizeProblemResponse(problem: ProblemResponseDto): ProblemListItem {
        return {
            ...this.normalizeProblemSummary(problem),
        };
    }

    private normalizeProblemDetail(problem: ProblemResponseDto): ProblemEditorItem {
        return {
            ...this.normalizeProblemSummary(problem),
            signature: this.normalizeSignature(problem.signature),
            csharpStarterCode: problem.csharpStarterCode ?? '',
            pythonStarterCode: problem.pythonStarterCode ?? '',
            cppStarterCode: problem.cppStarterCode ?? '',
        };
    }

    private normalizeProblemTemplate(template: ProblemTemplateResponseDto): ProblemTemplateItem {
        return {
            key: template.key ?? '',
            name: template.name ?? '',
            summary: template.summary ?? '',
            title: template.title ?? '',
            description: template.description ?? '',
            difficulty: template.difficulty ?? '',
            topic: template.topic ?? '',
            constraintsText: template.constraintsText ?? '',
            exampleInput: template.exampleInput ?? '',
            exampleOutput: template.exampleOutput ?? '',
            executionMode: template.executionMode ?? 'stdin',
            signature: this.normalizeSignature(template.signature),
            csharpStarterCode: template.csharpStarterCode ?? '',
            pythonStarterCode: template.pythonStarterCode ?? '',
            cppStarterCode: template.cppStarterCode ?? '',
        };
    }

    private normalizeSignature(
        signature: ProblemSignatureDefinitionDto | null | undefined
    ): ProblemSignatureDefinition | null {
        if (!signature) {
            return null;
        }

        return {
            inputBindingMode: signature.inputBindingMode ?? '',
            csharpMethodName: signature.csharpMethodName ?? '',
            pythonMethodName: signature.pythonMethodName ?? '',
            cppMethodName: signature.cppMethodName ?? '',
            returnType: signature.returnType ?? '',
            parameters: (signature.parameters ?? []).map(parameter => ({
                name: parameter.name ?? '',
                type: parameter.type ?? '',
            })),
        };
    }

    private normalizeSignaturePreview(
        preview: ProblemSignaturePreviewResponseDto
    ): ProblemSignaturePreview {
        return {
            csharpStarterCode: preview.csharpStarterCode ?? '',
            pythonStarterCode: preview.pythonStarterCode ?? '',
            cppStarterCode: preview.cppStarterCode ?? '',
        };
    }

    private normalizeInterviewSession(
        session: InterviewSessionResponseDto
    ): InterviewSessionSummary {
        return {
            id: session.id ?? '',
            candidateId: session.candidateId ?? '',
            startedAt: session.startedAt ?? '',
            submittedAt: session.submittedAt ?? null,
            status: session.status ?? '',
            totalScore: session.totalScore ?? 0,
        };
    }

    private normalizeInterviewSessionDetails(
        details: InterviewSessionDetailsDto
    ): InterviewSessionDetails {
        return {
            session: {
                id: details.session?.id ?? '',
                interviewId: details.session?.interviewId ?? '',
                candidateId: details.session?.candidateId ?? '',
                startedAt: details.session?.startedAt ?? '',
                submittedAt: details.session?.submittedAt ?? null,
                status: details.session?.status ?? '',
                totalScore: details.session?.totalScore ?? 0,
            },
            submissions: (details.submissions ?? []).map(submission =>
                this.normalizeInterviewSubmission(submission)
            ),
        };
    }

    private normalizeInterviewSubmission(
        submission: SubmissionResponseDto
    ): InterviewSessionDetails['submissions'][number] {
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
            aiFeedback: submission.aiFeedback ?? null,
            aiFeedbackSource: submission.aiFeedbackSource ?? null,
            aiFeedbackStatus: (submission.aiFeedbackStatus as InterviewSessionDetails['submissions'][number]['aiFeedbackStatus']) ?? 'Pending',
            submittedAt: submission.submittedAt ?? '',
        };
    }

    private normalizeReplaceProblemCatalogResponse(
        response: ReplaceProblemCatalogResponseDto
    ): ReplaceProblemCatalogResponse {
        return {
            deletedProblemCount: response.deletedProblemCount ?? 0,
            deletedInterviewCount: response.deletedInterviewCount ?? 0,
            deletedSubmissionCount: response.deletedSubmissionCount ?? 0,
            createdProblemCount: response.createdProblemCount ?? 0,
            createdProblemTitles: response.createdProblemTitles ?? [],
        };
    }

    private normalizeAdminAuditLog(log: AdminAuditLogResponseDto): AdminAuditLogListItem {
        return {
            id: log.id ?? '',
            adminUserId: log.adminUserId ?? '',
            adminEmail: log.adminEmail ?? '',
            adminFullName: log.adminFullName ?? '',
            actionType: log.actionType ?? '',
            targetType: log.targetType ?? '',
            targetId: log.targetId ?? null,
            targetDisplayName: log.targetDisplayName ?? null,
            summary: log.summary ?? '',
            detailsJson: log.detailsJson ?? null,
            createdAt: log.createdAt ?? '',
        };
    }

    private normalizeTestCase(testCase: TestCaseResponseDto): TestCaseListItem {
        return {
            id: testCase.id ?? '',
            problemId: testCase.problemId ?? '',
            input: testCase.input ?? '',
            expectedOutput: testCase.expectedOutput ?? '',
            isHidden: testCase.isHidden ?? false,
            orderIndex: testCase.orderIndex ?? 0,
        };
    }
}
