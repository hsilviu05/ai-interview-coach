import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  forwardRef,
  Input,
  NgZone,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import type * as monaco from 'monaco-editor';

type Monaco = typeof monaco;
type MonacoEditorInstance = monaco.editor.IStandaloneCodeEditor;
type MonacoTextModel = monaco.editor.ITextModel;
interface TextEditResult {
  value: string;
  selectionStart: number;
  selectionEnd: number;
}

interface MonacoAmdRequire {
  config: (config: { paths: Record<string, string> }) => void;
  (modules: string[], onLoad: () => void, onError?: (error: unknown) => void): void;
}

declare global {
  interface Window {
    monaco?: Monaco;
    require?: MonacoAmdRequire;
  }
}

@Component({
  selector: 'app-code-editor',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './code-editor.html',
  styleUrl: './code-editor.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CodeEditor),
      multi: true,
    },
  ],
})
export class CodeEditor implements AfterViewInit, OnChanges, OnDestroy, ControlValueAccessor {
  private static readonly monacoLoadTimeoutMs = 5000;
  private static readonly fallbackIndent = '  ';
  private static monacoLoaderPromise: Promise<Monaco> | null = null;
  private static loaderScriptPromise: Promise<void> | null = null;

  @Input() language = 'csharp';
  @Input() height = 460;

  @ViewChild('editorHost', { static: true })
  private readonly editorHost?: ElementRef<HTMLDivElement>;

  private readonly ngZone = inject(NgZone);

  protected readonly useTextareaFallback = signal(false);
  protected readonly isDisabled = signal(false);
  protected readonly value = signal('');
  protected readonly isMonacoReady = signal(false);

  private monaco?: Monaco;
  private editor?: MonacoEditorInstance;
  private model?: MonacoTextModel;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  async ngAfterViewInit(): Promise<void> {
    if (this.shouldUseFallbackEditor()) {
      this.activateFallback();
      return;
    }

    try {
      await this.initializeMonaco();
    } catch {
      this.activateFallback();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['language'] && this.monaco && this.model) {
      this.monaco.editor.setModelLanguage(this.model, this.language);
    }

    if (changes['height'] && this.editor) {
      queueMicrotask(() => {
        this.editor?.layout();
      });
    }
  }

  ngOnDestroy(): void {
    this.editor?.dispose();
    this.model?.dispose();
  }

  writeValue(value: string | null): void {
    this.value.set(value ?? '');

    if (this.model && this.model.getValue() !== this.value()) {
      this.model.setValue(this.value());
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
    this.editor?.updateOptions({ readOnly: isDisabled });
  }

  protected handleFallbackInput(event: Event): void {
    const nextValue = (event.target as HTMLTextAreaElement).value;
    this.value.set(nextValue);
    this.onChange(nextValue);
  }

  protected handleFallbackKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || this.isDisabled()) {
      return;
    }

    const textarea = event.target as HTMLTextAreaElement | null;

    if (!textarea) {
      return;
    }

    event.preventDefault();

    const selectionStart = textarea.selectionStart ?? 0;
    const selectionEnd = textarea.selectionEnd ?? selectionStart;
    const nextEdit = event.shiftKey
      ? this.outdentSelection(textarea.value, selectionStart, selectionEnd)
      : this.indentSelection(textarea.value, selectionStart, selectionEnd);

    this.value.set(nextEdit.value);
    this.onChange(nextEdit.value);

    queueMicrotask(() => {
      textarea.value = nextEdit.value; 
      textarea.setSelectionRange(nextEdit.selectionStart, nextEdit.selectionEnd);
    });
  }

  protected handleBlur(): void {
    this.onTouched();
  }

  private async initializeMonaco(): Promise<void> {
    const monaco = await this.loadMonaco();
    const host = this.editorHost?.nativeElement;

    if (!host) {
      this.activateFallback();
      return;
    }

    this.monaco = monaco;
    this.model = monaco.editor.createModel(this.value(), this.language);
    this.editor = monaco.editor.create(host, {
      model: this.model,
      automaticLayout: true,
      fontSize: 14,
      insertSpaces: true,
      lineNumbers: 'on',
      minimap: { enabled: false },
      padding: { top: 16, bottom: 16 },
      readOnly: this.isDisabled(),
      roundedSelection: true,
      scrollBeyondLastLine: false,
      tabFocusMode: false,
      tabSize: 2,
      wordWrap: 'on',
    });

    this.isMonacoReady.set(true);

    this.editor.onDidBlurEditorWidget(() => {
      this.ngZone.run(() => {
        this.onTouched();
      });
    });

    this.editor.onDidChangeModelContent(() => {
      const nextValue = this.model?.getValue() ?? '';

      if (nextValue === this.value()) {
        return;
      }

      this.value.set(nextValue);
      this.ngZone.run(() => {
        this.onChange(nextValue);
      });
    });
  }

  private async loadMonaco(): Promise<Monaco> {
    if (window.monaco) {
      return window.monaco;
    }

    if (!CodeEditor.monacoLoaderPromise) {
      CodeEditor.monacoLoaderPromise = this.loadMonacoFromAssets();
    }

    return CodeEditor.monacoLoaderPromise;
  }

  private async loadMonacoFromAssets(): Promise<Monaco> {
    const loaderUrl = new URL('/assets/monaco-editor/vs/loader.js', document.baseURI).toString();
    await this.ensureLoaderScript(loaderUrl);

    const amdRequire = window.require;

    if (!amdRequire) {
      throw new Error('Monaco AMD loader is unavailable.');
    }

    amdRequire.config({
      paths: {
        vs: new URL('/assets/monaco-editor/vs', document.baseURI).toString(),
      },
    });

    return this.withTimeout(
      new Promise<Monaco>((resolve, reject) => {
        amdRequire(
          ['vs/editor/editor.main'],
          () => {
            if (!window.monaco) {
              reject(new Error('Monaco failed to initialize.'));
              return;
            }

            resolve(window.monaco);
          },
          reject
        );
      }),
      'Monaco editor timed out while loading.'
    );
  }

  private ensureLoaderScript(loaderUrl: string): Promise<void> {
    if (CodeEditor.loaderScriptPromise) {
      return CodeEditor.loaderScriptPromise;
    }

    const existingLoader = document.querySelector<HTMLScriptElement>(
      'script[data-monaco-loader="true"]'
    );

    if (existingLoader?.dataset['loaded'] === 'true') {
      return Promise.resolve();
    }

    if (existingLoader) {
      CodeEditor.loaderScriptPromise = Promise.resolve();
      return CodeEditor.loaderScriptPromise;
    }

    CodeEditor.loaderScriptPromise = fetch(loaderUrl)
      .then(response => {
        if (!response.ok) {
          throw new Error('Failed to fetch Monaco loader.');
        }

        return response.text();
      })
      .then(loaderSource => {
        const sanitizedLoaderSource = loaderSource.replace(
          /^\/\/# sourceMappingURL=.*$/m,
          ''
        );
        const script = document.createElement('script');

        script.dataset['monacoLoader'] = 'true';
        script.dataset['loaded'] = 'true';
        script.text = sanitizedLoaderSource;
        document.head.appendChild(script);
      });

    return CodeEditor.loaderScriptPromise;
  }

  private withTimeout<T>(promise: Promise<T>, timeoutMessage: string): Promise<T> {
    return Promise.race([
      promise,
      new Promise<T>((_, reject) => {
        window.setTimeout(() => reject(new Error(timeoutMessage)), CodeEditor.monacoLoadTimeoutMs);
      }),
    ]);
  }

  private activateFallback(): void {
    this.useTextareaFallback.set(true);
    this.isMonacoReady.set(false);
    this.editor?.dispose();
    this.editor = undefined;
    this.model?.dispose();
    this.model = undefined;
  }

  private shouldUseFallbackEditor(): boolean {
    return typeof window === 'undefined' || /jsdom/i.test(window.navigator.userAgent);
  }

  private indentSelection(value: string, selectionStart: number, selectionEnd: number): TextEditResult {
    const indent = CodeEditor.fallbackIndent;

    if (selectionStart === selectionEnd) {
      return {
        value: `${value.slice(0, selectionStart)}${indent}${value.slice(selectionEnd)}`,
        selectionStart: selectionStart + indent.length,
        selectionEnd: selectionEnd + indent.length,
      };
    }

    const blockStart = this.getLineStart(value, selectionStart);
    const block = value.slice(blockStart, selectionEnd);
    const lines = block.split('\n');
    const indentedBlock = lines.map(line => `${indent}${line}`).join('\n');
    const totalInsertedCharacters = lines.length * indent.length;

    return {
      value: `${value.slice(0, blockStart)}${indentedBlock}${value.slice(selectionEnd)}`,
      selectionStart: selectionStart + indent.length,
      selectionEnd: selectionEnd + totalInsertedCharacters,
    };
  }

  private outdentSelection(value: string, selectionStart: number, selectionEnd: number): TextEditResult {
    const blockStart = this.getLineStart(value, selectionStart);
    const block = value.slice(blockStart, selectionEnd);
    const lines = block.split('\n');
    let firstLineRemovedCharacters = 0;
    let totalRemovedCharacters = 0;

    const outdentedBlock = lines
      .map((line, index) => {
        const { trimmedLine, removedCharacters } = this.removeLeadingIndent(line);

        if (index === 0) {
          firstLineRemovedCharacters = removedCharacters;
        }

        totalRemovedCharacters += removedCharacters;
        return trimmedLine;
      })
      .join('\n');

    return {
      value: `${value.slice(0, blockStart)}${outdentedBlock}${value.slice(selectionEnd)}`,
      selectionStart: Math.max(blockStart, selectionStart - firstLineRemovedCharacters),
      selectionEnd: Math.max(blockStart, selectionEnd - totalRemovedCharacters),
    };
  }

  private removeLeadingIndent(line: string): { trimmedLine: string; removedCharacters: number } {
    if (line.startsWith(CodeEditor.fallbackIndent)) {
      return {
        trimmedLine: line.slice(CodeEditor.fallbackIndent.length),
        removedCharacters: CodeEditor.fallbackIndent.length,
      };
    }

    if (line.startsWith('\t') || line.startsWith(' ')) {
      return {
        trimmedLine: line.slice(1),
        removedCharacters: 1,
      };
    }

    return {
      trimmedLine: line,
      removedCharacters: 0,
    };
  }

  private getLineStart(value: string, index: number): number {
    return value.lastIndexOf('\n', Math.max(0, index - 1)) + 1;
  }
}
