import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Job, JobRecommendation } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-find-jobs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './find-jobs.component.html'
})
export class FindJobsComponent implements OnInit {
  jobs: Job[] = [];
  recommendedJobs: JobRecommendation[] = [];
  activeTab: 'all' | 'recommended' = 'all';
  search = '';
  loading = true;
  loadingRecommendations = false;
  appliedJobs = new Set<string>();
  applyingTo: string | null = null;
  message = '';

  constructor(
    private api: ApiService,
    public auth: AuthService
  ) {}


  ngOnInit(): void {
    this.fetchJobs();
    if (this.auth.isCandidate() && this.auth.getToken()) {
      this.fetchMyApplications();
      this.fetchRecommendations();
    }
  }


  fetchJobs(query = ''): void {
    this.loading = true;
    this.api.getJobs(query || undefined).subscribe({
      next: (data) => { this.jobs = data; this.loading = false; },
      error: () => { this.jobs = []; this.loading = false; }
    });
  }

  fetchMyApplications(): void {
    this.api.getMyApplications().subscribe({
      next: (apps) => { this.appliedJobs = new Set(apps.map(a => a.jobPostingId)); },
      error: () => {}
    });
  }

  fetchRecommendations(): void {
    this.loadingRecommendations = true;
    this.api.getJobRecommendations().subscribe({
      next: (data) => {
        this.recommendedJobs = data;
        this.loadingRecommendations = false;
      },
      error: () => {
        this.recommendedJobs = [];
        this.loadingRecommendations = false;
      }
    });
  }

  switchTab(tab: 'all' | 'recommended'): void {
    this.activeTab = tab;
    if (tab === 'recommended') {
      this.fetchRecommendations();
    }
  }


  onSearch(): void {
    this.fetchJobs(this.search);
  }

  applyToJob(jobId: string): void {
    if (!this.auth.getToken() || !this.auth.isCandidate()) return;
    this.applyingTo = jobId;
    this.message = '';
    this.api.apply(jobId).subscribe({
      next: (data) => {
        this.appliedJobs.add(jobId);
        this.message = `Applied! AI Match Score: ${data.matchScore}%`;
        this.applyingTo = null;
        setTimeout(() => this.message = '', 4000);
      },
      error: (err) => {
        this.message = err.error?.message || 'Failed to apply';
        this.applyingTo = null;
        setTimeout(() => this.message = '', 4000);
      }
    });
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
