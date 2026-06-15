import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { InterviewSessionSummary } from '../../models/interviewer-session.models';
import { InterviewListItem } from '../../models/interviewer-list.models';
import { Navbar } from '../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-interview-sessions-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './interview-sessions-page.html',
  styleUrl: './interview-sessions-page.scss',
})
export class InterviewSessionsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly interviewerApi = inject(InterviewerApi);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly sessions = signal<InterviewSessionSummary[]>([]);
  readonly interviewId = signal('');
  readonly interview = signal<InterviewListItem | null>(null);

  readonly totalPoints = computed(() =>
    this.interview()?.problems.reduce((sum, p) => sum + p.points, 0) ?? 0
  );

  ngOnInit(): void {
    const interviewId = this.route.snapshot.paramMap.get('interviewId');

    if (!interviewId) {
      this.errorMessage.set('Missing interview id.');
      this.loading.set(false);
      return;
    }

    this.interviewId.set(interviewId);
    this.loadData(interviewId);
  }

  private loadData(interviewId: string): void {
    this.loading.set(true);
    this.errorMessage.set('');

    forkJoin({
      sessions: this.interviewerApi.getInterviewSessions(interviewId),
      interview: this.interviewerApi.getInterviewById(interviewId),
    }).subscribe({
      next: ({ sessions, interview }) => {
        this.sessions.set(sessions);
        this.interview.set(interview);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load sessions.');
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  formatScore(session: InterviewSessionSummary): string {
    return `${session.totalScore} / ${this.totalPoints()} pts`;
  }

  openSession(sessionId: string): void {
    this.router.navigate(['/interviewer/sessions', sessionId]);
  }

  goBack(): void {
    this.router.navigateByUrl('/interviewer/dashboard');
  }
}