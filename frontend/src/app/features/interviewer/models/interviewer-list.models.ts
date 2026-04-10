export interface InterviewListItem {
  id: string;
  title: string;
  positionName: string;
  description: string;
  durationMinutes: number;
  accessToken: string;
  isActive: boolean;
  interviewerId: string;
  createdAt: string;
  problems: {
    problemId: string;
    title: string;
    orderIndex: number;
    points: number;
  }[];
}