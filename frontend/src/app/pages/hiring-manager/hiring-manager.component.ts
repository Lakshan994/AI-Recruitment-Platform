import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Application } from '../../services/api.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
    selector: 'app-hiring-manager',
    standalone: true,
    imports: [CommonModule, FormsModule],
    providers: [DatePipe],
    templateUrl: './hiring-manager.component.html'
})
export class HiringManagerComponent implements OnInit {
    applications: Application[] = [];
    isLoading = true;
    error = '';
    successMessage = '';

    // Evaluation & Feedback fields
    isEvalModalOpen = false;
    selectedAppForEval: Application | null = null;
    evaluationScore = 5;
    interviewFeedback = '';
    submittingEval = false;
    generatingFeedback = false;

    // Schedule Modal
    isScheduleOpen = false;
    selectedCandidateForInterview: Application | null = null;
    interviewDate = '';
    interviewLocation = 'Online (Google Meet)';
    interviewMeetingLink = 'https://meet.google.com/abc-defg-hij';
    loading = false;

    // Communication Modal
    isCommOpen = false;
    selectedCandidateForComm: Application | null = null;
    commType: 'email' | 'sms' = 'email';
    commSubject = '';
    commBody = '';
    commMessage = '';

    constructor(private api: ApiService, private datePipe: DatePipe) { }

    ngOnInit(): void {
        this.loadApplications();
    }

    loadApplications(): void {
        this.isLoading = true;
        this.api.getShortlistedApplications().subscribe({
            next: (data) => {
                this.applications = data;
                this.isLoading = false;
            },
            error: (err: HttpErrorResponse) => {
                this.error = 'Failed to load shortlisted applications. Error: ' + (err.error?.message || err.message);
                this.isLoading = false;
            }
        });
    }

    makeDecision(appId: string, status: string): void {
        this.api.updateHiringDecision(appId, status).subscribe({
            next: (res) => {
                this.successMessage = res.message || `Decision saved: ${status}`;
                setTimeout(() => this.successMessage = '', 3000);
                const app = this.applications.find(a => a.id === appId);
                if (app) app.status = status;
            },
            error: () => {
                this.error = 'Failed to save hiring decision.';
                setTimeout(() => this.error = '', 3000);
            }
        });
    }

    // Evaluation Modal
    openEvalModal(app: Application): void {
        this.selectedAppForEval = app;
        this.evaluationScore = app.evaluationScore || 5;
        this.interviewFeedback = app.interviewFeedback || '';
        this.isEvalModalOpen = true;
    }

    closeEvalModal(): void {
        this.isEvalModalOpen = false;
        this.selectedAppForEval = null;
    }

    generateAiFeedback(): void {
        if (!this.selectedAppForEval) return;
        this.generatingFeedback = true;
        this.api.generateFeedback(this.selectedAppForEval.id, this.evaluationScore).subscribe({
            next: (res) => {
                this.interviewFeedback = res.feedback;
                this.generatingFeedback = false;
            },
            error: (err) => {
                this.error = 'Failed to generate AI feedback comments.';
                setTimeout(() => this.error = '', 3000);
                this.generatingFeedback = false;
            }
        });
    }

    submitEvaluation(): void {
        if (!this.selectedAppForEval) return;

        this.submittingEval = true;
        this.api.updateApplicationEvaluation(
            this.selectedAppForEval.id,
            this.evaluationScore,
            this.interviewFeedback
        ).subscribe({
            next: (res) => {
                this.successMessage = res.message || 'Evaluation saved successfully!';
                setTimeout(() => this.successMessage = '', 3000);

                // Update local object
                const app = this.applications.find(a => a.id === this.selectedAppForEval?.id);
                if (app) {
                    app.evaluationScore = this.evaluationScore;
                    app.interviewFeedback = this.interviewFeedback;
                }

                this.closeEvalModal();
                this.submittingEval = false;
            },
            error: (err: HttpErrorResponse) => {
                this.error = 'Failed to save evaluation. Error: ' + (err.error?.message || err.message);
                setTimeout(() => this.error = '', 4000);
                this.submittingEval = false;
            }
        });
    }

    setScore(score: number): void {
        this.evaluationScore = score;
    }

    getStars(rating: number | undefined): number[] {
        if (!rating) return [];
        return Array(rating).fill(0);
    }

    getEmptyStars(rating: number | undefined): number[] {
        const score = rating || 0;
        return Array(5 - score).fill(0);
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
        if (!this.selectedCandidateForInterview) return;

        this.loading = true;
        this.api.scheduleInterview(
            this.selectedCandidateForInterview.candidateId,
            this.selectedCandidateForInterview.jobPostingId,
            this.interviewDate,
            this.interviewLocation,
            this.interviewMeetingLink
        ).subscribe({
            next: (res) => {
                this.successMessage = res.message || 'Interview scheduled successfully!';
                this.closeScheduleModal();
                this.loadApplications();
                this.loading = false;
                setTimeout(() => this.successMessage = '', 3000);
            },
            error: (err) => {
                this.error = err.error?.message || 'Failed to schedule interview.';
                this.loading = false;
                setTimeout(() => this.error = '', 3000);
            }
        });
    }

    // Communication Modal
    openCommModal(candidate: Application, type: 'email' | 'sms'): void {
        this.selectedCandidateForComm = candidate;
        this.commType = type;
        this.commSubject = `TalentAI - Regarding your application`;
        this.commBody = `Hi ${candidate.candidateName},\n\nWe reviewed your profile and would like to discuss further. Let us know your availability.\n\nBest regards,\nHiring Team`;
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
                    this.successMessage = res.Message || 'Email sent successfully!';
                    this.closeCommModal();
                    this.loading = false;
                    setTimeout(() => this.successMessage = '', 3000);
                },
                error: (err) => {
                    this.error = err.error?.Message || 'Failed to send email.';
                    this.loading = false;
                    setTimeout(() => this.error = '', 3000);
                }
            });
        } else {
            this.api.sendSms(
                this.selectedCandidateForComm.candidateId,
                '',
                this.commMessage
            ).subscribe({
                next: (res) => {
                    this.successMessage = res.Message || 'SMS sent successfully!';
                    this.closeCommModal();
                    this.loading = false;
                    setTimeout(() => this.successMessage = '', 3000);
                },
                error: (err) => {
                    this.error = err.error?.Message || 'Failed to send SMS.';
                    this.loading = false;
                    setTimeout(() => this.error = '', 3000);
                }
            });
        }
    }

    formatDate(dateStr: string): string {
        return this.datePipe.transform(dateStr, 'mediumDate') || dateStr;
    }
}
