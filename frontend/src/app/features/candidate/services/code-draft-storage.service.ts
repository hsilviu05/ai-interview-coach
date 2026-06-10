import { Injectable, inject } from '@angular/core';
import { StorageService } from '../../../core/services/storage.service';

export interface CodeWorkspaceDraft {
  language: string;
  sourceCode: string;
  updatedAt: string;
}

interface CodeWorkspaceDraftCollection {
  selectedLanguage: string;
  draftsByLanguage: Record<string, CodeWorkspaceDraft>;
}

@Injectable({ providedIn: 'root' })
export class CodeDraftStorageService {
  private readonly storage = inject(StorageService);
  private readonly keyPrefix = 'candidate_workspace_draft';

  getDraft(scope: string, language?: string): CodeWorkspaceDraft | null {
    const collection = this.getDraftCollection(scope);

    if (!collection) {
      return null;
    }

    const selectedLanguage = language ?? collection.selectedLanguage;

    if (!selectedLanguage) {
      return null;
    }

    return collection.draftsByLanguage[selectedLanguage] ?? null;
  }

  saveDraft(scope: string, draft: CodeWorkspaceDraft): void {
    const collection = this.getDraftCollection(scope) ?? {
      selectedLanguage: draft.language,
      draftsByLanguage: {},
    };

    collection.selectedLanguage = draft.language;
    collection.draftsByLanguage[draft.language] = draft;
    this.storage.setItem(this.buildKey(scope), JSON.stringify(collection));
  }

  clearDraft(scope: string, language?: string): void {
    if (!language) {
      this.removeDraftCollection(scope);
      return;
    }

    const collection = this.getDraftCollection(scope);

    if (!collection) {
      return;
    }

    delete collection.draftsByLanguage[language];

    const remainingLanguages = Object.keys(collection.draftsByLanguage);
    if (remainingLanguages.length === 0) {
      this.removeDraftCollection(scope);
      return;
    }

    if (!collection.draftsByLanguage[collection.selectedLanguage]) {
      collection.selectedLanguage = remainingLanguages[0];
    }

    this.storage.setItem(this.buildKey(scope), JSON.stringify(collection));
  }

  private getDraftCollection(scope: string): CodeWorkspaceDraftCollection | null {
    const serializedDraft = this.storage.getItem(this.buildKey(scope));

    if (!serializedDraft) {
      return null;
    }

    try {
      const parsedDraft = JSON.parse(serializedDraft) as Partial<CodeWorkspaceDraftCollection & CodeWorkspaceDraft>;
      const legacyDraft = this.parseLegacyDraft(parsedDraft);

      if (legacyDraft) {
        return {
          selectedLanguage: legacyDraft.language,
          draftsByLanguage: {
            [legacyDraft.language]: legacyDraft,
          },
        };
      }

      if (
        typeof parsedDraft.selectedLanguage !== 'string' ||
        typeof parsedDraft.draftsByLanguage !== 'object' ||
        !parsedDraft.draftsByLanguage
      ) {
        this.removeDraftCollection(scope);
        return null;
      }

      const normalizedDraftsByLanguage = Object.entries(parsedDraft.draftsByLanguage).reduce<Record<string, CodeWorkspaceDraft>>(
        (draftsByLanguage, [language, draftValue]) => {
          const draft = this.parseLegacyDraft(draftValue);

          if (draft) {
            draftsByLanguage[language] = draft;
          }

          return draftsByLanguage;
        },
        {}
      );

      if (Object.keys(normalizedDraftsByLanguage).length === 0) {
        this.removeDraftCollection(scope);
        return null;
      }

      return {
        selectedLanguage: normalizedDraftsByLanguage[parsedDraft.selectedLanguage]
          ? parsedDraft.selectedLanguage
          : Object.keys(normalizedDraftsByLanguage)[0],
        draftsByLanguage: normalizedDraftsByLanguage,
      };
    } catch {
      this.removeDraftCollection(scope);
      return null;
    }
  }

  private parseLegacyDraft(value: unknown): CodeWorkspaceDraft | null {
    if (!value || typeof value !== 'object') {
      return null;
    }

    const draft = value as Partial<CodeWorkspaceDraft>;

    if (
      typeof draft.language !== 'string' ||
      !draft.language ||
      typeof draft.sourceCode !== 'string' ||
      typeof draft.updatedAt !== 'string'
    ) {
      return null;
    }

    return {
      language: draft.language,
      sourceCode: draft.sourceCode,
      updatedAt: draft.updatedAt,
    };
  }

  private removeDraftCollection(scope: string): void {
    this.storage.removeItem(this.buildKey(scope));
  }

  private buildKey(scope: string): string {
    return `${this.keyPrefix}:${scope}`;
  }
}
