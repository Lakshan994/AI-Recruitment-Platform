import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, User, AdminStats } from '../../services/api.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.component.html'
})
export class AdminComponent implements OnInit {
  activeTab = 'users';
  users: User[] = [];
  stats: AdminStats | null = null;
  loadingUsers = false;
  loadingStats = false;
  message = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.fetchUsers();
    this.fetchStats();
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

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString();
  }
}
