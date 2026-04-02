import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InterviewSolvePage } from './interview-solve-page';

describe('InterviewSolvePage', () => {
  let component: InterviewSolvePage;
  let fixture: ComponentFixture<InterviewSolvePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InterviewSolvePage],
    }).compileComponents();

    fixture = TestBed.createComponent(InterviewSolvePage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
