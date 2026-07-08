import { Component, OnInit, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ApiService, DashboardStats } from '../../services/api.service';
import Chart from 'chart.js/auto';

@Component({
    selector: 'app-ai-insights',
    standalone: true,
    imports: [CommonModule, RouterLink],
    templateUrl: './ai-insights.component.html'
})
export class AiInsightsComponent implements OnInit, AfterViewInit {
    isLoggedIn = false;
    stats: DashboardStats | null = null;
    chart: any = null;

    @ViewChild('pieChart') pieChartRef!: ElementRef;

    constructor(public auth: AuthService, private api: ApiService) { }

    ngOnInit(): void {
        this.isLoggedIn = this.auth.isLoggedIn();
        if (this.isLoggedIn) {
            this.api.getStats().subscribe({
                next: (data) => {
                    this.stats = data;
                    this.renderChart();
                },
                error: (err) => console.error(err)
            });
        }
    }

    ngAfterViewInit() {
        this.renderChart();
    }

    renderChart() {
        if (!this.stats || !this.pieChartRef) return;

        if (this.chart) {
            this.chart.destroy();
        }

        const applied = this.stats.totalApplications - (this.stats.shortlisted + this.stats.interviewed + this.stats.hired + this.stats.rejected);
        const ctx = this.pieChartRef.nativeElement.getContext('2d');

        this.chart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Applied', 'Shortlisted', 'Interviewed', 'Hired', 'Rejected'],
                datasets: [{
                    data: [
                        Math.max(0, applied),
                        this.stats.shortlisted,
                        this.stats.interviewed,
                        this.stats.hired,
                        this.stats.rejected
                    ],
                    backgroundColor: [
                        '#3b82f6', // blue
                        '#8b5cf6', // violet
                        '#f59e0b', // amber
                        '#10b981', // emerald
                        '#ef4444'  // red
                    ],
                    borderWidth: 0,
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            color: '#94a3b8' // text-slate-400
                        }
                    }
                },
                cutout: '70%'
            }
        });
    }
}