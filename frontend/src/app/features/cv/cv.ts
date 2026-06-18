import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { CvService } from '../../core/services/cv.service';
import { AiService } from '../../core/services/ai.service';
import { CvFeedback, FeedbackSuggestion } from '../../core/models/cv-feedback.model';

@Component({
  selector: 'app-cv',
  imports: [CommonModule],
  templateUrl: './cv.html',
  styleUrl: './cv.css'
})
export class Cv implements OnInit {
  showCVs = signal(false);
  isUploading = signal(false);
  uploadSuccess = signal(false);
  uploadError = signal('');
  cvs = signal<any[]>([]);

  feedback = signal<CvFeedback | null>(null);
  loadingFeedback = signal(false);
  feedbackError = signal('');

  fileName = signal('No CV Uploaded');
  lastScanned = signal('-');

  private userId = '';

  constructor(
    private cvService: CvService,
    private aiService: AiService
  ) {}

  ngOnInit(): void {
    this.userId = this.getUserIdFromToken();
    this.loadCVs();
    this.loadFeedback();
  }

  onFileSelected(event: Event): void {
    this.uploadSuccess.set(false);
    this.uploadError.set('');

    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    this.isUploading.set(true);

    this.cvService.uploadCV(file, this.userId).pipe(
      finalize(() => this.isUploading.set(false))
    ).subscribe({
      next: () => {
        this.uploadSuccess.set(true);
        this.fileName.set(file.name);
        this.lastScanned.set(new Date().toLocaleString());
        this.loadCVs();
        this.loadFeedback();
        input.value = '';
        setTimeout(() => this.uploadSuccess.set(false), 3000);
      },
      error: () => {
        this.uploadError.set('Upload failed');
      }
    });
  }

  loadCVs(): void {
    if (!this.userId) return;
    this.cvService.getUserCVs(this.userId).subscribe({
      next: (response) => {
        this.cvs.set(response);
        this.showCVs.set(true);
      },
      error: (error) => console.error(error)
    });
  }

  loadFeedback(): void {
    if (!this.userId) return;
    this.loadingFeedback.set(true);
    this.feedbackError.set('');

    this.aiService.getCvFeedback(this.userId).pipe(
      finalize(() => this.loadingFeedback.set(false))
    ).subscribe({
      next: (result) => this.feedback.set(result),
      error: () => this.feedbackError.set('Could not load CV analysis. Upload a CV first.')
    });
  }

  deleteCV(cvId: number): void {
    this.cvService.deleteCV(cvId).subscribe({
      next: () => {
        this.cvs.set(this.cvs().filter(x => x.cvId !== cvId));
      },
      error: (error) => console.error(error)
    });
  }

  downloadCV(cv: any): void {
    if (!cv?.downloadUrl) return;
    window.open(cv.downloadUrl, '_blank');
  }

  downloadReport(): void {
    const cvs = this.cvs();
    if (!cvs.length) return;

    const latestCV = cvs.reduce((latest, current) =>
      new Date(current.uploadedAt) > new Date(latest.uploadedAt) ? current : latest
    );

    this.downloadCV(latestCV);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const cairoTime = new Date(date.getTime() + (3 * 60 * 60 * 1000));
    return cairoTime.toLocaleString();
  }

  getOriginalFileName(fileName: string): string {
    const index = fileName.indexOf('_');
    return index > -1 ? fileName.substring(index + 1) : fileName;
  }

  priorityClass(priority: string): string {
    const map: Record<string, string> = {
      'High': 'badge-high',
      'Medium': 'badge-medium',
      'Low': 'badge-low'
    };
    return map[priority] || 'badge-low';
  }

  private getUserIdFromToken(): string {
    const token = localStorage.getItem('authToken');
    if (!token) return '';

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload[
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
      ] || payload['nameid'] || '';
    } catch {
      return '';
    }
  }
}
