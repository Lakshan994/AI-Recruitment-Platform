import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';

export interface UserData {
  email: string;
  role: string;
  firstName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private userSubject = new BehaviorSubject<UserData | null>(this.getStoredUser());
  user$ = this.userSubject.asObservable();

  constructor(private router: Router) {}

  private getStoredUser(): UserData | null {
    const stored = localStorage.getItem('user');
    const token = localStorage.getItem('token');
    if (stored && token) {
      try {
        return JSON.parse(stored) as UserData;
      } catch {
        return null;
      }
    }
    return null;
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getUser(): UserData | null {
    return this.userSubject.value;
  }

  isLoggedIn(): boolean {
    return !!this.getToken() && !!this.getUser();
  }

  isRecruiter(): boolean {
    return this.getUser()?.role === 'Recruiter';
  }

  isCandidate(): boolean {
    return this.getUser()?.role === 'Candidate';
  }

  isAdmin(): boolean {
    return this.getUser()?.role === 'Admin';
  }

  isHiringManager(): boolean {
    return this.getUser()?.role === 'HiringManager';
  }

  setSession(token: string, user: UserData): void {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(user));
    this.userSubject.next(user);
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.userSubject.next(null);
    this.router.navigate(['/']);
  }
}
