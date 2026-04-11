import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { CreateInterviewPage } from './create-interview-page';

describe('CreateInterviewPage', () => {
  let component: CreateInterviewPage;
  let fixture: ComponentFixture<CreateInterviewPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateInterviewPage],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateInterviewPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
