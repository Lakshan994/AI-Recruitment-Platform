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

  // Secure Documents Vault State
  documents: any[] = [];
  loadingDocs = false;
  isUploadingDoc = false;
  docTypeToUpload = 'Certification';
  selectedDocFile: File | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadProfile();
    this.fetchDocuments();
  }

  fetchDocuments(): void {
    this.loadingDocs = true;
    this.api.getUserDocuments().subscribe({
      next: (data) => {
        this.documents = data;
        this.loadingDocs = false;
      },
      error: () => {
        this.documents = [];
        this.loadingDocs = false;
      }
    });
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
        if (res.parsedProfile) {
          this.profile.bio = res.parsedProfile.bio || this.profile.bio;
          this.profile.skills = res.parsedProfile.skills || this.profile.skills;
          this.profile.experience = res.parsedProfile.experience || this.profile.experience;
          this.profile.education = res.parsedProfile.education || this.profile.education;
          this.message = 'Resume uploaded and analyzed by AI. Profile fields autofilled!';
        } else {
          this.message = 'Resume uploaded successfully!';
        }
        this.isUploading = false;
        this.selectedFile = null;
        setTimeout(() => this.message = '', 5000);
      },
      error: () => {
        this.error = 'Failed to upload and parse resume.';
        this.isUploading = false;
        setTimeout(() => this.error = '', 3000);
      }
    });
  }

  onDocFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      this.selectedDocFile = file;
    }
  }

  uploadSecureDoc(): void {
    if (!this.selectedDocFile) return;

    this.isUploadingDoc = true;
    this.message = '';
    this.error = '';

    this.api.uploadDocument(this.selectedDocFile, this.docTypeToUpload).subscribe({
      next: (res) => {
        this.message = 'Document uploaded securely to Cloud Storage!';
        this.isUploadingDoc = false;
        this.selectedDocFile = null;
        this.fetchDocuments();
        setTimeout(() => this.message = '', 5000);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to upload document to Cloud Storage.';
        this.isUploadingDoc = false;
        setTimeout(() => this.error = '', 3000);
      }
    });
  }

  deleteSecureDoc(id: string): void {
    if (!confirm('Are you sure you want to delete this document from secure storage?')) return;

    this.api.deleteDocument(id).subscribe({
      next: () => {
        this.message = 'Document deleted successfully!';
        this.fetchDocuments();
        setTimeout(() => this.message = '', 3000);
      },
      error: () => {
        this.error = 'Failed to delete document.';
        setTimeout(() => this.error = '', 3000);
      }
    });
  }

  downloadSecureDoc(id: string): void {
    const url = this.api.getDocumentDownloadUrl(id);
    window.open(url, '_blank');
  }
}
