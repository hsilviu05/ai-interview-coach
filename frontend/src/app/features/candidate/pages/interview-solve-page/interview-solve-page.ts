import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import {
  CandidateApi,
} from '../../services/candidate-api.service';
import { CandidateInterviewProblemDto, CandidateInterviewResponse, InterviewSessionResponse } from '../../models/candidate-interview.models';
import { SubmissionResponse } from '../../models/candidate-submission.models';
import {
  SubmissionApi,
} from '../../services/submission-api.service';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { CodeEditor } from '../../../../shared/components/code-editor/code-editor';
import { CodeDraftStorageService } from '../../services/code-draft-storage.service';

@Component({
  selector: 'app-interview-solve-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Navbar, CodeEditor],
  templateUrl: './interview-solve-page.html',
  styleUrl: './interview-solve-page.scss',
})
export class InterviewSolvePage implements OnInit, OnDestroy {
  private static readonly defaultSourceCode = `using System;

var input = Console.In.ReadToEnd();

// TODO: Parse the test input from stdin and write only the expected output.
Console.WriteLine(input.Trim());`;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly candidateApi = inject(CandidateApi);
  private readonly submissionApi = inject(SubmissionApi);
  private readonly codeDraftStorage = inject(CodeDraftStorageService);

  private isHydratingDraft = false;

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
  readonly lastSavedAt = signal<string | null>(null);
  readonly completedProblemIds = computed(() =>
    new Set(
      this.submissions()
        .filter(submission => submission.status === 'Accepted')
        .map(submission => submission.problemId)
    )
  );
  readonly allProblemsCompleted = computed(() => {
    const interview = this.interview();

    return !!interview &&
      interview.problems.length > 0 &&
      interview.problems.every(problem => this.completedProblemIds().has(problem.problemId));
  });

  readonly autosaveStatus = computed(() => {
    const lastSavedAt = this.lastSavedAt();

    if (!lastSavedAt) {
      return 'Draft autosaves locally in this browser for each problem.';
    }

    return `Autosaved at ${new Date(lastSavedAt).toLocaleTimeString([], {
      hour: 'numeric',
      minute: '2-digit',
      second: '2-digit',
    })}`;
  });

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
    sourceCode: [InterviewSolvePage.defaultSourceCode, [Validators.required, Validators.minLength(10)]],
  });

  constructor() {
    this.form.valueChanges.pipe(
      debounceTime(500),
      map(() => JSON.stringify(this.form.getRawValue())),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      if (this.loading() || this.isHydratingDraft) {
        return;
      }

      this.persistCurrentDraft();
    });
  }

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

  ngOnDestroy(): void {
    this.persistCurrentDraft();
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
        this.focusFirstOpenProblem();
        this.loading.set(false);
      },
      complete: () => {
        this.focusFirstOpenProblem();
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
        this.focusFirstOpenProblem();
        this.loading.set(false);
      },
      complete: () => {
        this.focusFirstOpenProblem();
        this.loading.set(false);
      },
    });
  }

  selectProblem(problem: CandidateInterviewProblemDto): void {
    if (this.selectedProblemId() === problem.problemId) {
      return;
    }

    this.persistCurrentDraft();
    this.selectedProblemId.set(problem.problemId);
    this.successMessage.set('');
    this.errorMessage.set('');
    this.hydrateDraftForSelectedProblem();
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
        this.persistCurrentDraft();

        if (submission.status === 'Accepted') {
          this.advanceAfterAcceptedSubmission(problem.problemId);
          this.errorMessage.set('');
        } else {
          const diagnostic = submission.executionOutput?.trim();
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

  goToPracticeProblems(): void {
    this.router.navigateByUrl('/candidate/practice');
  }

  private hydrateDraftForSelectedProblem(): void {
    const problemId = this.selectedProblemId();

    if (!problemId) {
      return;
    }

    const draft = this.codeDraftStorage.getDraft(this.buildDraftScope(problemId));
    const latestSubmission = this.getLatestSubmissionForProblem(problemId);
    const nextLanguage = draft?.language ?? latestSubmission?.language ?? 'csharp';
    const nextSourceCode =
      draft?.sourceCode ??
      latestSubmission?.sourceCode ??
      InterviewSolvePage.defaultSourceCode;

    this.isHydratingDraft = true;
    this.form.reset(
      {
        language: nextLanguage,
        sourceCode: nextSourceCode,
      },
      { emitEvent: false }
    );
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.lastSavedAt.set(draft?.updatedAt ?? null);
    this.isHydratingDraft = false;
  }

  private persistCurrentDraft(): void {
    const problemId = this.selectedProblemId();

    if (!problemId) {
      return;
    }

    const now = new Date().toISOString();

    this.codeDraftStorage.saveDraft(this.buildDraftScope(problemId), {
      ...this.form.getRawValue(),
      updatedAt: now,
    });
    this.lastSavedAt.set(now);
  }

  private buildDraftScope(problemId: string): string {
    if (this.isPracticeMode()) {
      return `practice:${problemId}`;
    }

    return `interview:${this.session()?.id ?? this.interview()?.id ?? 'pending'}:${problemId}`;
  }

  private focusFirstOpenProblem(): void {
    const firstOpenProblem = this.interview()?.problems.find(
      problem => !this.completedProblemIds().has(problem.problemId)
    );

    if (!firstOpenProblem) {
      this.clearWorkspaceSelection();
      return;
    }

    this.selectedProblemId.set(firstOpenProblem.problemId);
    this.hydrateDraftForSelectedProblem();
  }

  private advanceAfterAcceptedSubmission(completedProblemId: string): void {
    const nextProblem = this.findNextOpenProblem(completedProblemId);

    if (!nextProblem) {
      this.clearWorkspaceSelection();
      this.successMessage.set(
        this.isPracticeMode()
          ? 'Problem accepted. Practice complete.'
          : 'Problem accepted. No remaining open problems.'
      );
      return;
    }

    this.selectedProblemId.set(nextProblem.problemId);
    this.hydrateDraftForSelectedProblem();
    this.successMessage.set(`Problem accepted. Moved to ${nextProblem.orderIndex}. ${nextProblem.title}.`);
  }

  private clearWorkspaceSelection(): void {
    this.selectedProblemId.set(null);
    this.lastSavedAt.set(null);
    this.isHydratingDraft = true;
    this.form.reset(
      {
        language: 'csharp',
        sourceCode: InterviewSolvePage.defaultSourceCode,
      },
      { emitEvent: false }
    );
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.isHydratingDraft = false;
  }

  private findNextOpenProblem(completedProblemId: string): CandidateInterviewProblemDto | null {
    const problems = this.interview()?.problems ?? [];
    const completedProblemIds = this.completedProblemIds();
    const currentProblemIndex = problems.findIndex(problem => problem.problemId === completedProblemId);

    if (currentProblemIndex === -1) {
      return null;
    }

    for (let problemIndex = currentProblemIndex + 1; problemIndex < problems.length; problemIndex += 1) {
      const problem = problems[problemIndex];

      if (!completedProblemIds.has(problem.problemId)) {
        return problem;
      }
    }

    return null;
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
