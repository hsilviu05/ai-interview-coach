import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, Navbar],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  private readonly router = inject(Router);

  goToCreateInterview(): void {
    this.router.navigateByUrl('/interviewer/create-interview');
  }
}