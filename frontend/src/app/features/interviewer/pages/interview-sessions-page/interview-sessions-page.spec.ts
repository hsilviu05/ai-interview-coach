import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { InterviewSessionsPage } from './interview-sessions-page';

describe('InterviewSessionsPage', () => {
  let component: InterviewSessionsPage;
  let fixture: ComponentFixture<InterviewSessionsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InterviewSessionsPage],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(InterviewSessionsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
