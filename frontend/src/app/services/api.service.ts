import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API = 'http://localhost:5076';

export interface Job {
  id: string;
  title: string;
  description: string;
  requiredSkills: string;
  recruiterName: string;
  recruiterId: string;
  createdAt: string;
  isActive: boolean;
}

export interface Application {
  id: string;
  jobPostingId: string;
  jobTitle: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  resumeUrl: string;
  status: string;
  aiMatchScore: number;
  appliedAt: string;
}

export interface DashboardStats {
  totalJobs: number;
  totalApplications: number;
  shortlisted: number;
  interviewed: number;
  hired: number;
  rejected: number;
  avgMatchScore: number;
}

export interface AuthResponse {
  token: string;
  email: string;
  role: string;
  firstName: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  createdAt: string;
}

export interface AdminStats {
  totalUsers: number;
  totalCandidates: number;
  totalRecruiters: number;
  totalHiringManagers: number;
  totalAdmins: number;
  totalJobs: number;
  activeJobs: number;
  totalApplications: number;
  applicationsApplied: number;
  applicationsShortlisted: number;
  applicationsInterviewed: number;
  applicationsHired: number;
  applicationsRejected: number;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  // Auth
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API}/api/auth/login`, { email, password });
  }

  register(email: string, password: string, firstName: string, lastName: string, role: string): Observable<any> {
    return this.http.post(`${API}/api/auth/register`, { email, password, firstName, lastName, role });
  }

  // Jobs
  getJobs(search?: string): Observable<Job[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<Job[]>(`${API}/api/jobs`, { params });
  }

  getMyJobs(): Observable<Job[]> {
    return this.http.get<Job[]>(`${API}/api/jobs/my`);
  }

  createJob(title: string, description: string, requiredSkills: string): Observable<any> {
    return this.http.post(`${API}/api/jobs`, { title, description, requiredSkills });
  }

  deleteJob(id: string): Observable<any> {
    return this.http.delete(`${API}/api/jobs/${id}`);
  }

  // Applications
  apply(jobPostingId: string): Observable<any> {
    return this.http.post(`${API}/api/applications`, { jobPostingId, resumeUrl: '' });
  }

  getMyApplications(): Observable<Application[]> {
    return this.http.get<Application[]>(`${API}/api/applications/my`);
  }

  getJobApplications(jobId: string): Observable<Application[]> {
    return this.http.get<Application[]>(`${API}/api/applications/job/${jobId}`);
  }

  updateApplicationStatus(appId: string, status: string): Observable<any> {
    return this.http.put(`${API}/api/applications/${appId}/status`, { status });
  }

  getShortlistedApplications(): Observable<Application[]> {
    return this.http.get<Application[]>(`${API}/api/applications/shortlisted`);
  }

  updateHiringDecision(id: string, status: string): Observable<any> {
    return this.http.put(`${API}/api/applications/${id}/hiring-decision`, { status });
  }

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${API}/api/applications/stats`);
  }

  // Admin
  getAdminUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${API}/api/admin/users`);
  }

  updateUserRole(userId: string, role: string): Observable<any> {
    return this.http.put(`${API}/api/admin/users/${userId}/role`, { role });
  }

  getAdminStats(): Observable<AdminStats> {
    return this.http.get<AdminStats>(`${API}/api/admin/analytics`);
  }

  // Profile
  getProfile(): Observable<any> {
    return this.http.get(`${API}/api/profile`);
  }

  updateProfile(data: any): Observable<any> {
    return this.http.put(`${API}/api/profile`, data);
  }

  uploadResume(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${API}/api/profile/upload-resume`, formData);
  }
}
