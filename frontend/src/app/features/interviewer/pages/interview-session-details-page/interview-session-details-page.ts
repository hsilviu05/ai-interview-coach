import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { InterviewSessionDetails } from '../../models/interviewer-session.models';
import { Navbar } from '../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-interview-session-details-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './interview-session-details-page.html',
  styleUrl: './interview-session-details-page.scss',
})
export class InterviewSessionDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly interviewerApi = inject(InterviewerApi);
  private feedbackRefreshTimeoutId: ReturnType<typeof setTimeout> | null = null;

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly details = signal<InterviewSessionDetails | null>(null);

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.stopFeedbackPolling();
    });
  }

  ngOnInit(): void {
    const sessionId = this.route.snapshot.paramMap.get('sessionId');

    if (!sessionId) {
      this.errorMessage.set('Missing session id.');
      this.loading.set(false);
      return;
    }

    this.loadDetails(sessionId);
  }

  private loadDetails(sessionId: string, showLoading = true): void {
    if (showLoading) {
      this.loading.set(true);
      this.errorMessage.set('');
    }

    this.interviewerApi.getInterviewSessionDetails(sessionId).subscribe({
      next: details => {
        this.setDetails(details);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load session details.');
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  goBack(): void {
    const interviewId = this.details()?.session.interviewId;

    if (interviewId) {
      this.router.navigate(['/interviewer', interviewId, 'sessions']);
      return;
    }

    this.router.navigateByUrl('/interviewer/dashboard');
  }

  formatLanguage(language: string): string {
    return language === 'cpp'
      ? 'C++'
      : language === 'python'
        ? 'Python'
        : language === 'csharp'
          ? 'C#'
          : language;
  }

  isAiFeedbackReady(details: InterviewSessionDetails['submissions'][number]): boolean {
    return details.aiFeedbackStatus === 'Ready' && !!details.aiFeedback?.trim();
  }

  isAiFeedbackPending(details: InterviewSessionDetails['submissions'][number]): boolean {
    return details.aiFeedbackStatus === 'Pending';
  }

  isAiFeedbackFailed(details: InterviewSessionDetails['submissions'][number]): boolean {
    return details.aiFeedbackStatus === 'Failed';
  }

  private setDetails(details: InterviewSessionDetails): void {
    this.details.set(details);

    if (
      details.submissions.some(submission => submission.aiFeedbackStatus === 'Pending')
    ) {
      this.ensureFeedbackPolling(details.session.id);
      return;
    }

    this.stopFeedbackPolling();
  }

  private ensureFeedbackPolling(sessionId: string, delayMs = 1500): void {
    if (this.feedbackRefreshTimeoutId !== null) {
      return;
    }

    this.feedbackRefreshTimeoutId = setTimeout(() => {
      this.feedbackRefreshTimeoutId = null;
      this.loadDetails(sessionId, false);
    }, delayMs);
  }

  private stopFeedbackPolling(): void {
    if (this.feedbackRefreshTimeoutId === null) {
      return;
    }

    clearTimeout(this.feedbackRefreshTimeoutId);
    this.feedbackRefreshTimeoutId = null;
  }
}
