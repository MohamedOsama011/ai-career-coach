import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-register',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  fullName: string = '';
  email: string = '';
  password: string = '';
  confirmPassword: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onSubmit() {
    this.errorMessage = '';

    if (!this.email || !this.password || !this.confirmPassword) {
      this.errorMessage = 'All fields are required';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match';
      return;
    }

    if (this.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters long';
      return;
    }
    //password RequireDigit
    if (!/\d/.test(this.password)) {
      this.errorMessage = 'Password must contain at least one digit';
      return;
    }

    this.isLoading = true;

    this.authService.register({
      fullName: this.fullName,
      email: this.email,
      password: this.password
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response) => {
        this.authService.saveToken(response.token);
        this.authService.saveUserInfo(response.fullName, response.email, response.roles);
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        console.error('Registration failed:', error);
        console.error('Error body:', JSON.stringify(error.error));
        this.errorMessage = typeof error.error === 'object' && error.error?.message
          ? error.error.message
          : 'An error occurred during registration. Please try again.';
      }
    });
  }
}
