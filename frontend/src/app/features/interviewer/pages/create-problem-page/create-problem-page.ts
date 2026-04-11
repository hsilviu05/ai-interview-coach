import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { CreateProblemRequest, ProblemListItem } from '../../models/interviewer.models';
import { TestCaseForm } from '../..//pages/test-case-form/test-case-form';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { finalize } from 'rxjs';
@Component({
  selector: 'app-create-problem-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Navbar, TestCaseForm],
  templateUrl: './create-problem-page.html',
  styleUrl: './create-problem-page.scss',
})
export class CreateProblemPage {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly interviewerApi = inject(InterviewerApi);

  loading = false;
  errorMessage = '';
  successMessage = '';
  createdProblem: ProblemListItem | null = null;

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    difficulty: ['Easy', [Validators.required]],
    topic: ['', [Validators.required, Validators.maxLength(100)]],
    constraintsText: ['', [Validators.required]],
    exampleInput: ['', [Validators.required]],
    exampleOutput: ['', [Validators.required]],
    isPublic: [true, [Validators.required]],
  });

  submit(): void {
    if (this.loading) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.successMessage = '';
    this.errorMessage = '';
    this.createdProblem = null;

    const payload: CreateProblemRequest = this.form.getRawValue();

    this.interviewerApi.createProblem(payload).pipe(
      finalize(() => {
        this.loading = false;
      })
    ).subscribe({
      next: problem => {
        this.createdProblem = problem;
        this.successMessage = 'Problem created successfully. You can now add test cases.';
      },
      error: err => {
        this.errorMessage = err?.error?.message ?? 'Failed to create problem.';
      },
    });
  }

  goToProblems(): void {
    this.router.navigateByUrl('/interviewer/problems');
  }

  createAnother(): void {
    this.createdProblem = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.form.reset({
      title: '',
      description: '',
      difficulty: 'Easy',
      topic: '',
      constraintsText: '',
      exampleInput: '',
      exampleOutput: '',
      isPublic: true,
    });
  }
}