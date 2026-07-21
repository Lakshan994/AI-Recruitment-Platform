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
    perfStats: any = null;
    chart: any = null;

    activeTab = 'pipeline'; // 'pipeline' or 'trends'
    trendsReport = '';
    trendsLoading = false;
    trendsError = '';
    chartError = '';

    private _pieChartRef!: ElementRef;
    @ViewChild('pieChart') set pieChart(el: ElementRef) {
        if (el) {
            this._pieChartRef = el;
            this.pieChartRef = el;
            setTimeout(() => this.renderChart(), 0);
        }
    }
    pieChartRef!: ElementRef;

    constructor(public auth: AuthService, private api: ApiService) { }

    ngOnInit(): void {
        this.isLoggedIn = this.auth.isLoggedIn();
        if (this.isLoggedIn) {
            this.api.getStats().subscribe({
                next: (data) => {
                    this.stats = data;
                    setTimeout(() => this.renderChart(), 0);
                },
                error: (err) => console.error(err)
            });

            this.api.getPerformanceAnalytics().subscribe({
                next: (data) => {
                    this.perfStats = data;
                },
                error: (err) => console.error(err)
            });
        }
    }

    selectTab(tab: string): void {
        this.activeTab = tab;
        if (tab === 'trends' && !this.trendsReport) {
            this.loadTrends();
        } else if (tab === 'pipeline') {
            setTimeout(() => this.renderChart(), 0);
        }
    }

    loadTrends(): void {
        this.trendsLoading = true;
        this.trendsError = '';
        this.api.getHiringTrends().subscribe({
            next: (res) => {
                this.trendsReport = res.report;
                this.trendsLoading = false;
            },
            error: (err) => {
                this.trendsError = 'Failed to load hiring trends report. Please configure the Gemini API key in appsettings.json.';
                this.trendsLoading = false;
            }
        });
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

        try {
            this.chartError = '';
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
        } catch (e: any) {
            this.chartError = e.message || 'Unknown error rendering chart';
            console.error(e);
        }
    }
}
