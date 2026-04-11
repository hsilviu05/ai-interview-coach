import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { PracticeProblemsPage } from './practice-problems-page';
import { CandidateApi } from '../../services/candidate-api.service';

describe('PracticeProblemsPage', () => {
  let component: PracticeProblemsPage;
  let fixture: ComponentFixture<PracticeProblemsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PracticeProblemsPage],
      providers: [
        provideRouter([]),
        {
          provide: CandidateApi,
          useValue: {
            getPracticeProblems: () => of([]),
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
});
