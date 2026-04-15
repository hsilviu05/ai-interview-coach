export interface CandidatePracticeProblemSummary {
  id: string;
  title: string;
  description: string;
  difficulty: string;
  topic: string;
  constraintsText: string;
  exampleInput: string;
  exampleOutput: string;
  createdAt: string;
}

export interface CandidatePracticeProblemDetail extends CandidatePracticeProblemSummary {
  executionMode: string;
  csharpStarterCode: string;
  pythonStarterCode: string;
  cppStarterCode: string;
}
