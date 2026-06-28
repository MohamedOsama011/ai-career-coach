import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { CvService } from '../../core/services/cv.service';
import { AiService } from '../../core/services/ai.service';
import { CareerProfileStore } from '../../core/store/career-profile-store';
import { CVResponseDto } from '../../core/models/cv.model';
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
  loadingCVs = signal(true);

  feedback = signal<CvFeedback | null>(null);
  loadingFeedback = signal(false);
  feedbackError = signal('');
  displayedScore = signal(0);
  deletingCvId = signal<number | null>(null);
  deleteSuccess = signal('');
  private animationTimer: number | null = null;

  showDiff = signal(false);
  diffCVText = signal('');
  diffLoading = signal(false);
  diffError = signal('');
  dismissedSuggestions = signal<Set<number>>(new Set());

  hasCV = computed(() => this.cvs().length > 0);
  fileName = signal('No CV Uploaded');
  lastScanned = signal('-');

  diffSegments = computed<{ start: number; end: number; index: number }[]>(() => {
    const text = this.diffCVText();
    const fb = this.feedback();
    if (!fb || !text) return [];
    const dismissed = this.dismissedSuggestions();
    const segments: { start: number; end: number; index: number }[] = [];
    fb.suggestions.forEach((s, i) => {
      if (dismissed.has(i)) return;
      if (!s.originalText) return;
      const start = text.indexOf(s.originalText);
      if (start < 0) return;
      segments.push({ start, end: start + s.originalText.length, index: i });
    });
    return segments;
  });

  diffChunks = computed<{ text: string; highlightIndex: number | null }[]>(() => {
    const text = this.diffCVText();
    const segs = this.diffSegments();
    if (!text) return [];
    if (segs.length === 0) return [{ text, highlightIndex: null }];

    const chunks: { text: string; highlightIndex: number | null }[] = [];
    let cursor = 0;
    for (const seg of segs) {
      if (seg.start > cursor) {
        chunks.push({ text: text.slice(cursor, seg.start), highlightIndex: null });
      }
      chunks.push({ text: text.slice(seg.start, seg.end), highlightIndex: seg.index });
      cursor = seg.end;
    }
    if (cursor < text.length) {
      chunks.push({ text: text.slice(cursor), highlightIndex: null });
    }
    return chunks;
  });

  private userId = '';
  isDragOver = signal(false);

  constructor(
    private cvService: CvService,
    private aiService: AiService,
    private careerProfileStore: CareerProfileStore
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
      next: (response) => {
        this.uploadSuccess.set(true);
        this.fileName.set(file.name);
        this.lastScanned.set(new Date().toLocaleString());
        this.loadCVs();
        this.loadFeedback();
        this.careerProfileStore.onCvUploaded((response as CVResponseDto).isNew);
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
    this.loadingCVs.set(true);
    this.cvService.getUserCVs(this.userId).subscribe({
      next: (response) => {
        this.cvs.set(response);
        this.showCVs.set(true);
        this.loadingCVs.set(false);
        if (response.length > 0) {
          this.loadFeedback();
        }
      },
      error: (error) => {
        console.error(error);
        this.loadingCVs.set(false);
      }
    });
  }

  loadFeedback(): void {
    if (!this.userId) return;
    this.loadingFeedback.set(true);
    this.feedbackError.set('');

    this.aiService.getCvFeedback().pipe(
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

  /** Ring geometry: circumference for radius r=52 → 2πr ≈ 326.726. */
  readonly ringCircumference = 2 * Math.PI * 52;

  /** Brand palette: blue/slate for strong, amber-only warning for weak. */
  private readonly AMBER = '#D97706';

  /** Verdict text color: deep slate-blue when strong, amber only when weak. */
  verdictColor(score: number): string {
    return score >= 65 ? '#1E3A8A' : this.AMBER;
  }

  /** Verdict label text for the overall score. */
  scoreVerdictLabel(score: number): string {
    if (score >= 85) return 'Excellent';
    if (score >= 65) return 'Strong profile';
    if (score >= 40) return 'Good progress';
    return 'Needs work';
  }

  /** SVG stroke-dashoffset for a given score (0–100) on the ring. */
  ringDashOffset(score: number): number {
    const clamped = Math.max(0, Math.min(100, score));
    return this.ringCircumference * (1 - clamped / 100);
  }

  /** Sub-score bar tier: blue for strong (≥65), amber-only warning below. */
  subBarTierClass(value: number): string {
    return value >= 65 ? 'bar--strong' : 'bar--warn';
  }

  /** Cached vs AI-generated chip (surfaces previously unused fromCache field). */
  feedbackChip(fb: CvFeedback): { label: string; icon: string } {
    return fb.fromCache
      ? { label: 'Cached', icon: 'bolt' }
      : { label: 'AI-generated', icon: 'auto_awesome' };
  }

  /** Human-readable generation date from the previously unused generatedAt field. */
  generatedDate(fb: CvFeedback): string {
    if (!fb?.generatedAt) return '';
    const d = new Date(fb.generatedAt);
    if (isNaN(d.getTime())) return '';
    return 'Generated ' + d.toLocaleDateString(undefined, {
      month: 'short', day: 'numeric', year: 'numeric'
    });
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

  openDiff(): void {
    const cvs = this.cvs();
    if (cvs.length === 0) return;
    const latestCV = cvs.reduce((latest, current) =>
      new Date(current.uploadedAt) > new Date(latest.uploadedAt) ? current : latest
    );

    this.showDiff.set(true);
    this.diffCVText.set('');
    this.diffError.set('');
    this.dismissedSuggestions.set(new Set());
    this.diffLoading.set(true);

    this.cvService.getCvText(latestCV.cvId).subscribe({
      next: (res) => {
        this.diffCVText.set(res.extractedData ?? '');
        this.diffLoading.set(false);
      },
      error: () => {
        this.diffLoading.set(false);
        this.diffError.set('Failed to load CV text. Please try again.');
      }
    });
  }

  closeDiff(): void {
    this.showDiff.set(false);
    this.diffCVText.set('');
    this.diffError.set('');
    this.dismissedSuggestions.set(new Set());
  }

  dismissSuggestion(index: number): void {
    this.dismissedSuggestions.update(set => {
      const next = new Set(set);
      next.add(index);
      return next;
    });
  }

  trackByIndex = (i: number): number => i;

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
