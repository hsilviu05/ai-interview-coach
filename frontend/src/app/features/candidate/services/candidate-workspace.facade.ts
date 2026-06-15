import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import {
  CandidateInterviewProblemDto,
  CandidateInterviewResponse,
  InterviewSessionResponse,
} from '../models/candidate-interview.models';
import {
  SubmissionFeedbackStatus,
  SubmissionResponse,
} from '../models/candidate-submission.models';
import { ProblemHintResponse } from '../models/candidate-problem-hint.models';
import { CandidateApi } from './candidate-api.service';
import { CodeDraftStorageService } from './code-draft-storage.service';
import { SubmissionApi } from './submission-api.service';

export type SubmissionLanguage = 'csharp' | 'python' | 'cpp';

export interface SubmissionLanguageOption {
  value: SubmissionLanguage;
  label: string;
}

@Injectable()
export class CandidateWorkspaceFacade {
  private static readonly functionExecutionMode = 'function';
  private static readonly defaultLanguage: SubmissionLanguage = 'csharp';
  private static readonly maxHintLevel = 3;
  private static readonly languageOptionsInternal: SubmissionLanguageOption[] = [
    { value: 'csharp', label: 'C#' },
    { value: 'python', label: 'Python' },
    { value: 'cpp', label: 'C++' },
  ];
  private static readonly defaultStarterSourceByLanguage: Record<SubmissionLanguage, string> = {
    csharp: `using System;

static string Solve(string rawInput)
{
    var lines = rawInput
        .Split(new[] { '\\r', '\\n' }, StringSplitOptions.RemoveEmptyEntries);

    // TODO: Parse lines or tokens based on the problem statement.
    // Return only the final answer as a string.
    return rawInput.Trim();
}

var input = Console.In.ReadToEnd();
Console.WriteLine(Solve(input));`,
    python: `import sys

def solve(raw_input: str) -> str:
    lines = [line for line in raw_input.splitlines() if line.strip()]

    # TODO: Parse lines or tokens based on the problem statement.
    # Return only the final answer as a string.
    return raw_input.strip()


if __name__ == "__main__":
    print(solve(sys.stdin.read()))`,
    cpp: `#include <algorithm>
#include <cctype>
#include <iostream>
#include <iterator>
#include <string>

std::string TrimCopy(const std::string& value) {
    const auto first = std::find_if_not(value.begin(), value.end(), [](unsigned char ch) {
        return std::isspace(ch);
    });
    const auto last = std::find_if_not(value.rbegin(), value.rend(), [](unsigned char ch) {
        return std::isspace(ch);
    }).base();

    return first >= last ? std::string() : std::string(first, last);
}

std::string Solve(const std::string& rawInput) {
    // TODO: Parse lines or tokens based on the problem statement.
    // Return only the final answer as a string.
    return TrimCopy(rawInput);
}

int main() {
    std::string input(
        (std::istreambuf_iterator<char>(std::cin)),
        std::istreambuf_iterator<char>());

    std::cout << Solve(input);
    return 0;
}`,
  };

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly candidateApi = inject(CandidateApi);
  private readonly submissionApi = inject(SubmissionApi);
  private readonly codeDraftStorage = inject(CodeDraftStorageService);

  private isHydratingDraft = false;
  private activeDraftLanguage: SubmissionLanguage =
    CandidateWorkspaceFacade.defaultLanguage;
  private feedbackRefreshTimeoutId: ReturnType<typeof setTimeout> | null = null;

  readonly loading = signal(true);
  readonly startingSession = signal(false);
  readonly submitting = signal(false);
  readonly completing = signal(false);
  readonly resettingProblem = signal(false);
  readonly resettingAllProblems = signal(false);
  readonly requestingHintLevel = signal<number | null>(null);
  readonly isPracticeMode = signal(false);
  readonly languageOptions = CandidateWorkspaceFacade.languageOptionsInternal;

  readonly errorMessage = signal('');
  readonly successMessage = signal('');
  readonly hintErrorMessage = signal('');

  readonly interview = signal<CandidateInterviewResponse | null>(null);
  readonly session = signal<InterviewSessionResponse | null>(null);
  readonly submissions = signal<SubmissionResponse[]>([]);
  readonly practiceHintsByProblemId = signal<Record<string, ProblemHintResponse[]>>({});
  readonly selectedProblemId = signal<string | null>(null);
  readonly lastSavedAt = signal<string | null>(null);
  readonly completedProblemIds = computed(
    () =>
      new Set(
        this.submissions()
          .filter(submission => submission.status === 'Accepted')
          .map(submission => submission.problemId)
      )
  );
  readonly allProblemsCompleted = computed(() => {
    const interview = this.interview();

    return (
      !!interview &&
      interview.problems.length > 0 &&
      interview.problems.every(problem =>
        this.completedProblemIds().has(problem.problemId)
      )
    );
  });
  readonly latestFeedbackSubmission = computed(
    () => {
      const selectedProblemId = this.selectedProblemId();
      const submissions = this.submissions();

      if (selectedProblemId) {
        return (
          submissions.find(submission => submission.problemId === selectedProblemId) ??
          null
        );
      }

      return submissions[0] ?? null;
    }
  );

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
      interview.problems.find(problem => problem.problemId === selectedProblemId) ??
      null
    );
  });

  readonly currentProblemHints = computed(() => {
    const problemId = this.selectedProblemId();

    if (!problemId) {
      return [];
    }

    return [...(this.practiceHintsByProblemId()[problemId] ?? [])].sort(
      (left, right) => left.level - right.level
    );
  });

  readonly nextHintLevel = computed(() => {
    if (!this.isPracticeMode() || !this.selectedProblem()) {
      return null;
    }

    const unlockedLevels = new Set(
      this.currentProblemHints().map(hint => hint.level)
    );

    for (
      let level = 1;
      level <= CandidateWorkspaceFacade.maxHintLevel;
      level += 1
    ) {
      if (!unlockedLevels.has(level)) {
        return level;
      }
    }

    return null;
  });

  readonly form = this.fb.nonNullable.group({
    language: [
      CandidateWorkspaceFacade.defaultLanguage,
      [Validators.required],
    ],
    sourceCode: [
      CandidateWorkspaceFacade.getStarterSourceCode(
        CandidateWorkspaceFacade.defaultLanguage
      ),
      [Validators.required, Validators.minLength(10)],
    ],
  });

  constructor() {
    this.form.controls.language.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(language => {
        if (this.loading() || this.isHydratingDraft) {
          return;
        }

        if (!CandidateWorkspaceFacade.isSupportedLanguage(language)) {
          return;
        }

        const problemId = this.selectedProblemId();
        if (!problemId) {
          this.activeDraftLanguage = language;
          return;
        }

        const currentSource = this.form.controls.sourceCode.value;
        const selectedProblem = this.selectedProblem();
        const draftScope = this.buildDraftScope(problemId);
        const now = new Date().toISOString();

        this.codeDraftStorage.saveDraft(draftScope, {
          language: this.activeDraftLanguage,
          sourceCode: currentSource,
          updatedAt: now,
        });

        const targetDraft = this.codeDraftStorage.getDraft(draftScope, language);
        const sanitizedTargetDraftSource =
          CandidateWorkspaceFacade.sanitizeStoredSourceCodeForLanguage(
            selectedProblem,
            language,
            targetDraft?.sourceCode
          );

        if (targetDraft && sanitizedTargetDraftSource === null) {
          this.codeDraftStorage.clearDraft(draftScope, language);
        }

        const starterSourceCode =
          CandidateWorkspaceFacade.getStarterSourceCodeForProblem(
            selectedProblem,
            language
          );
        const latestSubmission = this.getLatestSubmissionForProblemByLanguage(
          problemId,
          language
        );
        const nextSourceCode =
          sanitizedTargetDraftSource ??
          CandidateWorkspaceFacade.sanitizeStoredSourceCodeForLanguage(
            selectedProblem,
            language,
            latestSubmission?.sourceCode
          ) ??
          starterSourceCode;

        this.isHydratingDraft = true;
        this.form.controls.sourceCode.setValue(nextSourceCode, {
          emitEvent: false,
        });
        this.lastSavedAt.set(
          sanitizedTargetDraftSource ? targetDraft?.updatedAt ?? null : null
        );
        this.isHydratingDraft = false;
        this.activeDraftLanguage = language;
      });

    this.form.valueChanges
      .pipe(
        debounceTime(500),
        map(() => JSON.stringify(this.form.getRawValue())),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        if (this.loading() || this.isHydratingDraft) {
          return;
        }

        this.persistCurrentDraft();
      });

    this.destroyRef.onDestroy(() => {
      this.persistCurrentDraft();
      this.stopFeedbackPolling();
    });
  }

  initializeFromRoute(): void {
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

    this.submissionApi
      .createSubmission({
        problemId: problem.problemId,
        language: this.form.controls.language.value,
        sourceCode: this.form.controls.sourceCode.value,
        ...(session ? { interviewSessionId: session.id } : {}),
      })
      .subscribe({
        next: submission => {
          this.setSubmissions([submission, ...this.submissions()]);
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
          this.errorMessage.set(
            err?.error?.message ?? 'Failed to submit solution.'
          );
          this.submitting.set(false);
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
        this.errorMessage.set(
          err?.error?.message ?? 'Failed to complete interview.'
        );
        this.completing.set(false);
      },
      complete: () => {
        this.completing.set(false);
      },
    });
  }

  resetCurrentProblem(): void {
    const problem = this.selectedProblem() ?? this.getResetTargetProblem();

    if (!problem) {
      return;
    }

    const confirmed = window.confirm(
      this.isPracticeMode()
        ? `Reset ${problem.title}? This removes your saved submissions and local draft for this practice problem.`
        : `Reset ${problem.title}? This removes your saved submissions and local draft for this interview problem.`
    );

    if (!confirmed) {
      return;
    }

    this.resettingProblem.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.submissionApi
      .resetProblem(problem.problemId, this.session()?.id)
      .subscribe({
        next: () => {
          this.codeDraftStorage.clearDraft(this.buildDraftScope(problem.problemId));
          this.clearHintsForProblem(problem.problemId);
          this.setSubmissions(
            this.submissions().filter(submission => {
              if (submission.problemId !== problem.problemId) {
                return true;
              }

              if (this.isPracticeMode()) {
                return !!submission.interviewSessionId;
              }

              return submission.interviewSessionId !== this.session()?.id;
            })
          );
          this.selectedProblemId.set(problem.problemId);
          this.hydrateDraftForSelectedProblem();
          this.successMessage.set(
            `${problem.title} was reset. You can start fresh now.`
          );
        },
        error: err => {
          this.errorMessage.set(
            err?.error?.message ?? 'Failed to reset the problem.'
          );
          this.resettingProblem.set(false);
        },
        complete: () => {
          this.resettingProblem.set(false);
        },
      });
  }

  resetAllProblems(): void {
    if (this.isPracticeMode()) {
      this.resetCurrentProblem();
      return;
    }

    const session = this.session();
    const interview = this.interview();

    if (!session || !interview) {
      this.errorMessage.set('No active session.');
      return;
    }

    const confirmed = window.confirm(
      'Reset all problems in this interview? This removes every saved submission and local draft for this session.'
    );

    if (!confirmed) {
      return;
    }

    this.resettingAllProblems.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.submissionApi.resetInterviewSession(session.id).subscribe({
      next: () => {
        for (const problem of interview.problems) {
          this.codeDraftStorage.clearDraft(this.buildDraftScope(problem.problemId));
          this.clearHintsForProblem(problem.problemId);
        }

        this.setSubmissions(
          this.submissions().filter(
            submission => submission.interviewSessionId !== session.id
          )
        );
        this.focusFirstOpenProblem();
        this.successMessage.set(
          'All problems were reset. The session is ready for a fresh pass.'
        );
      },
      error: err => {
        this.errorMessage.set(
          err?.error?.message ?? 'Failed to reset the problems.'
        );
        this.resettingAllProblems.set(false);
      },
      complete: () => {
        this.resettingAllProblems.set(false);
      },
    });
  }

  getLatestSubmissionForProblem(problemId: string): SubmissionResponse | null {
    return (
      this.submissions().find(submission => submission.problemId === problemId) ??
      null
    );
  }

  getProblemTitle(problemId: string): string {
    return (
      this.interview()?.problems.find(problem => problem.problemId === problemId)
        ?.title ?? 'Latest Submission'
    );
  }

  getLanguageHelperText(): string {
    const selectedProblem = this.selectedProblem();
    const isFunctionSignatureProblem =
      selectedProblem?.executionMode ===
      CandidateWorkspaceFacade.functionExecutionMode;

    if (isFunctionSignatureProblem) {
      return this.form.controls.language.value === 'python'
        ? 'Implement only the exact Python Solution method shown in the starter code. A hidden runner handles input parsing and output formatting.'
        : this.form.controls.language.value === 'cpp'
          ? 'Implement only the exact C++ Solution method shown in the starter code. A hidden runner handles input parsing and output formatting.'
          : 'Implement only the exact C# Solution method shown in the starter code. A hidden runner handles input parsing and output formatting.';
    }

    return this.form.controls.language.value === 'python'
      ? 'Submissions run as standalone Python 3 programs. Read from stdin and write the final answer to stdout.'
      : this.form.controls.language.value === 'cpp'
        ? 'Submissions run as standalone C++17 console programs. Read from stdin and write the final answer to stdout.'
        : 'Submissions run as standalone C# console programs. Read from stdin and write the final answer to stdout.';
  }

  requestNextHint(): void {
    const nextHintLevel = this.nextHintLevel();

    if (nextHintLevel === null) {
      return;
    }

    this.requestHint(nextHintLevel);
  }

  goToPracticeProblems(): void {
    this.router.navigateByUrl('/candidate/practice');
  }

  isAiFeedbackReady(submission: SubmissionResponse | null): boolean {
    return (
      submission?.aiFeedbackStatus === 'Ready' &&
      !!submission.aiFeedback?.trim()
    );
  }

  isAiFeedbackPending(submission: SubmissionResponse | null): boolean {
    return submission?.aiFeedbackStatus === 'Pending';
  }

  isAiFeedbackFailed(submission: SubmissionResponse | null): boolean {
    return submission?.aiFeedbackStatus === 'Failed';
  }

  private loadPracticeProblem(problemId: string): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');
    this.hintErrorMessage.set('');

    this.candidateApi.getPracticeProblemById(problemId).subscribe({
      next: problem => {
        this.interview.set(this.buildPracticeInterview(problem));
        this.selectedProblemId.set(problem.problemId);
        this.loadPracticeSubmissions(problem.problemId);
      },
      error: err => {
        this.errorMessage.set(
          err?.error?.message ?? 'Failed to load practice problem.'
        );
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
        this.setSubmissions(
          submissions.filter(
            submission =>
              submission.problemId === problemId &&
              !submission.interviewSessionId
          )
        );
      },
      error: () => {
        this.setSubmissions([]);
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
    this.hintErrorMessage.set('');

    this.candidateApi.startInterviewSession(token).subscribe({
      next: session => {
        this.session.set(session);
        this.loadSessionSubmissions(session.id);
      },
      error: err => {
        const isAlreadyCompleted =
          err?.status === 409 ||
          String(err?.error?.message ?? '').toLowerCase().includes('already completed');

        this.errorMessage.set(
          isAlreadyCompleted
            ? 'This interview session has already been completed. Contact your interviewer if this is unexpected.'
            : (err?.error?.message ?? 'Failed to start session.')
        );
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
        this.setSubmissions(submissions);
      },
      error: () => {
        this.setSubmissions([]);
        this.focusFirstOpenProblem();
        this.loading.set(false);
      },
      complete: () => {
        this.focusFirstOpenProblem();
        this.loading.set(false);
      },
    });
  }

  private hydrateDraftForSelectedProblem(): void {
    const problemId = this.selectedProblemId();

    if (!problemId) {
      return;
    }

    this.hintErrorMessage.set('');

    const draft = this.codeDraftStorage.getDraft(this.buildDraftScope(problemId));
    const latestSubmission = this.getLatestSubmissionForProblem(problemId);
    const selectedProblem =
      this.interview()?.problems.find(problem => problem.problemId === problemId) ??
      null;
    const nextLanguage = CandidateWorkspaceFacade.normalizeLanguage(
      draft?.language ??
        latestSubmission?.language ??
        CandidateWorkspaceFacade.defaultLanguage
    );
    const draftScope = this.buildDraftScope(problemId);
    const languageDraft = this.codeDraftStorage.getDraft(
      this.buildDraftScope(problemId),
      nextLanguage
    );
    const latestLanguageSubmission =
      this.getLatestSubmissionForProblemByLanguage(problemId, nextLanguage);
    const sanitizedDraftSource =
      CandidateWorkspaceFacade.sanitizeStoredSourceCodeForLanguage(
        selectedProblem,
        nextLanguage,
        languageDraft?.sourceCode
      );
    const sanitizedLatestSubmissionSource =
      CandidateWorkspaceFacade.sanitizeStoredSourceCodeForLanguage(
        selectedProblem,
        nextLanguage,
        latestLanguageSubmission?.sourceCode
      );

    if (languageDraft && sanitizedDraftSource === null) {
      this.codeDraftStorage.clearDraft(draftScope, nextLanguage);
    }

    const nextSourceCode =
      sanitizedDraftSource ??
      sanitizedLatestSubmissionSource ??
      CandidateWorkspaceFacade.getStarterSourceCodeForProblem(
        selectedProblem,
        nextLanguage
      );

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
    this.lastSavedAt.set(
      sanitizedDraftSource
        ? languageDraft?.updatedAt ?? draft?.updatedAt ?? null
        : null
    );
    this.isHydratingDraft = false;
    this.activeDraftLanguage = nextLanguage;
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

  private requestHint(level: number): void {
    const problem = this.selectedProblem();

    if (!problem || !this.isPracticeMode()) {
      return;
    }

    const existingHint = this.currentProblemHints().find(hint => hint.level === level);
    if (existingHint) {
      return;
    }

    this.requestingHintLevel.set(level);
    this.hintErrorMessage.set('');

    this.candidateApi
      .requestPracticeHint(problem.problemId, {
        level,
        language: this.form.controls.language.value,
        sourceCode: this.form.controls.sourceCode.value,
      })
      .subscribe({
        next: hint => {
          const existingHints = this.practiceHintsByProblemId()[problem.problemId] ?? [];
          const nextHints = [...existingHints.filter(existing => existing.level !== hint.level), hint]
            .sort((left, right) => left.level - right.level);

          this.practiceHintsByProblemId.update(hintsByProblemId => ({
            ...hintsByProblemId,
            [problem.problemId]: nextHints,
          }));
        },
        error: err => {
          this.requestingHintLevel.set(null);
          this.hintErrorMessage.set(
            err?.error?.message ?? 'Unable to generate a hint right now.'
          );
        },
        complete: () => {
          this.requestingHintLevel.set(null);
        },
      });
  }

  private clearHintsForProblem(problemId: string): void {
    this.practiceHintsByProblemId.update(hintsByProblemId => {
      if (!(problemId in hintsByProblemId)) {
        return hintsByProblemId;
      }

      const nextHintsByProblemId = { ...hintsByProblemId };
      delete nextHintsByProblemId[problemId];
      return nextHintsByProblemId;
    });

    if (this.selectedProblemId() === problemId) {
      this.hintErrorMessage.set('');
    }
  }

  private getLatestSubmissionForProblemByLanguage(
    problemId: string,
    language: SubmissionLanguage
  ): SubmissionResponse | null {
    return (
      this.submissions().find(
        submission =>
          submission.problemId === problemId && submission.language === language
      ) ?? null
    );
  }

  private getResetTargetProblem(): CandidateInterviewProblemDto | null {
    if (!this.isPracticeMode()) {
      return null;
    }

    return this.interview()?.problems[0] ?? null;
  }

  private setSubmissions(submissions: SubmissionResponse[]): void {
    this.submissions.set(submissions);

    if (this.hasPendingAiFeedback(submissions)) {
      this.ensureFeedbackPolling();
      return;
    }

    this.stopFeedbackPolling();
  }

  private hasPendingAiFeedback(submissions: SubmissionResponse[]): boolean {
    return submissions.some(
      submission => submission.aiFeedbackStatus === 'Pending'
    );
  }

  private ensureFeedbackPolling(delayMs = 1500): void {
    if (!this.hasPendingAiFeedback(this.submissions())) {
      this.stopFeedbackPolling();
      return;
    }

    if (this.feedbackRefreshTimeoutId !== null) {
      return;
    }

    this.feedbackRefreshTimeoutId = setTimeout(() => {
      this.feedbackRefreshTimeoutId = null;
      this.refreshPendingFeedback();
    }, delayMs);
  }

  private stopFeedbackPolling(): void {
    if (this.feedbackRefreshTimeoutId === null) {
      return;
    }

    clearTimeout(this.feedbackRefreshTimeoutId);
    this.feedbackRefreshTimeoutId = null;
  }

  private refreshPendingFeedback(): void {
    if (!this.hasPendingAiFeedback(this.submissions())) {
      this.stopFeedbackPolling();
      return;
    }

    if (this.isPracticeMode()) {
      const practiceProblemId =
        this.interview()?.problems[0]?.problemId ?? this.selectedProblemId();

      if (!practiceProblemId) {
        this.stopFeedbackPolling();
        return;
      }

      this.submissionApi.getMySubmissions().subscribe({
        next: submissions => {
          this.setSubmissions(
            submissions.filter(
              submission =>
                submission.problemId === practiceProblemId &&
                !submission.interviewSessionId
            )
          );
        },
        error: () => {
          this.ensureFeedbackPolling(4000);
        },
      });
      return;
    }

    const sessionId = this.session()?.id;

    if (!sessionId) {
      this.stopFeedbackPolling();
      return;
    }

    this.submissionApi.getByInterviewSession(sessionId).subscribe({
      next: submissions => {
        this.setSubmissions(submissions);
      },
      error: () => {
        this.ensureFeedbackPolling(4000);
      },
    });
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
    this.successMessage.set(
      `Problem accepted. Moved to ${nextProblem.orderIndex}. ${nextProblem.title}.`
    );
  }

  private clearWorkspaceSelection(): void {
    this.selectedProblemId.set(null);
    this.lastSavedAt.set(null);
    this.isHydratingDraft = true;
    this.form.reset(
      {
        language: CandidateWorkspaceFacade.defaultLanguage,
        sourceCode: CandidateWorkspaceFacade.getStarterSourceCode(
          CandidateWorkspaceFacade.defaultLanguage
        ),
      },
      { emitEvent: false }
    );
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.isHydratingDraft = false;
    this.activeDraftLanguage = CandidateWorkspaceFacade.defaultLanguage;
  }

  private findNextOpenProblem(
    completedProblemId: string
  ): CandidateInterviewProblemDto | null {
    const problems = this.interview()?.problems ?? [];
    const completedProblemIds = this.completedProblemIds();
    const currentProblemIndex = problems.findIndex(
      problem => problem.problemId === completedProblemId
    );

    if (currentProblemIndex === -1) {
      return null;
    }

    for (
      let problemIndex = currentProblemIndex + 1;
      problemIndex < problems.length;
      problemIndex += 1
    ) {
      const problem = problems[problemIndex];

      if (!completedProblemIds.has(problem.problemId)) {
        return problem;
      }
    }

    return null;
  }

  private buildPracticeInterview(
    problem: CandidateInterviewProblemDto
  ): CandidateInterviewResponse {
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

  private static normalizeLanguage(language: string): SubmissionLanguage {
    return CandidateWorkspaceFacade.isSupportedLanguage(language)
      ? language
      : CandidateWorkspaceFacade.defaultLanguage;
  }

  private static isSupportedLanguage(
    language: string
  ): language is SubmissionLanguage {
    return CandidateWorkspaceFacade.languageOptionsInternal.some(
      option => option.value === language
    );
  }

  private static getStarterSourceCode(language: SubmissionLanguage): string {
    return CandidateWorkspaceFacade.defaultStarterSourceByLanguage[language];
  }

  private static getStarterSourceCodeForProblem(
    problem: CandidateInterviewProblemDto | null,
    language: SubmissionLanguage
  ): string {
    if (problem?.executionMode === CandidateWorkspaceFacade.functionExecutionMode) {
      const configuredStarter =
        language === 'python'
          ? problem.pythonStarterCode
          : language === 'cpp'
            ? problem.cppStarterCode
            : problem.csharpStarterCode;

      if (configuredStarter?.trim()) {
        return configuredStarter;
      }
    }

    return CandidateWorkspaceFacade.getStarterSourceCode(language);
  }

  private static sanitizeStoredSourceCodeForLanguage(
    problem: CandidateInterviewProblemDto | null,
    language: SubmissionLanguage,
    sourceCode: string | null | undefined
  ): string | null {
    if (!sourceCode) {
      return null;
    }

    const expectedStarter = CandidateWorkspaceFacade.getStarterSourceCodeForProblem(
      problem,
      language
    );
    if (sourceCode === expectedStarter) {
      return sourceCode;
    }

    for (const option of CandidateWorkspaceFacade.languageOptionsInternal) {
      if (option.value === language) {
        continue;
      }

      const otherStarter = CandidateWorkspaceFacade.getStarterSourceCodeForProblem(
        problem,
        option.value
      );
      if (sourceCode === otherStarter) {
        return null;
      }
    }

    const detectedSourceLanguage =
      CandidateWorkspaceFacade.detectSourceLanguage(sourceCode);
    if (detectedSourceLanguage && detectedSourceLanguage !== language) {
      return null;
    }

    return sourceCode;
  }

  private static detectSourceLanguage(
    sourceCode: string
  ): SubmissionLanguage | null {
    const normalizedSource = sourceCode.trim();

    if (!normalizedSource) {
      return null;
    }

    if (
      normalizedSource.includes('class Solution:') ||
      normalizedSource.includes('def ')
    ) {
      return 'python';
    }

    if (
      normalizedSource.includes('#include') ||
      normalizedSource.includes('using namespace std') ||
      normalizedSource.includes('std::') ||
      normalizedSource.includes('vector<')
    ) {
      return 'cpp';
    }

    if (
      normalizedSource.includes('public class Solution') ||
      normalizedSource.includes('using System') ||
      normalizedSource.includes('Console.')
    ) {
      return 'csharp';
    }

    return null;
  }
}
