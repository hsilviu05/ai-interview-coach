import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { InterviewAccessPage } from './interview-access-page';

describe('InterviewAccessPage', () => {
  let component: InterviewAccessPage;
  let fixture: ComponentFixture<InterviewAccessPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InterviewAccessPage],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(InterviewAccessPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
