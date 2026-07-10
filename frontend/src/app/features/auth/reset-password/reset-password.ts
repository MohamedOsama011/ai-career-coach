import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword implements OnInit {
  password = '';
  confirmPassword = '';
  passwordTouched = false;
  confirmPasswordTouched = false;
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  email = '';
  token = '';
  linkValid = signal(false);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.email = params['Email'] || '';
      this.token = params['token'] || '';
      this.linkValid.set(!!this.email && !!this.token);
      if (!this.linkValid()) {
        this.errorMessage.set('Invalid or missing reset link. Please request a new password reset.');
      }
    });
  }

  onSubmit(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (!this.password || !this.confirmPassword) {
      this.errorMessage.set('All fields are required.');
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }

    if (this.password.length < 6) {
      this.errorMessage.set('Password must be at least 6 characters long.');
      return;
    }

    this.isLoading.set(true);

    this.authService.resetPassword({
      email: this.email,
      token: this.token,
      password: this.password,
      confirmPassword: this.confirmPassword,
    }).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage.set('Password reset successfully!');
          setTimeout(() => this.router.navigate(['/login']), 2000);
        } else {
          this.errorMessage.set(typeof response.data === 'string' ? response.data : 'Failed to reset password.');
        }
      },
      error: (error) => {
        console.error('Reset password failed:', error);
        this.errorMessage.set(
          typeof error.error === 'object' && error.error?.message
            ? error.error.message
            : 'Failed to reset password. Please try again.'
        );
      }
    });
  }
}
