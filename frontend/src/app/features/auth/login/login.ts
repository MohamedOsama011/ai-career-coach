import { Component, signal, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { GoogleAuthService } from '../../../core/services/google-auth.service';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements AfterViewInit {
  email = '';
  password = '';
  rememberMe = false;
  showPassword = false;
  emailTouched = false;
  passwordTouched = false;
  errorMessage = signal('');
  isLoading = signal(false);

  constructor(
    private authService: AuthService,
    private googleAuth: GoogleAuthService,
    private router: Router
  ) {}

  ngAfterViewInit(): void {
    this.googleAuth.initializeGoogleButton('google-sign-in-button');
  }

  onGoogleClick(): void {
    this.googleAuth.triggerGoogleLogin();
  }

  onSubmit() {
    this.errorMessage.set('');

    if (!this.email || !this.password) {
      this.emailTouched = true;
      this.passwordTouched = true;
      this.errorMessage.set('Please enter both email and password.');
      return;
    }

    this.isLoading.set(true);

    this.authService.login({
      email: this.email,
      password: this.password,
      rememberMe: this.rememberMe
    }).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        this.authService.saveToken(response.token, this.rememberMe);
        this.authService.saveUserInfo(response.fullName, this.email, response.roles);
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.errorMessage.set(
          typeof error.error === 'object' && error.error?.message
            ? error.error.message
            : 'Invalid email or password. Please try again.'
        );
      }
    });
  }
}
