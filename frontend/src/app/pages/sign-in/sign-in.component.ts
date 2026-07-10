import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-sign-in',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './sign-in.component.html'
})
export class SignInComponent implements OnInit {
  isLogin = true;
  email = '';
  password = '';
  firstName = '';
  lastName = '';
  role = 'Candidate';
  error = '';
  loading = false;

  constructor(
    private auth: AuthService,
    private api: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.redirectUser();
    }
  }

  private redirectUser(): void {
    if (this.auth.isAdmin()) this.router.navigate(['/admin']);
    else if (this.auth.isHiringManager()) this.router.navigate(['/hiring-manager']);
    else if (this.auth.isRecruiter()) this.router.navigate(['/recruiters']);
    else this.router.navigate(['/dashboard']);
  }

  toggleMode(): void {
    this.isLogin = !this.isLogin;
    this.error = '';
  }

  onSubmit(): void {
    this.error = '';
    this.loading = true;

    if (this.isLogin) {
      this.api.login(this.email, this.password).subscribe({
        next: (data) => {
          this.auth.setSession(data.token, { email: data.email, role: data.role, firstName: data.firstName });
          this.redirectUser();
        },
        error: (err) => {
          this.error = err.error?.message || 'Authentication failed';
          this.loading = false;
        }
      });
    } else {
      this.api.register(this.email, this.password, this.firstName, this.lastName, this.role).subscribe({
        next: () => {
          this.isLogin = true;
          this.error = 'Registration successful! Please sign in.';
          this.loading = false;
        },
        error: (err) => {
          this.error = err.error?.message || 'Registration failed';
          this.loading = false;
        }
      });
    }
  }
}
