import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { InterviewListItem } from '../../models/interviewer-list.models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage implements OnInit {
  private readonly router = inject(Router);
  private readonly interviewerApi = inject(InterviewerApi);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly interviews = signal<InterviewListItem[]>([]);

  ngOnInit(): void {
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
}