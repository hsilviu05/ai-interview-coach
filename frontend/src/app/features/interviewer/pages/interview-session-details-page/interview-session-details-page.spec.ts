import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InterviewSessionDetailsPage } from './interview-session-details-page';

describe('InterviewSessionDetailsPage', () => {
  let component: InterviewSessionDetailsPage;
  let fixture: ComponentFixture<InterviewSessionDetailsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InterviewSessionDetailsPage],
    }).compileComponents();

    fixture = TestBed.createComponent(InterviewSessionDetailsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
