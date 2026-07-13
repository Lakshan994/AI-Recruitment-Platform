import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  profile: any = {
    bio: '',
    skills: '',
    experience: '',
    education: '',
    resumeUrl: ''
  };
  
  isLoading = true;
  isSaving = false;
  isUploading = false;
  message = '';
  error = '';
  selectedFile: File | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.api.getProfile().subscribe({
      next: (data) => {
        this.profile = data;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Failed to load profile data.';
        this.isLoading = false;
      }
    });
  }

  saveProfile(): void {
    this.isSaving = true;
    this.message = '';
    this.error = '';

    const updateData = {
      bio: this.profile.bio,
      skills: this.profile.skills,
      experience: this.profile.experience,
      education: this.profile.education
    };

    this.api.updateProfile(updateData).subscribe({
      next: (res) => {
        this.message = res.message || 'Profile updated successfully!';
        this.isSaving = false;
        setTimeout(() => this.message = '', 3000);
      },
      error: () => {
        this.error = 'Failed to update profile.';
        this.isSaving = false;
        setTimeout(() => this.error = '', 3000);
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      this.selectedFile = file;
    }
  }

  uploadResume(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.message = '';
    this.error = '';

    this.api.uploadResume(this.selectedFile).subscribe({
      next: (res) => {
        this.profile.resumeUrl = res.resumeUrl;
        this.message = 'Resume uploaded successfully!';
        this.isUploading = false;
        this.selectedFile = null;
        setTimeout(() => this.message = '', 3000);
      },
      error: () => {
        this.error = 'Failed to upload resume.';
        this.isUploading = false;
        setTimeout(() => this.error = '', 3000);
      }
    });
  }
}
