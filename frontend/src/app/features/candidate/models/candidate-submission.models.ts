
export interface CreateSubmissionRequest {
  problemId: string;
  interviewSessionId?: string | null;
  language: string;
  sourceCode: string;
}

export interface SubmissionResponse {
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
  executionOutput?: string | null;
  aiFeedback?: string | null;
  aiFeedbackSource?: string | null;
  submittedAt: string;
}
