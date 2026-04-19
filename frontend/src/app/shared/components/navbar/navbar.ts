import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { canAccessCandidateWorkspace, canAccessInterviewerWorkspace, isAdminRole } from '../../../core/auth/access-policies';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly role = computed(() => this.authService.getRole());
  readonly canAccessInterviewerWorkspace = computed(() =>
    canAccessInterviewerWorkspace(this.role()));
  readonly canAccessCandidateWorkspace = computed(() =>
    canAccessCandidateWorkspace(this.role()));
  readonly isAdmin = computed(() => isAdminRole(this.role()));

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigateByUrl('/login');
      },
      error: () => {
        this.router.navigateByUrl('/login');
      },
    });
  }
}
