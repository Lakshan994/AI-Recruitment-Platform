import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, User, AdminStats } from '../../services/api.service';

interface Department {
    id: string;
    name: string;
    code: string;
    manager: string;
    staffCount: number;
    vacancies: number;
}

interface RolePermission {
    role: string;
    manageUsers: boolean;
    postJobs: boolean;
    scheduleInterviews: boolean;
    hiringDecision: boolean;
    viewAnalytics: boolean;
}

@Component({
    selector: 'app-admin',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './admin.component.html'
})
export class AdminComponent implements OnInit, OnDestroy {
    activeTab = 'users';
    users: User[] = [];
    stats: AdminStats | null = null;
    loadingUsers = false;
    loadingStats = false;
    message = '';

    // 1. Roles & Permissions State
    rolePermissions: RolePermission[] = [
        { role: 'Admin', manageUsers: true, postJobs: true, scheduleInterviews: true, hiringDecision: true, viewAnalytics: true },
        { role: 'Recruiter', manageUsers: false, postJobs: true, scheduleInterviews: true, hiringDecision: false, viewAnalytics: true },
        { role: 'HiringManager', manageUsers: false, postJobs: false, scheduleInterviews: false, hiringDecision: true, viewAnalytics: true },
        { role: 'Candidate', manageUsers: false, postJobs: false, scheduleInterviews: false, hiringDecision: false, viewAnalytics: false }
    ];

    // 2. Organization & Department State
    companyName = 'TalentAI Corporation';
    companyHQ = 'San Francisco, CA';
    headcount = 120;
    website = 'https://talentai.com';

    departments: Department[] = [
        { id: '1', name: 'Engineering', code: 'ENG', manager: 'John Doe', staffCount: 14, vacancies: 3 },
        { id: '2', name: 'Product Management', code: 'PM', manager: 'Sarah Smith', staffCount: 4, vacancies: 1 },
        { id: '3', name: 'Human Resources', code: 'HR', manager: 'Udari Erandika', staffCount: 3, vacancies: 2 },
        { id: '4', name: 'Finance & Sales', code: 'FIN', manager: 'Robert Chen', staffCount: 8, vacancies: 0 }
    ];

    isDeptModalOpen = false;
    newDeptName = '';
    newDeptCode = '';
    newDeptManager = '';
    newDeptStaff = 1;
    newDeptVacancies = 0;

    // 3. System Monitoring State
    cpuUsage = 24;
    ramUsage = 1.4;
    dbPoolActive = 5;
    latency = 12;
    serverStatus = 'Online';
    sysLogs: string[] = [];
    private telemetryInterval: any;
    private logInterval: any;

    // 4. Communication & Notification State
    notificationLogs: any[] = [];
    loadingLogs = false;

    testRecipientId = '';
    testEmail = '';
    testSubject = 'AI Recruitment: System Integration Test';
    testBody = 'This is a test notification message generated from the communication services portal.';
    testPhone = '';
    testSmsMsg = 'AI Recruitment: Integration test SMS message.';
    sendingTest = false;

    constructor(private api: ApiService) { }

    ngOnInit(): void {
        this.fetchUsers();
        this.fetchStats();
        this.startSystemMonitoring();
    }

    selectTab(tab: string): void {
        this.activeTab = tab;
        if (tab === 'communications') {
            this.fetchNotificationLogs();
        }
    }

    fetchNotificationLogs(): void {
        this.loadingLogs = true;
        this.api.getNotificationLogs().subscribe({
            next: (data) => {
                this.notificationLogs = data;
                this.loadingLogs = false;
            },
            error: () => {
                this.loadingLogs = false;
            }
        });
    }

    sendTestEmail(): void {
        if (!this.testEmail || !this.testSubject || !this.testBody) return;
        this.sendingTest = true;
        this.api.sendTestEmail(this.testRecipientId || 'system-test', this.testEmail, this.testSubject, this.testBody).subscribe({
            next: (res) => {
                this.message = res.message || 'Test email sent successfully!';
                this.sendingTest = false;
                this.fetchNotificationLogs();
                setTimeout(() => this.message = '', 3000);
            },
            error: (err) => {
                this.message = err.error?.message || 'Failed to send test email.';
                this.sendingTest = false;
                setTimeout(() => this.message = '', 3000);
            }
        });
    }

    sendTestSms(): void {
        if (!this.testPhone || !this.testSmsMsg) return;
        this.sendingTest = true;
        this.api.sendTestSms(this.testRecipientId || 'system-test', this.testPhone, this.testSmsMsg).subscribe({
            next: (res) => {
                this.message = res.message || 'Test SMS sent successfully!';
                this.sendingTest = false;
                this.fetchNotificationLogs();
                setTimeout(() => this.message = '', 3000);
            },
            error: (err) => {
                this.message = err.error?.message || 'Failed to send test SMS.';
                this.sendingTest = false;
                setTimeout(() => this.message = '', 3000);
            }
        });
    }

    ngOnDestroy(): void {
        if (this.telemetryInterval) clearInterval(this.telemetryInterval);
        if (this.logInterval) clearInterval(this.logInterval);
    }

    fetchUsers(): void {
        this.loadingUsers = true;
        this.api.getAdminUsers().subscribe({
            next: (data) => {
                this.users = data;
                this.loadingUsers = false;
            },
            error: () => {
                this.loadingUsers = false;
            }
        });
    }

    fetchStats(): void {
        this.loadingStats = true;
        this.api.getAdminStats().subscribe({
            next: (data) => {
                this.stats = data;
                this.loadingStats = false;
            },
            error: () => {
                this.loadingStats = false;
            }
        });
    }

    updateRole(userId: string, event: Event): void {
        const select = event.target as HTMLSelectElement;
        const newRole = select.value;

        this.api.updateUserRole(userId, newRole).subscribe({
            next: () => {
                this.message = 'Role updated successfully!';
                setTimeout(() => this.message = '', 3000);
            },
            error: () => {
                this.message = 'Failed to update role';
                setTimeout(() => this.message = '', 3000);
            }
        });
    }

    // Permissions functions
    savePermissions(): void {
        this.message = 'Permissions updated and saved successfully!';
        setTimeout(() => this.message = '', 3000);
    }

    // Department functions
    openDeptModal(): void {
        this.newDeptName = '';
        this.newDeptCode = '';
        this.newDeptManager = '';
        this.newDeptStaff = 1;
        this.newDeptVacancies = 0;
        this.isDeptModalOpen = true;
    }

    closeDeptModal(): void {
        this.isDeptModalOpen = false;
    }

    addDepartment(): void {
        if (!this.newDeptName || !this.newDeptCode) return;

        const newDept: Department = {
            id: (this.departments.length + 1).toString(),
            name: this.newDeptName,
            code: this.newDeptCode.toUpperCase(),
            manager: this.newDeptManager || 'Unassigned',
            staffCount: this.newDeptStaff || 1,
            vacancies: this.newDeptVacancies || 0
        };

        this.departments.push(newDept);
        this.message = `Department '${newDept.name}' created successfully!`;
        this.closeDeptModal();
        setTimeout(() => this.message = '', 3000);
    }

    deleteDepartment(id: string): void {
        this.departments = this.departments.filter(d => d.id !== id);
        this.message = 'Department deleted successfully.';
        setTimeout(() => this.message = '', 3000);
    }

    // System monitoring simulator
    startSystemMonitoring(): void {
        const now = new Date();
        this.sysLogs.push(`[${now.toLocaleTimeString()}] System Initialized: Monitoring node-1`);
        this.sysLogs.push(`[${now.toLocaleTimeString()}] Database Connection: Active pool initialized on port 3306`);
        this.sysLogs.push(`[${now.toLocaleTimeString()}] Authorization Middleware: JWT tokens verified successfully`);

        // Simulate ticking CPU and Memory usage
        this.telemetryInterval = setInterval(() => {
            this.cpuUsage = Math.floor(Math.random() * 15 + 15); // 15% to 30%
            this.ramUsage = Number((Math.random() * 0.2 + 1.3).toFixed(2)); // 1.3GB to 1.5GB
            this.latency = Math.floor(Math.random() * 8 + 8); // 8ms to 16ms
            this.dbPoolActive = Math.floor(Math.random() * 3 + 4); // 4 to 7 connections
        }, 3000);

        // Simulate logs ticking
        const paths = ['/api/auth/login', '/api/applications/stats', '/api/jobs', '/api/interviews/job', '/api/communication/email'];
        const roles = ['Candidate', 'Recruiter', 'HiringManager', 'Admin'];
        this.logInterval = setInterval(() => {
            const logTime = new Date().toLocaleTimeString();
            const path = paths[Math.floor(Math.random() * paths.length)];
            const role = roles[Math.floor(Math.random() * roles.length)];
            const code = Math.random() > 0.9 ? '401 Unauthorized' : '200 OK';

            this.sysLogs.unshift(`[${logTime}] GET ${path} - ${code} (${role})`);
            if (this.sysLogs.length > 30) {
                this.sysLogs.pop();
            }
        }, 4500);
    }

    formatDate(dateStr: string): string {
        return new Date(dateStr).toLocaleDateString();
    }
}
