import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
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
  private readonly authService = inject(AuthService);

  readonly loading = signal(true);
  readonly replacingCatalog = signal(false);
  readonly deletingProblemId = signal<string | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');
  readonly problems = signal<ProblemListItem[]>([]);
  readonly isAdmin = computed(() => this.authService.getRole() === 'Admin');

  ngOnInit(): void {
    this.loadProblems();
  }

  private loadProblems(showLoading = true): void {
    if (showLoading) {
      this.loading.set(true);
    }

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
    if (!this.isAdmin()) {
      return;
    }

    this.router.navigateByUrl('/interviewer/create-problem');
  }

  deleteProblem(problem: ProblemListItem): void {
    if (!this.isAdmin()) {
      return;
    }

    if (this.deletingProblemId() || this.replacingCatalog()) {
      return;
    }

    const confirmed = window.confirm(
      `Delete "${problem.title}"? This removes its test cases too. If the problem is already attached to interviews, the app will block the delete.`
    );

    if (!confirmed) {
      return;
    }

    this.deletingProblemId.set(problem.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.interviewerApi.deleteProblem(problem.id).subscribe({
      next: () => {
        this.problems.set(this.problems().filter(existingProblem => existingProblem.id !== problem.id));
        this.successMessage.set(`Deleted "${problem.title}".`);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to delete the problem.');
      },
      complete: () => {
        this.deletingProblemId.set(null);
      },
    });
  }

  replaceCatalogWithStarterSet(): void {
    if (!this.isAdmin()) {
      return;
    }

    if (this.replacingCatalog() || this.deletingProblemId()) {
      return;
    }

    const confirmed = window.confirm(
      'Replace the entire catalog with a fresh starter set? This clears existing problems, interviews, sessions, and submissions so you can start clean.'
    );

    if (!confirmed) {
      return;
    }

    this.replacingCatalog.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.interviewerApi.replaceCatalogWithStarterSet().subscribe({
      next: result => {
        const titles = result.createdProblemTitles.join(', ');
        this.successMessage.set(
          `Catalog replaced. Removed ${result.deletedProblemCount} problems, ${result.deletedInterviewCount} interviews, and ${result.deletedSubmissionCount} submissions. Added ${result.createdProblemCount} starter problems: ${titles}.`
        );
        this.loadProblems(false);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to replace the problem catalog.');
      },
      complete: () => {
        this.replacingCatalog.set(false);
      },
    });
  }

  getExecutionModeLabel(problem: ProblemListItem): string {
    return problem.executionMode === 'function'
      ? 'Function Signature'
      : 'Full Program';
  }
}
