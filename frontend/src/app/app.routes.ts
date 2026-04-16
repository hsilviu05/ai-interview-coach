import { Routes } from '@angular/router';
import { LoginPage } from './features/auth/pages/login-page/login-page';
import { RegisterPage } from './features/auth/pages/register-page/register-page';
import { DashboardPage } from './features/interviewer/pages/dashboard-page/dashboard-page';
import { CreateInterviewPage } from './features/interviewer/pages/create-interview-page/create-interview-page';
import { InterviewSessionsPage } from './features/interviewer/pages/interview-sessions-page/interview-sessions-page';
import { InterviewSessionDetailsPage } from './features/interviewer/pages/interview-session-details-page/interview-session-details-page';
import { InterviewAccessPage } from './features/candidate/pages/interview-access-page/interview-access-page';
import { InterviewSolvePage } from './features/candidate/pages/interview-solve-page/interview-solve-page';
import { InterviewResultPage } from './features/candidate/pages/interview-result-page/interview-result-page';
import { PracticeProblemsPage } from './features/candidate/pages/practice-problems-page/practice-problems-page';
import { InterviewsListPage } from './features/interviewer/pages/interviews-list-page/interviews-list-page';
import { InterviewerProblemsListPage } from './features/interviewer/pages/interviewer-problems-list-page/interviewer-problems-list-page';
import { CreateProblemPage } from './features/interviewer/pages/create-problem-page/create-problem-page';
import { routeAccessPolicies } from './core/auth/access-policies';
import { authGuard } from './core/guards/auth-guard';
import { roleGuard } from './core/guards/role-guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  { path: 'login', component: LoginPage },
  { path: 'register', component: RegisterPage },

  {
    path: 'interviewer',
    canActivate: [authGuard, roleGuard],
    data: { accessPolicy: routeAccessPolicies.interviewerWorkspace },
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardPage },
      { path: 'interviews', component: InterviewsListPage },
      { path: 'create-interview', component: CreateInterviewPage },
      { path: ':interviewId/sessions', component: InterviewSessionsPage },
      { path: 'sessions/:sessionId', component: InterviewSessionDetailsPage },
      { path: 'problems', component: InterviewerProblemsListPage },
      {
        path: 'create-problem',
        component: CreateProblemPage,
        canActivate: [roleGuard],
        data: { accessPolicy: routeAccessPolicies.adminProblemManagement },
      },
    ],
  },

  {
    path: 'candidate',
    canActivate: [authGuard, roleGuard],
    data: { accessPolicy: routeAccessPolicies.candidateWorkspace },
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'access' },
      { path: 'access', component: InterviewAccessPage },
      { path: 'solve/:token', component: InterviewSolvePage },
      { path: 'practice', component: PracticeProblemsPage },
      { path: 'practice/:problemId', component: InterviewSolvePage, data: { mode: 'practice' } },
      { path: 'result/:sessionId', component: InterviewResultPage },
    ],
  },

  { path: '**', redirectTo: 'login' },
];
