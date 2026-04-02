import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InterviewResultPage } from './interview-result-page';

describe('InterviewResultPage', () => {
  let component: InterviewResultPage;
  let fixture: ComponentFixture<InterviewResultPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InterviewResultPage],
    }).compileComponents();

    fixture = TestBed.createComponent(InterviewResultPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
