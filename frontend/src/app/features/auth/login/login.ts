import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  email: string = '';
  password: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onSubmit(){
    this.errorMessage = '';

    if (!this.email || !this.password) {
      this.errorMessage = 'Please enter both email and password.';
      return;
    }

    this.isLoading = true;

    this.authService.login({
      email: this.email,
      password: this.password
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response) => {
        this.authService.saveToken(response.token);
        this.authService.saveUserInfo(response.fullName, this.email, response.roles);
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        console.error('Login failed:', error);
        console.error('Error body:', JSON.stringify(error.error));
        this.errorMessage = typeof error.error === 'object' && error.error?.message
          ? error.error.message
          : 'Invalid email or password. Please try again.';
      }
    });
  }
}
