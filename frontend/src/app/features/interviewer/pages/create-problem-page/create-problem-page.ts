import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, debounceTime } from 'rxjs';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import {
  CreateProblemRequest,
  ProblemEditorItem,
  ProblemListItem,
  ProblemSignatureDefinition,
  ProblemSignatureParameter,
  ProblemSignaturePreview,
  ProblemTemplateItem,
  UpdateProblemRequest,
} from '../../models/interviewer.models';
import { TestCaseForm } from '../../pages/test-case-form/test-case-form';
import { InterviewerApi } from '../../services/interviewer-api.service';

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
  private static readonly defaultSignatureType = 'string';
  private static readonly defaultInputBindingMode = 'json_object';

  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly interviewerApi = inject(InterviewerApi);

  readonly signatureInputBindingOptions = [
    { value: 'json_object', label: 'JSON object fields' },
    { value: 'raw_text', label: 'Raw stdin text' },
  ];

  readonly signatureTypeOptions = [
    { value: 'string', label: 'string' },
    { value: 'int', label: 'int' },
    { value: 'bool', label: 'bool' },
    { value: 'int_array', label: 'int[] / List[int] / vector<int>' },
    { value: 'string_array', label: 'string[] / List[str] / vector<string>' },
  ];

  loading = false;
  loadingProblem = !!this.route.snapshot.paramMap.get('problemId');
  loadingTemplates = true;
  loadingPreview = false;
  errorMessage = '';
  successMessage = '';
  templateErrorMessage = '';
  previewErrorMessage = '';
  createdProblem: ProblemListItem | null = null;
  problemTemplates: ProblemTemplateItem[] = [];
  selectedTemplateKey = '';
  signaturePreview: ProblemSignaturePreview | null = null;
  editingProblemId: string | null = this.route.snapshot.paramMap.get('problemId');

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    difficulty: ['Easy', [Validators.required]],
    topic: ['', [Validators.required, Validators.maxLength(100)]],
    constraintsText: ['', [Validators.required]],
    exampleInput: ['', [Validators.required]],
    exampleOutput: ['', [Validators.required]],
    executionMode: [CreateProblemPage.functionExecutionMode, [Validators.required]],
    signature: this.fb.nonNullable.group({
      inputBindingMode: [
        CreateProblemPage.defaultInputBindingMode,
        [Validators.required],
      ],
      csharpMethodName: ['', [Validators.required, Validators.maxLength(100)]],
      pythonMethodName: ['', [Validators.required, Validators.maxLength(100)]],
      cppMethodName: ['', [Validators.required, Validators.maxLength(100)]],
      returnType: [CreateProblemPage.defaultSignatureType, [Validators.required]],
      parameters: this.fb.array([this.buildSignatureParameterGroup()]),
    }),
    csharpStarterCode: [''],
    pythonStarterCode: [''],
    cppStarterCode: [''],
    isPublic: [true, [Validators.required]],
  });

  ngOnInit(): void {
    this.loadProblemTemplates();
    this.setupSignaturePreview();
    this.loadProblemIfEditing();
  }

  get signatureParameters(): FormArray {
    return this.form.controls.signature.controls.parameters;
  }

  isFunctionSignatureMode(): boolean {
    return (
      this.form.controls.executionMode.value ===
      CreateProblemPage.functionExecutionMode
    );
  }

  isEditMode(): boolean {
    return this.editingProblemId !== null;
  }

  get pageBadge(): string {
    return this.isEditMode() ? 'Edit Problem' : 'Create Problem';
  }

  get pageTitle(): string {
    return this.isEditMode()
      ? 'Update a reusable coding problem.'
      : 'Create a reusable coding problem.';
  }

  get pageDescription(): string {
    return this.isEditMode()
      ? 'Edit the shared problem definition with the same signature-driven generator used during creation.'
      : 'Define the problem once and reuse it in multiple interviews.';
  }

  get submitButtonLabel(): string {
    if (this.loading) {
      return this.isEditMode() ? 'Saving Changes...' : 'Creating Problem...';
    }

    return this.isEditMode() ? 'Save Changes' : 'Create Problem';
  }

  get resultTitle(): string {
    return this.isEditMode() ? 'Problem Updated' : 'Problem Created';
  }

  get secondaryResultActionLabel(): string {
    return this.isEditMode() ? 'Create New Problem' : 'Create Another';
  }

  get problemIdForTestCases(): string | null {
    return this.createdProblem?.id ?? this.editingProblemId;
  }

  addSignatureParameter(): void {
    this.signatureParameters.push(this.buildSignatureParameterGroup());
  }

  removeSignatureParameter(index: number): void {
    if (this.signatureParameters.length <= 1) {
      return;
    }

    this.signatureParameters.removeAt(index);
    this.refreshSignaturePreview();
  }

  loadTemplate(template: ProblemTemplateItem): void {
    this.selectedTemplateKey = template.key;
    this.form.patchValue({
      title: this.form.controls.title.value || template.title,
      description: this.form.controls.description.value || template.description,
      difficulty: this.form.controls.difficulty.value || template.difficulty,
      topic: this.form.controls.topic.value || template.topic,
      constraintsText:
        this.form.controls.constraintsText.value || template.constraintsText,
      exampleInput:
        this.form.controls.exampleInput.value || template.exampleInput,
      exampleOutput:
        this.form.controls.exampleOutput.value || template.exampleOutput,
      executionMode: template.executionMode,
      csharpStarterCode: template.csharpStarterCode,
      pythonStarterCode: template.pythonStarterCode,
      cppStarterCode: template.cppStarterCode,
    });

    this.resetSignatureForm(template.signature);
    this.signaturePreview = {
      csharpStarterCode: template.csharpStarterCode,
      pythonStarterCode: template.pythonStarterCode,
      cppStarterCode: template.cppStarterCode,
    };
    this.refreshSignaturePreview();
  }

  submit(): void {
    if (this.loading || this.loadingProblem) {
      return;
    }

    if (this.form.invalid || (this.isFunctionSignatureMode() && !this.buildSignaturePayload())) {
      this.form.markAllAsTouched();
      this.signatureParameters.controls.forEach(control => control.markAllAsTouched());
      return;
    }

    this.loading = true;
    this.successMessage = '';
    this.errorMessage = '';
    this.createdProblem = null;

    const rawValue = this.form.getRawValue();
    const isFunctionMode = this.isFunctionSignatureMode();
    const payload: CreateProblemRequest = {
      title: rawValue.title,
      description: rawValue.description,
      difficulty: rawValue.difficulty,
      topic: rawValue.topic,
      constraintsText: rawValue.constraintsText,
      exampleInput: rawValue.exampleInput,
      exampleOutput: rawValue.exampleOutput,
      executionMode: rawValue.executionMode,
      signature: isFunctionMode ? this.buildSignaturePayload() ?? undefined : undefined,
      csharpStarterCode: isFunctionMode ? '' : rawValue.csharpStarterCode,
      pythonStarterCode: isFunctionMode ? '' : rawValue.pythonStarterCode,
      cppStarterCode: isFunctionMode ? '' : rawValue.cppStarterCode,
      isPublic: rawValue.isPublic,
    };

    const saveRequest = this.isEditMode()
      ? this.interviewerApi.updateProblem(this.editingProblemId!, {
          id: this.editingProblemId!,
          ...payload,
        } as UpdateProblemRequest)
      : this.interviewerApi.createProblem(payload);

    saveRequest
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: problem => {
          this.createdProblem = problem;
          this.successMessage = this.isEditMode()
            ? 'Problem updated successfully. The signature preview and generated starters are now in sync with the saved problem.'
            : 'Problem created successfully. You can now add test cases.';
        },
        error: err => {
          this.errorMessage = err?.error?.message ?? (this.isEditMode()
            ? 'Failed to update problem.'
            : 'Failed to create problem.');
        },
      });
  }

  goToProblems(): void {
    this.router.navigateByUrl('/interviewer/problems');
  }

  createAnother(): void {
    if (this.isEditMode()) {
      this.router.navigateByUrl('/interviewer/create-problem');
      return;
    }

    this.createdProblem = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.selectedTemplateKey = '';
    this.previewErrorMessage = '';
    this.signaturePreview = null;
    this.form.reset({
      title: '',
      description: '',
      difficulty: 'Easy',
      topic: '',
      constraintsText: '',
      exampleInput: '',
      exampleOutput: '',
      executionMode: CreateProblemPage.functionExecutionMode,
      csharpStarterCode: '',
      pythonStarterCode: '',
      cppStarterCode: '',
      isPublic: true,
    });
    this.resetSignatureForm();
  }

  private buildSignatureParameterGroup(
    value?: Partial<ProblemSignatureParameter>
  ) {
    return this.fb.nonNullable.group({
      name: [
        value?.name ?? '',
        [Validators.required, Validators.maxLength(100)],
      ],
      type: [
        value?.type ?? CreateProblemPage.defaultSignatureType,
        [Validators.required],
      ],
    });
  }

  private resetSignatureForm(signature?: ProblemSignatureDefinition | null): void {
    const parameters = signature?.parameters?.length
      ? signature.parameters
      : [null];

    while (this.signatureParameters.length > 0) {
      this.signatureParameters.removeAt(0);
    }

    for (const parameter of parameters) {
      this.signatureParameters.push(
        this.buildSignatureParameterGroup(parameter ?? undefined)
      );
    }

    this.form.controls.signature.patchValue({
      inputBindingMode:
        signature?.inputBindingMode ??
        CreateProblemPage.defaultInputBindingMode,
      csharpMethodName: signature?.csharpMethodName ?? '',
      pythonMethodName: signature?.pythonMethodName ?? '',
      cppMethodName: signature?.cppMethodName ?? '',
      returnType:
        signature?.returnType ?? CreateProblemPage.defaultSignatureType,
    });
  }

  private setupSignaturePreview(): void {
    this.form.controls.executionMode.valueChanges.subscribe(() => {
      if (!this.isFunctionSignatureMode()) {
        this.signaturePreview = null;
        this.previewErrorMessage = '';
        return;
      }

      this.refreshSignaturePreview();
    });

    this.form.controls.signature.valueChanges
      .pipe(debounceTime(250))
      .subscribe(() => {
        if (!this.isFunctionSignatureMode()) {
          return;
        }

        this.refreshSignaturePreview();
      });

    this.refreshSignaturePreview();
  }

  private refreshSignaturePreview(): void {
    const signature = this.buildSignaturePayload();

    if (!this.isFunctionSignatureMode() || !signature) {
      this.signaturePreview = null;
      return;
    }

    this.loadingPreview = true;
    this.previewErrorMessage = '';

    this.interviewerApi
      .getProblemSignaturePreview(signature)
      .pipe(
        finalize(() => {
          this.loadingPreview = false;
        })
      )
      .subscribe({
        next: preview => {
          this.signaturePreview = preview;
        },
        error: err => {
          this.signaturePreview = null;
          this.previewErrorMessage =
            err?.error?.message ?? 'Failed to generate starter preview.';
        },
      });
  }

  private buildSignaturePayload(): ProblemSignatureDefinition | null {
    if (this.form.controls.signature.invalid) {
      return null;
    }

    const rawSignature = this.form.controls.signature.getRawValue();

    return {
      inputBindingMode: rawSignature.inputBindingMode,
      csharpMethodName: rawSignature.csharpMethodName.trim(),
      pythonMethodName: rawSignature.pythonMethodName.trim(),
      cppMethodName: rawSignature.cppMethodName.trim(),
      returnType: rawSignature.returnType,
      parameters: rawSignature.parameters.map(parameter => ({
        name: parameter.name.trim(),
        type: parameter.type,
      })),
    };
  }

  private loadProblemTemplates(): void {
    this.templateErrorMessage = '';

    this.interviewerApi
      .getProblemTemplates()
      .pipe(
        finalize(() => {
          this.loadingTemplates = false;
        })
      )
      .subscribe({
        next: templates => {
          this.problemTemplates = templates;
        },
        error: err => {
          this.templateErrorMessage =
            err?.error?.message ?? 'Failed to load shared problem templates.';
        },
      });
  }

  private loadProblemIfEditing(): void {
    if (!this.editingProblemId) {
      return;
    }

    this.errorMessage = '';

    this.interviewerApi
      .getProblemById(this.editingProblemId)
      .pipe(
        finalize(() => {
          this.loadingProblem = false;
        })
      )
      .subscribe({
        next: problem => {
          this.applyProblemToForm(problem);
        },
        error: err => {
          this.errorMessage =
            err?.error?.message ?? 'Failed to load the problem for editing.';
        },
      });
  }

  private applyProblemToForm(problem: ProblemEditorItem): void {
    this.createdProblem = null;
    this.selectedTemplateKey = '';
    this.form.patchValue({
      title: problem.title,
      description: problem.description,
      difficulty: problem.difficulty,
      topic: problem.topic,
      constraintsText: problem.constraintsText,
      exampleInput: problem.exampleInput,
      exampleOutput: problem.exampleOutput,
      executionMode: problem.executionMode,
      csharpStarterCode: problem.csharpStarterCode,
      pythonStarterCode: problem.pythonStarterCode,
      cppStarterCode: problem.cppStarterCode,
      isPublic: problem.isPublic,
    });

    this.resetSignatureForm(problem.signature);

    if (problem.executionMode === CreateProblemPage.functionExecutionMode) {
      this.signaturePreview = {
        csharpStarterCode: problem.csharpStarterCode,
        pythonStarterCode: problem.pythonStarterCode,
        cppStarterCode: problem.cppStarterCode,
      };
      this.refreshSignaturePreview();
      return;
    }

    this.signaturePreview = null;
    this.previewErrorMessage = '';
  }
}
