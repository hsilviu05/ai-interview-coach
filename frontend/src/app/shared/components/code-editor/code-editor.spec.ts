import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { afterEach, vi, describe, it, expect, beforeEach } from 'vitest';
import { CodeEditor } from './code-editor';

describe('CodeEditor', () => {
  let component: CodeEditor;
  let fixture: ComponentFixture<CodeEditor>;
  let changedValues: string[];
  let onChange: (value: string) => void;

  afterEach(() => {
    vi.restoreAllMocks();
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CodeEditor],
    }).compileComponents();

    fixture = TestBed.createComponent(CodeEditor);
    component = fixture.componentInstance;
    changedValues = [];
    onChange = value => changedValues.push(value);
    component.registerOnChange(onChange);

    component['useTextareaFallback'].set(true);

    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should insert indentation when tab is pressed in the fallback editor', async () => {
    component.writeValue('answer');
    fixture.detectChanges();

    const textareaDebug = fixture.debugElement.query(By.css('[data-testid="code-editor-textarea"]'));
    const textarea = textareaDebug.nativeElement as HTMLTextAreaElement;
    
    textarea.focus();
    textarea.setSelectionRange(0, 0);

    textareaDebug.triggerEventHandler('keydown', {
      key: 'Tab',
      shiftKey: false,
      preventDefault: vi.fn(),
      target: textarea
    });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(textarea.value).toBe('  answer');
    expect(textarea.selectionStart).toBe(2);
    expect(textarea.selectionEnd).toBe(2);
    expect(changedValues.at(-1)).toBe('  answer');
  });

  it('should outdent selected lines when shift tab is pressed in the fallback editor', async () => {
    component.writeValue('  first\n  second');
    fixture.detectChanges();

    const textareaDebug = fixture.debugElement.query(By.css('[data-testid="code-editor-textarea"]'));
    const textarea = textareaDebug.nativeElement as HTMLTextAreaElement;
    
    textarea.focus();
    textarea.setSelectionRange(0, textarea.value.length);

    textareaDebug.triggerEventHandler('keydown', {
      key: 'Tab',
      shiftKey: true,
      preventDefault: vi.fn(),
      target: textarea
    });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(textarea.value).toBe('first\nsecond');
    expect(changedValues.at(-1)).toBe('first\nsecond');
  });

  function getTextarea(): HTMLTextAreaElement {
    const textarea = fixture.nativeElement.querySelector('[data-testid="code-editor-textarea"]') as HTMLTextAreaElement | null;
    if (!textarea) {
      throw new Error('Expected the fallback textarea editor to be rendered in tests.');
    }
    return textarea;
  }
});