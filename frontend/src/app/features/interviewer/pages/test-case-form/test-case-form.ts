import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { TestCaseListItem } from '../../models/interviewer.models';

@Component({
  selector: 'app-test-case-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './test-case-form.html',
  styleUrl: './test-case-form.scss',
})
export class TestCaseForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly interviewerApi = inject(InterviewerApi);

  @Input({ required: true }) problemId!: string;

  loading = false;
  loadingTestCases = false;
  errorMessage = '';
  successMessage = '';
  testCases: TestCaseListItem[] = [];

  form = this.fb.nonNullable.group({
    input: ['', [Validators.required]],
    expectedOutput: ['', [Validators.required]],
    isHidden: [false],
    orderIndex: [1, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.loadTestCases();
  }

  private loadTestCases(): void {
    if (!this.problemId) return;

    this.loadingTestCases = true;
    this.errorMessage = '';

    this.interviewerApi.getTestCases(this.problemId, true).subscribe({
      next: (testCases) => {
        const sorted = [...testCases].sort((a, b) => a.orderIndex - b.orderIndex);
        this.testCases = sorted;

        const nextOrderIndex =
          sorted.length > 0 ? Math.max(...sorted.map(tc => tc.orderIndex)) + 1 : 1;

        this.form.patchValue({ orderIndex: nextOrderIndex });
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Failed to load test cases.';
        this.loadingTestCases = false;
      },
      complete: () => {
        this.loadingTestCases = false;
      },
    });
  }

  submit(): void {
    if (!this.problemId) {
      this.errorMessage = 'Missing problem id.';
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.interviewerApi.addTestCase(this.problemId, this.form.getRawValue()).subscribe({
      next: () => {
        this.successMessage = 'Test case added successfully.';
        this.form.patchValue({
          input: '',
          expectedOutput: '',
          isHidden: false,
        });
        this.loadTestCases();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Failed to add test case.';
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      },
    });
  }
}