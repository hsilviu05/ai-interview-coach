import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { ProblemListItem } from '../../models/interviewer.models';

@Component({
  selector: 'app-add-problem-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './add-problem-form.html',
  styleUrl: './add-problem-form.scss',
})
export class AddProblemForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly interviewerApi = inject(InterviewerApi);

  @Input({ required: true }) interviewId!: string;
  @Output() problemAdded = new EventEmitter<void>();

  loadingProblems = false;
  submitting = false;
  errorMessage = '';
  successMessage = '';

  problems: ProblemListItem[] = [];

  form = this.fb.nonNullable.group({
    problemId: ['', [Validators.required]],
    orderIndex: [1, [Validators.required, Validators.min(1)]],
    points: [100, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.loadProblems();
  }

  loadProblems(): void {
    this.loadingProblems = true;
    this.errorMessage = '';

    this.interviewerApi.getProblems().subscribe({
      next: (problems) => {
        this.problems = problems;
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Failed to load problems.';
        this.loadingProblems = false;
      },
      complete: () => {
        this.loadingProblems = false;
      },
    });
  }

  submit(): void {
    if (this.form.invalid || !this.interviewId) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.interviewerApi.addProblemToInterview(this.interviewId, this.form.getRawValue()).subscribe({
      next: () => {
        this.successMessage = 'Problem added successfully.';
        this.problemAdded.emit();

        this.form.patchValue({
          orderIndex: this.form.controls.orderIndex.value + 1,
          points: 100,
        });
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Failed to add problem.';
        this.submitting = false;
      },
      complete: () => {
        this.submitting = false;
      },
    });
  }
}