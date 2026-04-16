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
  executionMode: string;
  isPublic: boolean;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface ProblemTemplateItem {
  key: string;
  name: string;
  summary: string;
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
  csharpHarnessTemplate: string;
  pythonHarnessTemplate: string;
  cppHarnessTemplate: string;
}

export interface ReplaceProblemCatalogResponse {
  deletedProblemCount: number;
  deletedInterviewCount: number;
  deletedSubmissionCount: number;
  createdProblemCount: number;
  createdProblemTitles: string[];
}

export interface AdminAuditLogListItem {
  id: string;
  adminUserId: string;
  adminEmail: string;
  adminFullName: string;
  actionType: string;
  targetType: string;
  targetId?: string | null;
  targetDisplayName?: string | null;
  summary: string;
  detailsJson?: string | null;
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
  executionMode: string;
  csharpStarterCode: string;
  pythonStarterCode: string;
  cppStarterCode: string;
  csharpHarnessTemplate: string;
  pythonHarnessTemplate: string;
  cppHarnessTemplate: string;
  isPublic: boolean;
}

export interface InterviewProblemSelectionItem {
  problemId: string;
  title: string;
  selected: boolean;
  orderIndex: number;
  points: number;
}

export interface CreateInterviewFormValue {
  title: string;
  positionName: string;
  description: string;
  durationMinutes: number;
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
