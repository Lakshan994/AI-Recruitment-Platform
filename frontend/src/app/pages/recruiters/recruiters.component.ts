import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Job, Application } from '../../services/api.service';
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
    }

    fetchApplications(jobId: string): void {
        this.appsLoading = true;
        this.api.getJobApplications(jobId).subscribe({
            next: (data) => {
                // Sort by match score by default
                this.applications = data.sort((a, b) => b.aiMatchScore - a.aiMatchScore);
                this.appsLoading = false;
            },
            error: () => {
                this.applications = [];
                this.appsLoading = false;
            }
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
        return skills.split(',').map(s => s.trim());
    }

    timeAgo(dateStr: string): string {
        const diff = Date.now() - new Date(dateStr).getTime();
        const days = Math.floor(diff / 86400000);
        if (days === 0) return 'Today';
        if (days === 1) return '1 day ago';
        return `${days} days ago`;
    }
}