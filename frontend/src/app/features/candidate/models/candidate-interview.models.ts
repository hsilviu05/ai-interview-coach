export interface CandidateInterviewProblemDto {
  problemId: string;
  title: string;
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