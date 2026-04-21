import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { CodeEditor } from '../../../../shared/components/code-editor/code-editor';
import { CandidateWorkspaceFacade } from '../../services/candidate-workspace.facade';

@Component({
  selector: 'app-interview-solve-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Navbar, CodeEditor],
  templateUrl: './interview-solve-page.html',
  styleUrl: './interview-solve-page.scss',
  providers: [CandidateWorkspaceFacade],
})
export class InterviewSolvePage implements OnInit {
  private readonly workspace = inject(CandidateWorkspaceFacade);

  readonly form = this.workspace.form;
  readonly loading = this.workspace.loading;
  readonly startingSession = this.workspace.startingSession;
  readonly submitting = this.workspace.submitting;
  readonly completing = this.workspace.completing;
  readonly resettingProblem = this.workspace.resettingProblem;
  readonly resettingAllProblems = this.workspace.resettingAllProblems;
  readonly requestingHintLevel = this.workspace.requestingHintLevel;
  readonly isPracticeMode = this.workspace.isPracticeMode;
  readonly languageOptions = this.workspace.languageOptions;
  readonly errorMessage = this.workspace.errorMessage;
  readonly successMessage = this.workspace.successMessage;
  readonly hintErrorMessage = this.workspace.hintErrorMessage;
  readonly interview = this.workspace.interview;
  readonly session = this.workspace.session;
  readonly submissions = this.workspace.submissions;
  readonly selectedProblemId = this.workspace.selectedProblemId;
  readonly lastSavedAt = this.workspace.lastSavedAt;
  readonly completedProblemIds = this.workspace.completedProblemIds;
  readonly allProblemsCompleted = this.workspace.allProblemsCompleted;
  readonly latestFeedbackSubmission = this.workspace.latestFeedbackSubmission;
  readonly autosaveStatus = this.workspace.autosaveStatus;
  readonly selectedProblem = this.workspace.selectedProblem;
  readonly currentProblemHints = this.workspace.currentProblemHints;
  readonly nextHintLevel = this.workspace.nextHintLevel;

  ngOnInit(): void {
    this.workspace.initializeFromRoute();
  }

  selectProblem = this.workspace.selectProblem.bind(this.workspace);
  submitSolution = this.workspace.submitSolution.bind(this.workspace);
  completeInterview = this.workspace.completeInterview.bind(this.workspace);
  resetCurrentProblem = this.workspace.resetCurrentProblem.bind(this.workspace);
  resetAllProblems = this.workspace.resetAllProblems.bind(this.workspace);
  requestNextHint = this.workspace.requestNextHint.bind(this.workspace);
  getLatestSubmissionForProblem =
    this.workspace.getLatestSubmissionForProblem.bind(this.workspace);
  getProblemTitle = this.workspace.getProblemTitle.bind(this.workspace);
  getLanguageHelperText =
    this.workspace.getLanguageHelperText.bind(this.workspace);
  isAiFeedbackReady = this.workspace.isAiFeedbackReady.bind(this.workspace);
  isAiFeedbackPending = this.workspace.isAiFeedbackPending.bind(this.workspace);
  isAiFeedbackFailed = this.workspace.isAiFeedbackFailed.bind(this.workspace);
  goToPracticeProblems =
    this.workspace.goToPracticeProblems.bind(this.workspace);
}
