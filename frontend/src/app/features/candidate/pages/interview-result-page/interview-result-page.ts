import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import {
  SubmissionApi,
} from '../../services/submission-api.service';
import { SubmissionResponse } from '../../models/candidate-submission.models';

@Component({
  selector: 'app-interview-result-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './interview-result-page.html',
  styleUrl: './interview-result-page.scss',
})
export class InterviewResultPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly submissionApi = inject(SubmissionApi);
  private feedbackRefreshTimeoutId: ReturnType<typeof setTimeout> | null = null;

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly sessionId = signal('');
  readonly submissions = signal<SubmissionResponse[]>([]);

  readonly acceptedCount = signal(0);

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

    this.sessionId.set(sessionId);
    this.loadSubmissions(sessionId);
  }

  private loadSubmissions(sessionId: string, showLoading = true): void {
    if (showLoading) {
      this.loading.set(true);
      this.errorMessage.set('');
    }

    this.submissionApi.getByInterviewSession(sessionId).subscribe({
      next: submissions => {
        this.setSubmissions(submissions);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load interview results.');
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  goToAccessPage(): void {
    this.router.navigateByUrl('/candidate/access');
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

  isAiFeedbackReady(submission: SubmissionResponse): boolean {
    return submission.aiFeedbackStatus === 'Ready' && !!submission.aiFeedback?.trim();
  }

  isAiFeedbackPending(submission: SubmissionResponse): boolean {
    return submission.aiFeedbackStatus === 'Pending';
  }

  isAiFeedbackFailed(submission: SubmissionResponse): boolean {
    return submission.aiFeedbackStatus === 'Failed';
  }

  private setSubmissions(submissions: SubmissionResponse[]): void {
    this.submissions.set(submissions);
    this.acceptedCount.set(
      submissions.filter(submission => submission.status === 'Accepted').length
    );

    if (submissions.some(submission => submission.aiFeedbackStatus === 'Pending')) {
      this.ensureFeedbackPolling();
      return;
    }

    this.stopFeedbackPolling();
  }

  private ensureFeedbackPolling(delayMs = 1500): void {
    if (this.feedbackRefreshTimeoutId !== null || !this.sessionId()) {
      return;
    }

    this.feedbackRefreshTimeoutId = setTimeout(() => {
      this.feedbackRefreshTimeoutId = null;
      this.loadSubmissions(this.sessionId(), false);
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
