import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ApiService, Application } from '../../services/api.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-hiring-manager',
  standalone: true,
  imports: [CommonModule],
  providers: [DatePipe],
  templateUrl: './hiring-manager.component.html'
})
export class HiringManagerComponent implements OnInit {
  applications: Application[] = [];
  isLoading = true;
  error = '';
  successMessage = '';

  constructor(private api: ApiService, private datePipe: DatePipe) {}

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

  formatDate(dateStr: string): string {
    return this.datePipe.transform(dateStr, 'mediumDate') || dateStr;
  }
}
