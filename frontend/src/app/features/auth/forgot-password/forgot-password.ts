import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword {
  email = '';
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  constructor(private authService: AuthService) {}

  onSubmit(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (!this.email) {
      this.errorMessage.set('Please enter your email address.');
      return;
    }

    this.isLoading.set(true);

    this.authService.forgotPassword({ email: this.email }).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: () => {
        this.successMessage.set('Check your email for the password reset link.');
      },
      error: (error) => {
        console.error('Forgot password failed:', error);
        this.errorMessage.set(
          typeof error.error === 'object' && error.error?.message
            ? error.error.message
            : 'Failed to send reset email. Please try again.'
        );
      }
    });
  }
}
