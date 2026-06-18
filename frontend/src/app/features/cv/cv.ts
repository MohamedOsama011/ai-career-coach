import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { CvService } from '../../core/services/cv.service';

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

  overallScore = 74;
  keywordMatch = 81;
  impactStatements = 62;
  formatting = 83;
  leadershipSignals = 65;

  strengths = [
    'Quantified impact with measurable outcomes across multiple projects',
    'Strong technical keyword density aligned with software engineering roles',
    'Concise and professional summary section',
    'Projects demonstrate practical experience and problem solving'
  ];

  recommendations = [
    'Add leadership outcomes and team collaboration examples',
    'Surface system design and architecture experience more prominently',
    'Replace generic wording with measurable business impact',
    'Expand project descriptions with technologies and responsibilities'
  ];

  fileName = signal('No CV Uploaded');
  lastScanned = signal('-');

  private userId = '';

  constructor(private cvService: CvService) {}

  ngOnInit(): void {
    this.userId = this.getUserIdFromToken();
    this.loadCVs();
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
      next: (res: any) => {
        this.uploadSuccess.set(true);
        this.fileName.set(file.name);
        this.lastScanned.set(new Date().toLocaleString());
        this.loadCVs();
        input.value = '';
        setTimeout(() => this.uploadSuccess.set(false), 3000);
      },
      error: () => {
        this.uploadError.set('Upload failed');
      }
    });
  }

  loadCVs(): void {
    this.cvService.getUserCVs(this.userId).subscribe({
      next: (response) => {
        this.cvs.set(response);
        this.showCVs.set(true);
      },
      error: (error) => console.error(error)
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
