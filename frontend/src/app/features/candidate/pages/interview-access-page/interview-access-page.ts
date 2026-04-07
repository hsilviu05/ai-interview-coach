import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CandidateApi } from '../../services/candidate-api.service';
import { CandidateInterviewResponse } from '../../models/candidate-interview.models';
import { Navbar } from '../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-interview-access-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Navbar],
  templateUrl: './interview-access-page.html',
  styleUrl: './interview-access-page.scss',
})
export class InterviewAccessPage {
  private readonly fb = inject(FormBuilder);
  private readonly candidateApi = inject(CandidateApi);
  private readonly router = inject(Router);

  loading = false;
  errorMessage = '';
  interview: CandidateInterviewResponse | null = null;

  form = this.fb.nonNullable.group({
    token: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.interview = null;

    const token = this.form.controls.token.value.trim();

    this.candidateApi.getInterviewByToken(token).subscribe({
      next: (response) => {
        this.interview = response;
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Interview not found.';
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      },
    });
  }

  continueToInterview(): void {
    if (!this.interview) return;
    this.router.navigate(['/candidate/solve', this.interview.accessToken]);
  }
}