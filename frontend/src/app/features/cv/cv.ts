import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
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
export class Cv implements OnInit, OnDestroy {
  showCVs = signal(false);
  isUploading = signal(false);
  uploadSuccess = signal(false);
  uploadError = signal('');
  cvs = signal<any[]>([]);

  feedback = signal<CvFeedback | null>(null);
  loadingFeedback = signal(false);
  feedbackError = signal('');
  displayedScore = signal(0);
  deletingCvId = signal<number | null>(null);
  deleteSuccess = signal('');
  private animationTimer: number | null = null;

  hasCV = computed(() => this.cvs().length > 0);
  fileName = signal('No CV Uploaded');
  lastScanned = signal('-');

  private userId = '';
  isDragOver = signal(false);

  constructor(
    private cvService: CvService,
    private aiService: AiService
  ) {}

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file && (file.name.endsWith('.pdf') || file.name.endsWith('.doc') || file.name.endsWith('.docx'))) {
      this.uploadFile(file);
    }
  }

  onFileSelected(event: Event): void {
    this.uploadSuccess.set(false);
    this.uploadError.set('');

    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    this.uploadFile(file);
    input.value = '';
  }

  private uploadFile(file: File): void {
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
        setTimeout(() => this.uploadSuccess.set(false), 3000);
      },
      error: () => {
        this.uploadError.set('Upload failed');
      }
    });
  }

  ngOnInit(): void {
    this.userId = this.getUserIdFromToken();
    this.loadCVs();
  }

  loadCVs(): void {
    if (!this.userId) return;
    this.cvService.getUserCVs(this.userId).subscribe({
      next: (response) => {
        this.cvs.set(response);
        this.showCVs.set(true);
        if (response.length > 0) {
          this.loadFeedback();
        }
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
      next: (result) => {
        this.feedback.set(result);
        this.startScoreAnimation(result.overallScore);
      },
      error: () => this.feedbackError.set('Could not load CV analysis. Upload a CV first.')
    });
  }

  deleteCV(cvId: number): void {
    if (!confirm('Are you sure you want to delete this CV?')) return;

    this.deletingCvId.set(cvId);
    this.deleteSuccess.set('');

    this.cvService.deleteCV(cvId).pipe(
      finalize(() => this.deletingCvId.set(null))
    ).subscribe({
      next: () => {
        const remaining = this.cvs().filter(x => x.cvId !== cvId);
        this.cvs.set(remaining);
        this.deleteSuccess.set('CV deleted successfully');
        setTimeout(() => this.deleteSuccess.set(''), 3000);

        if (remaining.length === 0) {
          this.feedback.set(null);
          this.displayedScore.set(0);
          this.fileName.set('No CV Uploaded');
          this.lastScanned.set('-');
        } else {
          this.loadFeedback();
        }
      },
      error: () => {
        this.deleteSuccess.set('');
        this.uploadError.set('Failed to delete CV');
        setTimeout(() => this.uploadError.set(''), 3000);
      }
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

  startScoreAnimation(target: number): void {
    this.displayedScore.set(0);
    if (this.animationTimer !== null) {
      clearInterval(this.animationTimer);
      this.animationTimer = null;
    }
    const duration = 800;
    const step = Math.max(1, Math.ceil(target / (duration / 16)));
    const id = setInterval(() => {
      this.displayedScore.update(prev => {
        const next = prev + step;
        if (next >= target) {
          clearInterval(id);
          this.animationTimer = null;
          return target;
        }
        return next;
      });
    }, 16);
    this.animationTimer = id;
  }

  ngOnDestroy(): void {
    if (this.animationTimer) {
      clearInterval(this.animationTimer);
      this.animationTimer = null;
    }
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
