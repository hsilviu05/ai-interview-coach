import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { InterviewSessionSummary } from '../../models/interviewer-session.models';
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

  ngOnInit(): void {
    const interviewId = this.route.snapshot.paramMap.get('interviewId');

    if (!interviewId) {
      this.errorMessage.set('Missing interview id.');
      this.loading.set(false);
      return;
    }

    this.interviewId.set(interviewId);
    this.loadSessions(interviewId);
  }

  private loadSessions(interviewId: string): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.interviewerApi.getInterviewSessions(interviewId).subscribe({
      next: sessions => {
        this.sessions.set(sessions);
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

  openSession(sessionId: string): void {
    this.router.navigate(['/interviewer/sessions', sessionId]);
  }

  goBack(): void {
    this.router.navigateByUrl('/interviewer/dashboard');
  }
}