export interface RequestProblemHint {
  level: number;
  language: string;
  sourceCode: string;
}

export interface ProblemHintResponse {
  level: number;
  content: string;
  source: string;
}
