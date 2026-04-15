export interface InterviewSessionSummary {
  id: string;
  candidateId: string;
  startedAt: string;
  submittedAt?: string | null;
  status: string;
  totalScore: number;
}

export interface InterviewSubmissionDetails {
  id: string;
  candidateId: string;
  problemId: string;
  interviewSessionId?: string | null;
  language: string;
  sourceCode: string;
  status: string;
  passedTests: number;
  totalTests: number;
  executionTimeMs?: number | null;
  memoryKb?: number | null;
  aiFeedback?: string | null;
  aiFeedbackSource?: string | null;
  submittedAt: string;
}

export interface InterviewSessionInfo {
  id: string;
  interviewId: string;
  candidateId: string;
  startedAt: string;
  submittedAt?: string | null;
  status: string;
  totalScore: number;
}

export interface InterviewSessionDetails {
  session: InterviewSessionInfo;
  submissions: InterviewSubmissionDetails[];
}
