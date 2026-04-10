export interface CreateInterviewRequest {
  title: string;
  positionName: string;
  description: string;
  durationMinutes: number;
}

export interface InterviewProblemDto {
  problemId: string;
  title: string;
  orderIndex: number;
  points: number;
}

export interface InterviewResponse {
  id: string;
  title: string;
  positionName: string;
  description: string;
  durationMinutes: number;
  accessToken: string;
  isActive: boolean;
  interviewerId: string;
  createdAt: string;
  problems: InterviewProblemDto[];
}

export interface ProblemListItem {
  id: string;
  title: string;
  description: string;
  difficulty: string;
  topic: string;
  constraintsText: string;
  exampleInput: string;
  exampleOutput: string;
  createdByUserId: string;
  createdAt: string;
}

export interface AddProblemToInterviewRequest {
  problemId: string;
  orderIndex: number;
  points: number;
}
export interface CreateProblemRequest {
  title: string;
  description: string;
  difficulty: string;
  topic: string;
  constraintsText: string;
  exampleInput: string;
  exampleOutput: string;
  isPublic: boolean;
}

export interface TestCaseListItem {
  id: string;
  problemId: string;
  input: string;
  expectedOutput: string;
  isHidden: boolean;
  orderIndex: number;
}
export interface CreateTestCaseRequest {
  input: string;
  expectedOutput: string;
  isHidden: boolean;
  orderIndex: number;
}

export interface TestCaseListItem {
  id: string;
  problemId: string;
  input: string;
  expectedOutput: string;
  isHidden: boolean;
  orderIndex: number;
}