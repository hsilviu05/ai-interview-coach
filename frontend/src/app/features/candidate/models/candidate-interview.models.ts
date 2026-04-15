export interface CandidateInterviewVisibleTestCase {
  input: string;
  expectedOutput: string;
  orderIndex: number;
}

export interface CandidateInterviewProblemDto {
  problemId: string;
  title: string;
  description: string;
  difficulty: string;
  topic: string;
  constraintsText: string;
  exampleInput: string;
  exampleOutput: string;
  executionMode: string;
  csharpStarterCode: string;
  pythonStarterCode: string;
  cppStarterCode: string;
  visibleTestCases?: CandidateInterviewVisibleTestCase[];
  orderIndex: number;
  points: number;
}

export interface CandidateInterviewResponse {
  id: string;
  title: string;
  positionName: string;
  description: string;
  durationMinutes: number;
  accessToken: string;
  isActive: boolean;
  interviewerId: string;
  createdAt: string;
  problems: CandidateInterviewProblemDto[];
}

export interface InterviewSessionResponse {
  id: string;
  interviewId: string;
  candidateId: string;
  startedAt: string;
  submittedAt?: string | null;
  status: string;
  totalScore: number;
}
