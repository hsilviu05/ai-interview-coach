import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CandidatePracticeProblemSummary } from '../../models/candidate-practice.models';
import { CandidateApi } from '../../services/candidate-api.service';
import { Navbar } from '../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-practice-problems-page',
  standalone: true,
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './practice-problems-page.html',
  styleUrl: './practice-problems-page.scss',
})
export class PracticeProblemsPage implements OnInit {
  private readonly candidateApi = inject(CandidateApi);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly problems = signal<CandidatePracticeProblemSummary[]>([]);
  readonly searchTerm = signal('');
  readonly selectedDifficulty = signal('All');
  readonly selectedTopic = signal('All');

  readonly availableDifficulties = computed(() => [
    'All',
    ...Array.from(new Set(this.problems().map(problem => problem.difficulty).filter(Boolean))),
  ]);

  readonly availableTopics = computed(() => [
    'All',
    ...Array.from(new Set(this.problems().map(problem => problem.topic).filter(Boolean))).sort((left, right) =>
      left.localeCompare(right)
    ),
  ]);

  readonly filteredProblems = computed(() => {
    const searchTerm = this.searchTerm().trim().toLowerCase();
    const difficulty = this.selectedDifficulty();
    const topic = this.selectedTopic();

    return this.problems().filter(problem => {
      const matchesSearch =
        searchTerm.length === 0 ||
        [problem.title, problem.topic, problem.description, problem.difficulty]
          .join(' ')
          .toLowerCase()
          .includes(searchTerm);
      const matchesDifficulty = difficulty === 'All' || problem.difficulty === difficulty;
      const matchesTopic = topic === 'All' || problem.topic === topic;

      return matchesSearch && matchesDifficulty && matchesTopic;
    });
  });

  ngOnInit(): void {
    this.loadProblems();
  }

  updateSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  updateSelectedDifficulty(value: string): void {
    this.selectedDifficulty.set(value);
  }

  updateSelectedTopic(value: string): void {
    this.selectedTopic.set(value);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedDifficulty.set('All');
    this.selectedTopic.set('All');
  }

  openPracticeProblem(problemId: string): void {
    this.router.navigate(['/candidate/practice', problemId]);
  }

  private loadProblems(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.candidateApi.getPracticeProblems().subscribe({
      next: problems => {
        this.problems.set(problems);
      },
      error: err => {
        this.errorMessage.set(err?.error?.message ?? 'Failed to load practice problems.');
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }
}
