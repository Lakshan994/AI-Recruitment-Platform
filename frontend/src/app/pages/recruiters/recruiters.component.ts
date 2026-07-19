import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Job, Application, Interview } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-recruiters',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './recruiters.component.html'
})
export class RecruitersComponent implements OnInit {
    isFormOpen = false;
    title = '';
    description = '';
    requiredSkills = '';
    loading = false;
    message = '';

    jobs: Job[] = [];
    selectedJobId: string | null = null;
    applications: Application[] = [];
    appsLoading = false;
    isRecruiter = false;

    // Search & Filtering
    candidateSearch = '';
    statusFilter = 'All';

    // Scheduled Interviews
    interviews: Interview[] = [];
    intsLoading = false;

    // Schedule Modal
    isScheduleOpen = false;
    selectedCandidateForInterview: Application | null = null;
    interviewDate = '';
    interviewLocation = 'Online (Google Meet)';
    interviewMeetingLink = 'https://meet.google.com/abc-defg-hij';

    // Communication Modal
    isCommOpen = false;
    commType: 'email' | 'sms' = 'email';
    selectedCandidateForComm: Application | null = null;
    commSubject = '';
    commBody = '';
    commMessage = ''; // for SMS

    // Candidate Secure Documents Modal
    isDocsModalOpen = false;
    loadingCandidateDocs = false;
    selectedCandidateName = '';
    candidateDocs: any[] = [];

    constructor(private api: ApiService, private auth: AuthService) { }

    ngOnInit(): void {
        this.isRecruiter = this.auth.isRecruiter();
        if (this.isRecruiter) {
            this.fetchJobs();
        }
    }

    fetchJobs(): void {
        this.api.getMyJobs().subscribe({
            next: (data) => {
                this.jobs = data;
                if (this.jobs.length > 0 && !this.selectedJobId) {
                    this.selectJob(this.jobs[0].id);
                }
            },
            error: () => { }
        });
    }

    selectJob(jobId: string): void {
        this.selectedJobId = jobId;
        this.fetchApplications(jobId);
        this.fetchInterviews(jobId);
    }

    fetchApplications(jobId: string): void {
        this.appsLoading = true;
        this.api.getJobApplications(jobId).subscribe({
            next: (data) => {
                this.applications = data.sort((a, b) => b.aiMatchScore - a.aiMatchScore);
                this.appsLoading = false;
            },
            error: () => {
                this.applications = [];
                this.appsLoading = false;
            }
        });
    }

    fetchInterviews(jobId: string): void {
        this.intsLoading = true;
        this.api.getJobInterviews(jobId).subscribe({
            next: (data) => {
                this.interviews = data;
                this.intsLoading = false;
            },
            error: () => {
                this.interviews = [];
                this.intsLoading = false;
            }
        });
    }

    get filteredApplications(): Application[] {
        return this.applications.filter(app => {
            const name = app.candidateName || '';
            const email = app.candidateEmail || '';
            const matchesSearch = name.toLowerCase().includes(this.candidateSearch.toLowerCase()) ||
                email.toLowerCase().includes(this.candidateSearch.toLowerCase());
            const matchesStatus = this.statusFilter === 'All' || app.status === this.statusFilter;
            return matchesSearch && matchesStatus;
        });
    }

    onSubmit(): void {
        this.loading = true;
        this.message = '';

        this.api.createJob(this.title, this.description, this.requiredSkills).subscribe({
            next: () => {
                this.message = 'Job posted successfully!';
                this.title = '';
                this.description = '';
                this.requiredSkills = '';
                this.isFormOpen = false;
                this.fetchJobs();
                this.loading = false;
                setTimeout(() => this.message = '', 3000);
            },
            error: (err) => {
                this.message = err.message + ' | ' + JSON.stringify(err.error) + ' | Status: ' + err.status;
                this.loading = false;
            }
        });
    }

    updateStatus(appId: string, status: string): void {
        this.api.updateApplicationStatus(appId, status).subscribe({
            next: () => {
                if (this.selectedJobId) {
                    this.fetchApplications(this.selectedJobId);
                }
            },
            error: () => { }
        });
    }

    updateIntStatus(intId: string, status: string): void {
        this.api.updateInterviewStatus(intId, status).subscribe({
            next: () => {
                if (this.selectedJobId) {
                    this.fetchInterviews(this.selectedJobId);
                    this.fetchApplications(this.selectedJobId);
                }
            },
            error: () => { }
        });
    }

    // Interview Schedule Dialog
    openScheduleModal(candidate: Application): void {
        this.selectedCandidateForInterview = candidate;
        // set default date/time to tomorrow
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        tomorrow.setHours(10, 0, 0, 0);
        // Format to YYYY-MM-DDThh:mm
        const offset = tomorrow.getTimezoneOffset();
        const localTomorrow = new Date(tomorrow.getTime() - (offset * 60 * 1000));
        this.interviewDate = localTomorrow.toISOString().slice(0, 16);

        this.interviewLocation = 'Online (Google Meet)';
        this.interviewMeetingLink = 'https://meet.google.com/abc-defg-hij';
        this.isScheduleOpen = true;
    }

    closeScheduleModal(): void {
        this.isScheduleOpen = false;
        this.selectedCandidateForInterview = null;
    }

    submitSchedule(): void {
        if (!this.selectedCandidateForInterview || !this.selectedJobId) return;

        this.loading = true;
        this.api.scheduleInterview(
            this.selectedCandidateForInterview.candidateId,
            this.selectedJobId,
            this.interviewDate,
            this.interviewLocation,
            this.interviewMeetingLink
        ).subscribe({
            next: (res) => {
                this.message = res.message || 'Interview scheduled successfully!';
                this.closeScheduleModal();
                this.fetchApplications(this.selectedJobId!);
                this.fetchInterviews(this.selectedJobId!);
                this.loading = false;
                setTimeout(() => this.message = '', 3000);
            },
            error: (err) => {
                this.message = err.error?.message || 'Failed to schedule interview.';
                this.loading = false;
                setTimeout(() => this.message = '', 3000);
            }
        });
    }

    // Communication Modal
    openCommModal(candidate: Application, type: 'email' | 'sms'): void {
        this.selectedCandidateForComm = candidate;
        this.commType = type;
        this.commSubject = `TalentAI - Regarding your application for Job Posting`;
        this.commBody = `Hi ${candidate.candidateName},\n\nWe reviewed your profile and would like to discuss further. Let us know your availability.\n\nBest regards,\nRecruitment Team`;
        this.commMessage = `Hi ${candidate.candidateName}, thank you for your application. We would like to schedule a call. Please check your email for details.`;
        this.isCommOpen = true;
    }

    closeCommModal(): void {
        this.isCommOpen = false;
        this.selectedCandidateForComm = null;
    }

    submitComm(): void {
        if (!this.selectedCandidateForComm) return;

        this.loading = true;
        if (this.commType === 'email') {
            this.api.sendEmail(
                this.selectedCandidateForComm.candidateId,
                this.selectedCandidateForComm.candidateEmail,
                this.commSubject,
                this.commBody
            ).subscribe({
                next: (res) => {
                    this.message = res.Message || 'Email sent successfully!';
                    this.closeCommModal();
                    this.loading = false;
                    setTimeout(() => this.message = '', 3000);
                },
                error: (err) => {
                    this.message = err.error?.Message || 'Failed to send email.';
                    this.loading = false;
                    setTimeout(() => this.message = '', 3000);
                }
            });
        } else {
            // For SMS, we use candidate phone number. Let's make sure backend extracts it.
            // We pass phone number as an empty string (or if we don't have it, backend gets it from the CandidateId)
            this.api.sendSms(
                this.selectedCandidateForComm.candidateId,
                '', // Backend can query from User record
                this.commMessage
            ).subscribe({
                next: (res) => {
                    this.message = res.Message || 'SMS sent successfully!';
                    this.closeCommModal();
                    this.loading = false;
                    setTimeout(() => this.message = '', 3000);
                },
                error: (err) => {
                    this.message = err.error?.Message || 'Failed to send SMS.';
                    this.loading = false;
                    setTimeout(() => this.message = '', 3000);
                }
            });
        }
    }

    getStatusColor(status: string): string {
        switch (status) {
            case 'Applied': return 'bg-slate-500/10 text-slate-400 border-slate-500/20';
            case 'Shortlisted': return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
            case 'Interviewed': return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
            case 'Hired': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
            case 'Rejected': return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
            default: return 'bg-slate-500/10 text-slate-400 border-slate-500/20';
        }
    }

    getSkills(skills: string): string[] {
        return skills ? skills.split(',').map(s => s.trim()) : [];
    }

    timeAgo(dateStr: string): string {
        const diff = Date.now() - new Date(dateStr).getTime();
        const days = Math.floor(diff / 86400000);
        if (days === 0) return 'Today';
        if (days === 1) return '1 day ago';
        return `${days} days ago`;
    }

    toggleExplanation(app: any): void {
        app.showExplanation = !app.showExplanation;
    }

    syncGoogle(interviewId: string): void {
        this.api.getGoogleCalendarLink(interviewId).subscribe({
            next: (res) => {
                if (res?.url) window.open(res.url, '_blank');
            },
            error: (err) => console.error('Failed to get Google Calendar link', err)
        });
    }

    syncOutlook(interviewId: string): void {
        this.api.getOutlookCalendarLink(interviewId).subscribe({
            next: (res) => {
                if (res?.url) window.open(res.url, '_blank');
            },
            error: (err) => console.error('Failed to get Outlook Calendar link', err)
        });
    }

    downloadIcs(interviewId: string): void {
        const url = this.api.getIcsFileUrl(interviewId);
        window.open(url, '_blank');
    }

    viewCandidateDocs(candidateId: string, candidateName: string): void {
        this.selectedCandidateName = candidateName;
        this.isDocsModalOpen = true;
        this.loadingCandidateDocs = true;
        this.api.getCandidateDocuments(candidateId).subscribe({
            next: (data) => {
                this.candidateDocs = data;
                this.loadingCandidateDocs = false;
            },
            error: () => {
                this.candidateDocs = [];
                this.loadingCandidateDocs = false;
            }
        });
    }

    downloadSecureDoc(id: string): void {
        const url = this.api.getDocumentDownloadUrl(id);
        window.open(url, '_blank');
    }
}
