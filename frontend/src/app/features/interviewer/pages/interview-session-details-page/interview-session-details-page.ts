import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
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
  private readonly interviewerApi = inject(InterviewerApi);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly details = signal<InterviewSessionDetails | null>(null);

  ngOnInit(): void {
    const sessionId = this.route.snapshot.paramMap.get('sessionId');

    if (!sessionId) {
      this.errorMessage.set('Missing session id.');
      this.loading.set(false);
      return;
    }

    this.loadDetails(sessionId);
  }

  private loadDetails(sessionId: string): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.interviewerApi.getInterviewSessionDetails(sessionId).subscribe({
      next: details => {
        this.details.set(details);
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
}
