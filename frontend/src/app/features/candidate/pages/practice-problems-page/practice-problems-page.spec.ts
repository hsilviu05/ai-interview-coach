import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { PracticeProblemsPage } from './practice-problems-page';
import { CandidateApi } from '../../services/candidate-api.service';
import { CandidatePracticeProblemSummary } from '../../models/candidate-practice.models';

describe('PracticeProblemsPage', () => {
  let component: PracticeProblemsPage;
  let fixture: ComponentFixture<PracticeProblemsPage>;
  const practiceProblems: CandidatePracticeProblemSummary[] = [
    {
      id: '1',
      title: 'Two Sum',
      description: 'Find the pair of values that matches the target sum.',
      difficulty: 'Easy',
      topic: 'Arrays',
      constraintsText: '',
      exampleInput: 'nums = [2,7,11,15], target = 9',
      exampleOutput: '[0,1]',
      createdAt: new Date().toISOString(),
    },
    {
      id: '2',
      title: 'Binary Tree Depth',
      description: 'Compute the depth of a binary tree.',
      difficulty: 'Medium',
      topic: 'Trees',
      constraintsText: '',
      exampleInput: 'root = [3,9,20,null,null,15,7]',
      exampleOutput: '3',
      createdAt: new Date().toISOString(),
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PracticeProblemsPage],
      providers: [
        provideRouter([]),
        {
          provide: CandidateApi,
          useValue: {
            getPracticeProblems: () => of(practiceProblems),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PracticeProblemsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should filter problems by search text and difficulty', () => {
    component.updateSearchTerm('tree');
    component.updateSelectedDifficulty('Medium');

    expect(component.filteredProblems()).toEqual([practiceProblems[1]]);
  });
});
