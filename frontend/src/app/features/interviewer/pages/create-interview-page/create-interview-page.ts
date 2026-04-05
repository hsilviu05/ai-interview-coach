import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AddProblemForm } from '../add-problem-form/add-problem-form';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { InterviewResponse } from '../../models/interviewer.models';

@Component({
  selector: 'app-create-interview-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AddProblemForm],
  templateUrl: './create-interview-page.html',
  styleUrl: './create-interview-page.scss',
})
export class CreateInterviewPage {
  private readonly fb = inject(FormBuilder);
  private readonly interviewerApi = inject(InterviewerApi);
  private readonly router = inject(Router);

  loading = false;
  errorMessage = '';
  createdInterview: InterviewResponse | null = null;
  lastProblemAddedAt: string | null = null;

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    positionName: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    durationMinutes: [60, [Validators.required, Validators.min(1), Validators.max(300)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.createdInterview = null;

    this.interviewerApi.createInterview(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.createdInterview = response;
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Failed to create interview.';
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      },
    });
  }

  goBack(): void {
    this.router.navigateByUrl('/interviewer/dashboard');
  }

  onProblemAdded(): void {
    this.lastProblemAddedAt = new Date().toLocaleTimeString();
  }
}