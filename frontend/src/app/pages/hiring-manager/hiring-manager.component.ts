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

    formatDate(dateStr: string): string {
        return this.datePipe.transform(dateStr, 'mediumDate') || dateStr;
    }
}
