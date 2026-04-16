import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { CreateProblemRequest, ProblemListItem, ProblemTemplateItem } from '../../models/interviewer.models';
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
export class CreateProblemPage implements OnInit {
  private static readonly stdinExecutionMode = 'stdin';
  private static readonly functionExecutionMode = 'function';

  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly interviewerApi = inject(InterviewerApi);

  loading = false;
  loadingTemplates = false;
  errorMessage = '';
  successMessage = '';
  templateErrorMessage = '';
  createdProblem: ProblemListItem | null = null;
  problemTemplates: ProblemTemplateItem[] = [];
  selectedTemplateKey = '';

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    difficulty: ['Easy', [Validators.required]],
    topic: ['', [Validators.required, Validators.maxLength(100)]],
    constraintsText: ['', [Validators.required]],
    exampleInput: ['', [Validators.required]],
    exampleOutput: ['', [Validators.required]],
    executionMode: [CreateProblemPage.stdinExecutionMode, [Validators.required]],
    csharpStarterCode: [''],
    pythonStarterCode: [''],
    cppStarterCode: [''],
    csharpHarnessTemplate: [''],
    pythonHarnessTemplate: [''],
    cppHarnessTemplate: [''],
    isPublic: [true, [Validators.required]],
  });

  ngOnInit(): void {
    this.loadProblemTemplates();
  }

  isFunctionSignatureMode(): boolean {
    return this.form.controls.executionMode.value === CreateProblemPage.functionExecutionMode;
  }

  loadTemplate(template: ProblemTemplateItem): void {
    this.selectedTemplateKey = template.key;
    this.form.patchValue({
      title: this.form.controls.title.value || template.title,
      description: this.form.controls.description.value || template.description,
      difficulty: this.form.controls.difficulty.value || template.difficulty,
      topic: this.form.controls.topic.value || template.topic,
      constraintsText: this.form.controls.constraintsText.value || template.constraintsText,
      exampleInput: this.form.controls.exampleInput.value || template.exampleInput,
      exampleOutput: this.form.controls.exampleOutput.value || template.exampleOutput,
      executionMode: template.executionMode,
      csharpStarterCode: template.csharpStarterCode,
      pythonStarterCode: template.pythonStarterCode,
      cppStarterCode: template.cppStarterCode,
      csharpHarnessTemplate: template.csharpHarnessTemplate,
      pythonHarnessTemplate: template.pythonHarnessTemplate,
      cppHarnessTemplate: template.cppHarnessTemplate,
    });
  }

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
    this.selectedTemplateKey = '';
    this.form.reset({
      title: '',
      description: '',
      difficulty: 'Easy',
      topic: '',
      constraintsText: '',
      exampleInput: '',
      exampleOutput: '',
      executionMode: CreateProblemPage.stdinExecutionMode,
      csharpStarterCode: '',
      pythonStarterCode: '',
      cppStarterCode: '',
      csharpHarnessTemplate: '',
      pythonHarnessTemplate: '',
      cppHarnessTemplate: '',
      isPublic: true,
    });
  }

  private loadProblemTemplates(): void {
    this.loadingTemplates = true;
    this.templateErrorMessage = '';

    this.interviewerApi.getProblemTemplates()
      .pipe(finalize(() => {
        this.loadingTemplates = false;
      }))
      .subscribe({
        next: templates => {
          this.problemTemplates = templates;
        },
        error: err => {
          this.templateErrorMessage = err?.error?.message ?? 'Failed to load shared problem templates.';
        },
      });
  }
}
