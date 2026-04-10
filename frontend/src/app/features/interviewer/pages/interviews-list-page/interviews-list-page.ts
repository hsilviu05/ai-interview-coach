import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { InterviewListItem } from '../../models/interviewer-list.models';

@Component({
  selector: 'app-interviews-list-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './interviews-list-page.html',
  styleUrls: ['./interviews-list-page.scss'],
})
export class InterviewsListPage implements OnInit {
  private readonly router = inject(Router);
  private readonly interviewerApi = inject(InterviewerApi);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly interviews = signal<InterviewListItem[]>([]);
  readonly copiedTokenFor = signal<string | null>(null);

  ngOnInit(): void {
    this.loadInterviews();
  }

  private loadInterviews(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.interviewerApi.getMyInterviews().subscribe({
      next: interviews => {
        this.interviews.set(interviews);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load interviews.');
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  goToCreateInterview(): void {
    this.router.navigateByUrl('/interviewer/create-interview');
  }

  goToSessions(interviewId: string): void {
    this.router.navigate(['/interviewer', interviewId, 'sessions']);
  }

  copyToken(interviewId: string, token: string): void {
    navigator.clipboard.writeText(token).then(() => {
      this.copiedTokenFor.set(interviewId);

      setTimeout(() => {
        this.copiedTokenFor.set(null);
      }, 1500);
    });
  }
}