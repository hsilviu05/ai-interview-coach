import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  CandidateApi,
} from '../../services/candidate-api.service';
import { CandidateInterviewProblemDto, CandidateInterviewResponse, InterviewSessionResponse } from '../../models/candidate-interview.models';
import { SubmissionResponse } from '../../models/candidate-submission.models';
import {
  SubmissionApi,
} from '../../services/submission-api.service';
import { Navbar } from '../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-interview-solve-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Navbar],
  templateUrl: './interview-solve-page.html',
  styleUrl: './interview-solve-page.scss',
})
export class InterviewSolvePage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly candidateApi = inject(CandidateApi);
  private readonly submissionApi = inject(SubmissionApi);

  readonly loading = signal(true);
  readonly startingSession = signal(false);
  readonly submitting = signal(false);
  readonly completing = signal(false);
  readonly isPracticeMode = signal(false);

  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly interview = signal<CandidateInterviewResponse | null>(null);
  readonly session = signal<InterviewSessionResponse | null>(null);
  readonly submissions = signal<SubmissionResponse[]>([]);
  readonly selectedProblemId = signal<string | null>(null);

  readonly selectedProblem = computed(() => {
    const interview = this.interview();
    const selectedProblemId = this.selectedProblemId();

    if (!interview || !selectedProblemId) {
      return null;
    }

    return (
      interview.problems.find(problem => problem.problemId === selectedProblemId) ?? null
    );
  });

  form = this.fb.nonNullable.group({
    language: ['csharp', [Validators.required]],
    sourceCode: [
      `using System;

var input = Console.In.ReadToEnd();

// TODO: Parse the test input from stdin and write only the expected output.
Console.WriteLine(input.Trim());`,
      [Validators.required, Validators.minLength(10)],
    ],
  });

  ngOnInit(): void {
    const practiceProblemId = this.route.snapshot.paramMap.get('problemId');
    if (practiceProblemId) {
      this.isPracticeMode.set(true);
      this.loadPracticeProblem(practiceProblemId);
      return;
    }

    const token = this.route.snapshot.paramMap.get('token');

    if (!token) {
      this.errorMessage.set('Missing interview token.');
      this.loading.set(false);
      return;
    }

    this.loadInterviewAndStartSession(token);
  }

  private loadPracticeProblem(problemId: string): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.candidateApi.getPracticeProblemById(problemId).subscribe({
      next: problem => {
        this.interview.set(this.buildPracticeInterview(problem));
        this.selectedProblemId.set(problem.problemId);
        this.loadPracticeSubmissions(problem.problemId);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load practice problem.');
        this.loading.set(false);
      },
    });
  }

  private loadInterviewAndStartSession(token: string): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.candidateApi.getInterviewByToken(token).subscribe({
      next: interview => {
        this.interview.set(interview);

        if (interview.problems.length > 0) {
          this.selectedProblemId.set(interview.problems[0].problemId);
        }

        this.startSession(token);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load interview.');
        this.loading.set(false);
      },
    });
  }

  private loadPracticeSubmissions(problemId: string): void {
    this.submissionApi.getMySubmissions().subscribe({
      next: submissions => {
        this.submissions.set(
          submissions.filter(submission =>
            submission.problemId === problemId && !submission.interviewSessionId
          )
        );
      },
      error: () => {
        this.submissions.set([]);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  private startSession(token: string): void {
    this.startingSession.set(true);

    this.candidateApi.startInterviewSession(token).subscribe({
      next: session => {
        this.session.set(session);
        this.loadSessionSubmissions(session.id);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to start session.');
        this.loading.set(false);
        this.startingSession.set(false);
      },
      complete: () => {
        this.startingSession.set(false);
      },
    });
  }

  private loadSessionSubmissions(sessionId: string): void {
    this.submissionApi.getByInterviewSession(sessionId).subscribe({
      next: submissions => {
        this.submissions.set(submissions);
      },
      error: () => {
        this.submissions.set([]);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  selectProblem(problem: CandidateInterviewProblemDto): void {
    this.selectedProblemId.set(problem.problemId);
    this.successMessage.set('');
    this.errorMessage.set('');
  }

  submitSolution(): void {
    const session = this.session();
    const problem = this.selectedProblem();

    if (!problem) {
      this.errorMessage.set('No selected problem.');
      return;
    }

    if (!this.isPracticeMode() && !session) {
      this.errorMessage.set('No active session or selected problem.');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.submissionApi.createSubmission({
      problemId: problem.problemId,
      language: this.form.controls.language.value,
      sourceCode: this.form.controls.sourceCode.value,
      ...(session ? { interviewSessionId: session.id } : {}),
    }).subscribe({
      next: submission => {
        this.submissions.set([submission, ...this.submissions()]);
        const diagnostic = submission.executionOutput?.trim();

        if (submission.status === 'Accepted') {
          this.successMessage.set(diagnostic || 'Submission accepted.');
          this.errorMessage.set('');
        } else {
          this.errorMessage.set(
            diagnostic
              ? `${submission.status}: ${diagnostic}`
              : `Submission finished with status: ${submission.status}.`
          );
          this.successMessage.set('');
        }
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to submit solution.');
      },
      complete: () => {
        this.submitting.set(false);
      },
    });
  }

  completeInterview(): void {
    if (this.isPracticeMode()) {
      this.router.navigateByUrl('/candidate/practice');
      return;
    }

    const session = this.session();

    if (!session) {
      this.errorMessage.set('No active session.');
      return;
    }

    this.completing.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.candidateApi.completeInterviewSession(session.id).subscribe({
      next: completedSession => {
        this.session.set(completedSession);
        this.router.navigate(['/candidate/result', completedSession.id]);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to complete interview.');
      },
      complete: () => {
        this.completing.set(false);
      },
    });
  }

  getLatestSubmissionForProblem(problemId: string): SubmissionResponse | null {
    return this.submissions().find(submission => submission.problemId === problemId) ?? null;
  }

  private buildPracticeInterview(problem: CandidateInterviewProblemDto): CandidateInterviewResponse {
    return {
      id: `practice-${problem.problemId}`,
      title: 'Practice Workspace',
      positionName: 'Standalone Problem Practice',
      description: 'Use this workspace to practice on individual coding problems.',
      durationMinutes: 0,
      accessToken: '',
      isActive: true,
      interviewerId: '',
      createdAt: new Date().toISOString(),
      problems: [problem],
    };
  }
}
