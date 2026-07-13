import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService, Application } from '../../services/api.service';

@Component({
  selector: 'app-my-applications',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-applications.component.html'
})
export class MyApplicationsComponent implements OnInit {
  applications: Application[] = [];
  loading = true;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getMyApplications().subscribe({
      next: (data) => {
        this.applications = data;
        this.loading = false;
      },
      error: () => {
        this.applications = [];
        this.loading = false;
      }
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

  timeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const days = Math.floor(diff / 86400000);
    if (days === 0) return 'Today';
    if (days === 1) return '1 day ago';
    return `${days} days ago`;
  }
}
