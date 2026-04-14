import { Injectable, inject } from '@angular/core';
import { StorageService } from '../../../core/services/storage.service';

export interface CodeWorkspaceDraft {
  language: string;
  sourceCode: string;
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class CodeDraftStorageService {
  private readonly storage = inject(StorageService);
  private readonly keyPrefix = 'candidate_workspace_draft';

  getDraft(scope: string): CodeWorkspaceDraft | null {
    const serializedDraft = this.storage.getItem(this.buildKey(scope));

    if (!serializedDraft) {
      return null;
    }

    try {
      const draft = JSON.parse(serializedDraft) as Partial<CodeWorkspaceDraft>;

      if (
        typeof draft.language !== 'string' ||
        typeof draft.sourceCode !== 'string' ||
        typeof draft.updatedAt !== 'string'
      ) {
        this.clearDraft(scope);
        return null;
      }

      return {
        language: draft.language,
        sourceCode: draft.sourceCode,
        updatedAt: draft.updatedAt,
      };
    } catch {
      this.clearDraft(scope);
      return null;
    }
  }

  saveDraft(scope: string, draft: CodeWorkspaceDraft): void {
    this.storage.setItem(this.buildKey(scope), JSON.stringify(draft));
  }

  clearDraft(scope: string): void {
    this.storage.removeItem(this.buildKey(scope));
  }

  private buildKey(scope: string): string {
    return `${this.keyPrefix}:${scope}`;
  }
}
