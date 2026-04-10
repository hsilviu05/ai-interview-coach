import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { ProblemListItem } from '../../models/interviewer.models';
import { InterviewerApi } from '../../services/interviewer-api.service';

@Component({
  selector: 'app-interviewer-problems-list-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './interviewer-problems-list-page.html',
  styleUrl: './interviewer-problems-list-page.scss',
})
export class InterviewerProblemsListPage implements OnInit {
  private readonly router = inject(Router);
  private readonly interviewerApi = inject(InterviewerApi);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly problems = signal<ProblemListItem[]>([]);

  ngOnInit(): void {
    this.loadProblems();
  }

  private loadProblems(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.interviewerApi.getProblems().subscribe({
      next: problems => {
        this.problems.set(problems);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load problems.');
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  goToCreateProblem(): void {
    this.router.navigateByUrl('/interviewer/create-problem');
  }
}