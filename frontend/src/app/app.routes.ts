import { Routes } from '@angular/router';
import { authGuard, recruiterGuard, candidateGuard, adminGuard, hiringManagerGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent) },
  { path: 'jobs', loadComponent: () => import('./pages/find-jobs/find-jobs.component').then(m => m.FindJobsComponent) },
  { path: 'recruiters', loadComponent: () => import('./pages/recruiters/recruiters.component').then(m => m.RecruitersComponent), canActivate: [recruiterGuard] },
  { path: 'hiring-manager', loadComponent: () => import('./pages/hiring-manager/hiring-manager.component').then(m => m.HiringManagerComponent), canActivate: [hiringManagerGuard] },
  { path: 'admin', loadComponent: () => import('./pages/admin/admin.component').then(m => m.AdminComponent), canActivate: [adminGuard] },
  { path: 'insights', loadComponent: () => import('./pages/ai-insights/ai-insights.component').then(m => m.AiInsightsComponent) },
  { path: 'signin', loadComponent: () => import('./pages/sign-in/sign-in.component').then(m => m.SignInComponent) },
  { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent), canActivate: [authGuard] },
  { path: 'profile', loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent), canActivate: [authGuard] },
  { path: 'my-applications', loadComponent: () => import('./pages/my-applications/my-applications.component').then(m => m.MyApplicationsComponent), canActivate: [candidateGuard] },
  { path: 'live-interview/:id', loadComponent: () => import('./pages/live-interview/live-interview.component').then(m => m.LiveInterviewComponent), canActivate: [candidateGuard] },
  { path: '**', redirectTo: '' }
];
