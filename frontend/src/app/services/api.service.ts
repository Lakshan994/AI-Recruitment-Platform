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

export interface JobRecommendation {
    id: string;
    title: string;
    description: string;
    requiredSkills: string;
    recruiterName: string;
    createdAt: string;
    matchScore: number;
    matchingSkills: string[];
    recommendationReason: string;
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
    evaluationScore?: number;
    interviewFeedback?: string;
    aiMatchExplanation?: string;
    showExplanation?: boolean;
}

export interface PerformanceAnalytics {
    totalJobs: number;
    totalApplications: number;
    shortlistingRate: number;
    averageMatchScore: number;
    averageTimeToHireDays: number;
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

export interface Interview {
    id: string;
    candidateId: string;
    candidateName: string;
    candidateEmail: string;
    jobTitle: string;
    interviewDate: string;
    location: string;
    meetingLink: string;
    status: string;
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

export interface ChatMessage {
    role: string;
    text: string;
}

export interface LiveInterviewRequest {
    applicationId: string;
    history: ChatMessage[];
    newMessage: string;
}

export interface LiveInterviewResponse {
    reply: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
    constructor(private http: HttpClient) { }

    // Auth
    login(email: string, password: string): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${API}/api/auth/login`, { email, password });
    }

    register(email: string, password: string, confirmPassword: string, firstName: string, lastName: string, role: string): Observable<any> {
        return this.http.post(`${API}/api/auth/register`, { email, password, confirmPassword, firstName, lastName, role });
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

    getJobRecommendations(): Observable<JobRecommendation[]> {
        return this.http.get<JobRecommendation[]>(`${API}/api/jobs/recommendations`);
    }

    generateJobDescription(title: string, requiredSkills: string): Observable<{ description: string }> {
        return this.http.post<{ description: string }>(`${API}/api/jobs/generate-description`, { title, requiredSkills });
    }

    createJob(title: string, description: string, requiredSkills: string): Observable<any> {
        return this.http.post(`${API}/api/jobs`, { title, description, requiredSkills });
    }

    deleteJob(id: string): Observable<any> {
        return this.http.delete(`${API}/api/jobs/${id}`);
    }

    // Applications
    apply(jobPostingId: string, coverLetter?: string): Observable<any> {
        return this.http.post(`${API}/api/applications`, { jobPostingId, resumeUrl: '' });
    }

    generateCoverLetter(jobId: string): Observable<{ coverLetter: string }> {
        return this.http.post<{ coverLetter: string }>(`${API}/api/applications/job/${jobId}/generate-cover-letter`, {});
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

    updateApplicationEvaluation(appId: string, evaluationScore: number, interviewFeedback: string): Observable<any> {
        return this.http.put(`${API}/api/applications/${appId}/evaluation`, { evaluationScore, interviewFeedback });
    }

    generateFeedback(appId: string, evaluationScore: number): Observable<{ feedback: string }> {
        return this.http.post<{ feedback: string }>(`${API}/api/applications/${appId}/generate-feedback`, { evaluationScore });
    }

    getStats(): Observable<DashboardStats> {
        return this.http.get<DashboardStats>(`${API}/api/applications/stats`);
    }

    getNotificationLogs(): Observable<any[]> {
        return this.http.get<any[]>(`${API}/api/communication`);
    }

    sendTestEmail(candidateId: string, toEmail: string, subject: string, body: string): Observable<any> {
        return this.http.post(`${API}/api/communication/email`, { candidateId, toEmail, subject, body });
    }

    sendTestSms(candidateId: string, phoneNumber: string, message: string): Observable<any> {
        return this.http.post(`${API}/api/communication/sms`, { candidateId, phoneNumber, message });
    }


    // Interviews
    getJobInterviews(jobId: string): Observable<Interview[]> {
        return this.http.get<Interview[]>(`${API}/api/interviews/job/${jobId}`);
    }

    scheduleInterview(candidateId: string, jobPostingId: string, interviewDate: string, location: string, meetingLink: string): Observable<any> {
        return this.http.post(`${API}/api/interviews`, { candidateId, jobPostingId, interviewDate, location, meetingLink });
    }

    updateInterviewStatus(id: string, status: string): Observable<any> {
        return this.http.put(`${API}/api/interviews/${id}/status`, { status });
    }

    getMyInterviews(): Observable<any[]> {
        return this.http.get<any[]>(`${API}/api/interviews/my`);
    }

    getGoogleCalendarLink(interviewId: string): Observable<{ url: string }> {
        return this.http.get<{ url: string }>(`${API}/api/interviews/${interviewId}/google-calendar`);
    }

    getOutlookCalendarLink(interviewId: string): Observable<{ url: string }> {
        return this.http.get<{ url: string }>(`${API}/api/interviews/${interviewId}/outlook-calendar`);
    }

    getIcsFileUrl(interviewId: string): string {
        return `${API}/api/interviews/${interviewId}/ics`;
    }

    conductLiveInterview(applicationId: string, history: ChatMessage[], newMessage: string): Observable<LiveInterviewResponse> {
        const request: LiveInterviewRequest = { applicationId, history, newMessage };
        return this.http.post<LiveInterviewResponse>(`${API}/api/interviews/live`, request);
    }

    finishLiveInterview(applicationId: string): Observable<any> {
        return this.http.post(`${API}/api/interviews/live/${applicationId}/finish`, {});
    }

    // Communication
    sendEmail(candidateId: string, toEmail: string, subject: string, body: string): Observable<any> {
        return this.http.post(`${API}/api/communication/email`, { candidateId, toEmail, subject, body });
    }

    sendSms(candidateId: string, phoneNumber: string, message: string): Observable<any> {
        return this.http.post(`${API}/api/communication/sms`, { candidateId, phoneNumber, message });
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

    // Analytics
    getPerformanceAnalytics(): Observable<PerformanceAnalytics> {
        return this.http.get<PerformanceAnalytics>(`${API}/api/analytics/performance`);
    }

    getHiringTrends(): Observable<{ report: string }> {
        return this.http.get<{ report: string }>(`${API}/api/analytics/trends`);
    }

    // Secure Cloud Storage Documents
    getUserDocuments(): Observable<any[]> {
        return this.http.get<any[]>(`${API}/api/documents/my`);
    }

    uploadDocument(file: File, documentType: string): Observable<any> {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('documentType', documentType);
        return this.http.post(`${API}/api/documents/upload`, formData);
    }

    deleteDocument(id: string): Observable<any> {
        return this.http.delete(`${API}/api/documents/${id}`);
    }

    getDocumentDownloadUrl(id: string): string {
        return `${API}/api/documents/download/${id}`;
    }

    getCandidateDocuments(candidateId: string): Observable<any[]> {
        return this.http.get<any[]>(`${API}/api/documents/candidate/${candidateId}`);
    }
}
