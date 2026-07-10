import { Component, computed, signal, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { GoogleAuthService } from '../../../core/services/google-auth.service';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-register',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register implements AfterViewInit {
  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';
  agreeTerms = false;
  showPassword = false;
  showConfirmPassword = false;
  fullNameTouched = false;
  emailTouched = false;
  passwordTouched = false;
  confirmPasswordTouched = false;
  errorMessage = signal('');
  isLoading = signal(false);

  passwordStrength = computed(() => {
    const pwd = this.password;
    if (!pwd) return { score: 0, label: '', color: '', width: '0%' };
    let score = 0;
    if (pwd.length >= 6) score += 15;
    if (pwd.length >= 10) score += 10;
    if (/[a-z]/.test(pwd)) score += 15;
    if (/[A-Z]/.test(pwd)) score += 15;
    if (/\d/.test(pwd)) score += 20;
    if (/[^a-zA-Z0-9]/.test(pwd)) score += 25;
    if (pwd.length >= 14) score += 10;
    score = Math.min(score, 100);

    if (score < 30) return { score, label: 'Weak', color: 'var(--brand-danger)', width: `${score}%` };
    if (score < 50) return { score, label: 'Fair', color: '#F97316', width: `${score}%` };
    if (score < 75) return { score, label: 'Good', color: 'var(--brand-warning)', width: `${score}%` };
    return { score, label: 'Strong', color: 'var(--brand-success)', width: `${score}%` };
  });

  constructor(
    private authService: AuthService,
    private googleAuth: GoogleAuthService,
    private router: Router
  ) {}

  ngAfterViewInit(): void {
    this.googleAuth.initializeGoogleButton('google-register-button');
  }

  onGoogleClick(): void {
    this.googleAuth.triggerGoogleLogin();
  }

  onSubmit() {
    this.errorMessage.set('');

    this.fullNameTouched = true;
    this.emailTouched = true;
    this.passwordTouched = true;
    this.confirmPasswordTouched = true;

    if (!this.fullName || !this.email || !this.password || !this.confirmPassword) {
      this.errorMessage.set('All fields are required');
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage.set('Passwords do not match');
      return;
    }

    if (!this.agreeTerms) {
      this.errorMessage.set('Please agree to the terms and conditions.');
      return;
    }

    this.isLoading.set(true);

    this.authService.register({
      fullName: this.fullName,
      email: this.email,
      password: this.password
    }).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        this.authService.saveToken(response.token, true);
        this.authService.saveUserInfo(response.fullName, response.email, response.roles);
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.errorMessage.set(
          typeof error.error === 'object' && error.error?.message
            ? error.error.message
            : 'An error occurred during registration. Please try again.'
        );
      }
    });
  }
}
