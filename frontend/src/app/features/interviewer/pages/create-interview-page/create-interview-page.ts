import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, forkJoin, of, switchMap } from 'rxjs';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import {
  CreateInterviewFormValue,
  InterviewProblemSelectionItem,
  InterviewResponse,
  ProblemListItem,
} from '../../models/interviewer.models';
import { InterviewerApi } from '../../services/interviewer-api.service';

@Component({
  selector: 'app-create-interview-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Navbar],
  templateUrl: './create-interview-page.html',
  styleUrl: './create-interview-page.scss',
})
export class CreateInterviewPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly interviewerApi = inject(InterviewerApi);
  private readonly router = inject(Router);

  loading = false;
  loadingProblems = false;
  errorMessage = '';
  successMessage = '';
  copiedToken = false;

  createdInterview: InterviewResponse | null = null;
  availableProblems: InterviewProblemSelectionItem[] = [];

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    positionName: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    durationMinutes: [60, [Validators.required, Validators.min(1), Validators.max(300)]],
  });

  ngOnInit(): void {
    this.loadProblems();
  }

  private loadProblems(): void {
    this.loadingProblems = true;
    this.errorMessage = '';

    this.interviewerApi.getProblems().pipe(
      finalize(() => {
        this.loadingProblems = false;
      })
    ).subscribe({
      next: (problems: ProblemListItem[]) => {
        this.availableProblems = problems.map((problem, index) => ({
          problemId: problem.id,
          title: problem.title,
          selected: false,
          orderIndex: index + 1,
          points: 100,
        }));
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Failed to load problems.';
      },
    });
  }

  toggleProblemSelection(problemId: string, checked: boolean): void {
    this.availableProblems = this.availableProblems.map(problem =>
      problem.problemId === problemId
        ? { ...problem, selected: checked }
        : problem
    );
  }

  updateProblemOrder(problemId: string, value: number): void {
    this.availableProblems = this.availableProblems.map(problem =>
      problem.problemId === problemId
        ? { ...problem, orderIndex: value }
        : problem
    );
  }

  updateProblemPoints(problemId: string, value: number): void {
    this.availableProblems = this.availableProblems.map(problem =>
      problem.problemId === problemId
        ? { ...problem, points: value }
        : problem
    );
  }

  submit(): void {
    if (this.loading) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const selectedProblems = this.availableProblems.filter(problem => problem.selected);

    if (selectedProblems.length === 0) {
      this.errorMessage = 'Select at least one problem for the interview.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.createdInterview = null;

    const payload: CreateInterviewFormValue = this.form.getRawValue();

    this.interviewerApi.createInterview(payload).pipe(
      switchMap((createdInterview) => {
        this.createdInterview = createdInterview;

        const addProblemRequests = selectedProblems.map(problem =>
          this.interviewerApi.addProblemToInterview(createdInterview.id, {
            problemId: problem.problemId,
            orderIndex: problem.orderIndex,
            points: problem.points,
          })
        );

        return forkJoin(addProblemRequests).pipe(
          switchMap(() => of(createdInterview))
        );
      }),
      finalize(() => {
        this.loading = false;
      })
    ).subscribe({
      next: (createdInterview) => {
        this.createdInterview = createdInterview;
        this.successMessage = 'Interview created successfully and selected problems were attached.';
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Failed to create interview.';
      },
    });
  }

  goBack(): void {
    this.router.navigateByUrl('/interviewer/dashboard');
  }

  goToSessions(): void {
    if (!this.createdInterview) return;
    this.router.navigate(['/interviewer', this.createdInterview.id, 'sessions']);
  }

  copyToken(): void {
    if (!this.createdInterview) return;
    navigator.clipboard.writeText(this.createdInterview.accessToken).then(() => {
      this.copiedToken = true;
      setTimeout(() => {
        this.copiedToken = false;
      }, 2000);
    });
  }

  createAnother(): void {
    this.createdInterview = null;
    this.successMessage = '';
    this.errorMessage = '';
    this.copiedToken = false;

    this.form.reset({
      title: '',
      positionName: '',
      description: '',
      durationMinutes: 60,
    });

    this.availableProblems = this.availableProblems.map((problem, index) => ({
      ...problem,
      selected: false,
      orderIndex: index + 1,
      points: 100,
    }));
  }
}