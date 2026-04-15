import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
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
  private readonly submissionApi = inject(SubmissionApi);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly sessionId = signal('');
  readonly submissions = signal<SubmissionResponse[]>([]);

  readonly acceptedCount = signal(0);

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

  private loadSubmissions(sessionId: string): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.submissionApi.getByInterviewSession(sessionId).subscribe({
      next: submissions => {
        this.submissions.set(submissions);
        this.acceptedCount.set(
          submissions.filter(submission => submission.status === 'Accepted').length
        );
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
}
