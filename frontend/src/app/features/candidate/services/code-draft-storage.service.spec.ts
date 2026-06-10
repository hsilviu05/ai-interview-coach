import { TestBed } from '@angular/core/testing';

import { CodeDraftStorageService, CodeWorkspaceDraft } from './code-draft-storage.service';

const SCOPE = 'test-scope';
const OTHER_SCOPE = 'other-scope';
const KEY = 'candidate_workspace_draft:test-scope';

function draft(
  language = 'csharp',
  sourceCode = 'some code',
  updatedAt = '2024-01-01T00:00:00.000Z'
): CodeWorkspaceDraft {
  return { language, sourceCode, updatedAt };
}

describe('CodeDraftStorageService', () => {
  let service: CodeDraftStorageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(CodeDraftStorageService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ── saveDraft ─────────────────────────────────────────────────────────────

  describe('saveDraft', () => {
    it('persists a draft that is then retrievable by language', () => {
      service.saveDraft(SCOPE, draft('csharp', 'my c# code'));

      expect(service.getDraft(SCOPE, 'csharp')?.sourceCode).toBe('my c# code');
    });

    it('sets selectedLanguage to the saved draft language', () => {
      service.saveDraft(SCOPE, draft('python', 'py code'));

      // getDraft without explicit language reads selectedLanguage
      expect(service.getDraft(SCOPE)?.language).toBe('python');
    });

    it('preserves existing drafts for other languages when adding a new one', () => {
      service.saveDraft(SCOPE, draft('csharp', 'c# code'));
      service.saveDraft(SCOPE, draft('python', 'py code'));

      expect(service.getDraft(SCOPE, 'csharp')?.sourceCode).toBe('c# code');
      expect(service.getDraft(SCOPE, 'python')?.sourceCode).toBe('py code');
    });

    it('overwrites an existing draft for the same language without affecting others', () => {
      service.saveDraft(SCOPE, draft('csharp', 'original'));
      service.saveDraft(SCOPE, draft('python', 'py'));
      service.saveDraft(SCOPE, draft('csharp', 'updated'));

      expect(service.getDraft(SCOPE, 'csharp')?.sourceCode).toBe('updated');
      expect(service.getDraft(SCOPE, 'python')?.sourceCode).toBe('py');
    });

    it('scopes drafts so that different scopes do not interfere', () => {
      service.saveDraft(SCOPE, draft('csharp', 'scope A code'));
      service.saveDraft(OTHER_SCOPE, draft('csharp', 'scope B code'));

      expect(service.getDraft(SCOPE, 'csharp')?.sourceCode).toBe('scope A code');
      expect(service.getDraft(OTHER_SCOPE, 'csharp')?.sourceCode).toBe('scope B code');
    });
  });

  // ── getDraft ──────────────────────────────────────────────────────────────

  describe('getDraft', () => {
    it('returns null when no draft exists for the scope', () => {
      expect(service.getDraft('nonexistent')).toBeNull();
    });

    it('returns null when asking for a language that has no saved draft', () => {
      service.saveDraft(SCOPE, draft('csharp'));

      expect(service.getDraft(SCOPE, 'python')).toBeNull();
    });

    it('returns the selectedLanguage draft when no language is specified', () => {
      service.saveDraft(SCOPE, draft('csharp', 'c# first'));
      service.saveDraft(SCOPE, draft('python', 'py second')); // python becomes selectedLanguage

      const result = service.getDraft(SCOPE);
      expect(result?.language).toBe('python');
      expect(result?.sourceCode).toBe('py second');
    });

    it('removes the key and returns null for a legacy draft with an empty language', () => {
      // parseLegacyDraft rejects language: '' as invalid, so the stored object
      // fails both the legacy path and the collection shape check → cleaned up.
      localStorage.setItem(KEY, JSON.stringify({
        language: '',
        sourceCode: 'code',
        updatedAt: '2024-01-01T00:00:00.000Z',
      }));

      expect(service.getDraft(SCOPE)).toBeNull();
      expect(localStorage.getItem(KEY)).toBeNull();
    });
  });

  // ── clearDraft ────────────────────────────────────────────────────────────

  describe('clearDraft', () => {
    it('removes the entire collection when no language is specified', () => {
      service.saveDraft(SCOPE, draft('csharp'));
      service.saveDraft(SCOPE, draft('python'));

      service.clearDraft(SCOPE);

      expect(service.getDraft(SCOPE)).toBeNull();
      expect(localStorage.getItem(KEY)).toBeNull();
    });

    it('removes only the specified language and preserves the remaining drafts', () => {
      service.saveDraft(SCOPE, draft('csharp', 'c#'));
      service.saveDraft(SCOPE, draft('python', 'py'));

      service.clearDraft(SCOPE, 'python');

      expect(service.getDraft(SCOPE, 'csharp')?.sourceCode).toBe('c#');
      expect(service.getDraft(SCOPE, 'python')).toBeNull();
    });

    it('updates selectedLanguage to the first remaining language when the selected one is cleared', () => {
      service.saveDraft(SCOPE, draft('csharp', 'c#'));
      service.saveDraft(SCOPE, draft('python', 'py')); // python is now selectedLanguage

      service.clearDraft(SCOPE, 'python');

      // selectedLanguage falls back to 'csharp' (the only remaining)
      expect(service.getDraft(SCOPE)?.language).toBe('csharp');
    });

    it('removes the entire collection when the last language draft is cleared', () => {
      service.saveDraft(SCOPE, draft('csharp'));

      service.clearDraft(SCOPE, 'csharp');

      expect(service.getDraft(SCOPE)).toBeNull();
      expect(localStorage.getItem(KEY)).toBeNull();
    });

    it('does not throw when clearing a language from a scope that does not exist', () => {
      expect(() => service.clearDraft('nonexistent', 'csharp')).not.toThrow();
    });

    it('does not throw when clearing the entire collection for a scope that does not exist', () => {
      expect(() => service.clearDraft('nonexistent')).not.toThrow();
    });
  });

  // ── Legacy flat-format migration ──────────────────────────────────────────

  describe('legacy flat-format draft migration', () => {
    it('reads a legacy flat draft and makes it retrievable by language', () => {
      localStorage.setItem(KEY, JSON.stringify({
        language: 'csharp',
        sourceCode: 'legacy code',
        updatedAt: '2023-12-01T00:00:00.000Z',
      }));

      const result = service.getDraft(SCOPE, 'csharp');
      expect(result?.sourceCode).toBe('legacy code');
      expect(result?.language).toBe('csharp');
    });

    it('returns null and removes the key for corrupt (non-parseable) JSON', () => {
      localStorage.setItem(KEY, 'not{valid[json');

      expect(service.getDraft(SCOPE)).toBeNull();
      expect(localStorage.getItem(KEY)).toBeNull();
    });

    it('returns null and removes the key when the stored object is neither a legacy draft nor a valid collection', () => {
      localStorage.setItem(KEY, JSON.stringify({ completely: 'wrong', shape: true }));

      expect(service.getDraft(SCOPE)).toBeNull();
      expect(localStorage.getItem(KEY)).toBeNull();
    });

    it('returns null and removes the key when all draftsByLanguage entries are invalid', () => {
      localStorage.setItem(KEY, JSON.stringify({
        selectedLanguage: 'csharp',
        draftsByLanguage: {
          csharp: { missingRequiredFields: true },
        },
      }));

      expect(service.getDraft(SCOPE, 'csharp')).toBeNull();
      expect(localStorage.getItem(KEY)).toBeNull();
    });

    it('silently drops invalid individual entries and returns the valid ones', () => {
      localStorage.setItem(KEY, JSON.stringify({
        selectedLanguage: 'csharp',
        draftsByLanguage: {
          csharp: { language: 'csharp', sourceCode: 'valid', updatedAt: '2024-01-01T00:00:00.000Z' },
          python: { missingRequiredFields: true },
        },
      }));

      expect(service.getDraft(SCOPE, 'csharp')?.sourceCode).toBe('valid');
      expect(service.getDraft(SCOPE, 'python')).toBeNull();
    });

    it('falls back selectedLanguage to the first available key when the stored value is stale', () => {
      // selectedLanguage points to 'cpp' but only 'csharp' exists in draftsByLanguage
      localStorage.setItem(KEY, JSON.stringify({
        selectedLanguage: 'cpp',
        draftsByLanguage: {
          csharp: { language: 'csharp', sourceCode: 'c#', updatedAt: '2024-01-01T00:00:00.000Z' },
        },
      }));

      const result = service.getDraft(SCOPE); // no language → uses resolved selectedLanguage
      expect(result?.language).toBe('csharp');
    });
  });
});
