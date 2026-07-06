import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RoadmapService } from '../../core/services/roadmap.service';
import { AiService } from '../../core/services/ai.service';
import { GenerateRoadmapRequestDto, RoadmapTemplateDto, UserRoadmapDto } from '../../core/models/roadmap.model';

@Component({
  selector: 'app-roadmap',
  imports: [FormsModule],
  templateUrl: './roadmap.html',
  styleUrl: './roadmap.css',
})
export class Roadmap implements OnInit {
  userRoadmap = signal<UserRoadmapDto | null>(null);
  targetRole = signal('');
  selectedTrack = signal('');
  templates = signal<RoadmapTemplateDto[]>([]);
  loading = signal(false);
  generating = signal(false);
  error = signal('');
  expandedStep = signal<number | null>(null);

  constructor(
    private roadmapService: RoadmapService,
    private aiService: AiService
  ) {}

  ngOnInit(): void {
    this.loadTemplates();
    this.loadMyRoadmap();
  }

  loadTemplates(): void {
    this.roadmapService.getTemplates().subscribe({
      next: (data) => this.templates.set(data)
    });
  }

  loadMyRoadmap(): void {
    this.loading.set(true);
    this.error.set('');
    this.roadmapService.getMyRoadmap().subscribe({
      next: (data) => {
        if (data) {
          this.userRoadmap.set(data);
          this.targetRole.set(data.targetRole);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  generateRoadmap(): void {
    if (!this.targetRole().trim()) return;

    this.generating.set(true);
    this.error.set('');

    const req: GenerateRoadmapRequestDto = {
      targetRole: this.targetRole().trim(),
      templateTrack: this.selectedTrack() || undefined,
      forceRegenerate: !!this.userRoadmap()
    };

    this.roadmapService.generateRoadmap(req).subscribe({
      next: (data) => {
        this.userRoadmap.set(data);
        this.generating.set(false);
      },
      error: (err) => {
        this.generating.set(false);
        if (err.status === 400) {
          this.error.set(err.error?.message || 'Please upload your CV and get feedback first.');
        } else {
          this.error.set('Failed to generate roadmap. Please try again.');
        }
      }
    });
  }

  toggleStep(order: number): void {
    this.expandedStep.set(this.expandedStep() === order ? null : order);
  }

  levelClass(level: string): string {
    return level.toLowerCase().replace(/ /g, '-');
  }

  getDomain(url: string): string {
    try { return new URL(url).hostname.replace('www.', ''); } catch { return url; }
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric'
    });
  }

  matchPercent(score: number): number {
    return Math.max(0, Math.min(100, Math.round(score * 100)));
  }

  downloadRoadmapReport(): void {
    this.aiService.downloadRoadmapReport().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'Roadmap_Report.pdf';
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => alert('Failed to download report')
    });
  }
}
