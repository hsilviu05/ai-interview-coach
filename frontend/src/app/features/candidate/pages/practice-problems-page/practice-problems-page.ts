import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CandidatePracticeProblemSummary } from '../../models/candidate-practice.models';
import { CandidateApi } from '../../services/candidate-api.service';
import { Navbar } from '../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-practice-problems-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './practice-problems-page.html',
  styleUrl: './practice-problems-page.scss',
})
export class PracticeProblemsPage implements OnInit {
  private readonly candidateApi = inject(CandidateApi);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly problems = signal<CandidatePracticeProblemSummary[]>([]);

  ngOnInit(): void {
    this.loadProblems();
  }

  openPracticeProblem(problemId: string): void {
    this.router.navigate(['/candidate/practice', problemId]);
  }

  private loadProblems(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.candidateApi.getPracticeProblems().subscribe({
      next: problems => {
        this.problems.set(problems);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load practice problems.');
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }
}
