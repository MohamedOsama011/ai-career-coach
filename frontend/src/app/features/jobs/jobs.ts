import { Component, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { JobsService } from '../../core/services/jobs.service';
import { JobCard } from '../../shared/components/job-card/job-card';
import { Job, JobRecommendation } from '../../core/models/job.model';

@Component({
  selector: 'app-jobs',
  imports: [JobCard, DatePipe, RouterLink],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css',
})
export class Jobs implements OnInit {
  activeView = signal<'recommendations' | 'all'>('recommendations');
  activeFilter = signal<'all' | 'remote' | 'saved'>('all');

  jobs = signal<Job[]>([]);
  recommendations = signal<JobRecommendation[]>([]);
  recommendationsGeneratedAt = signal<string | null>(null);

  jobsLoading = signal(true);
  recommendationsLoading = signal(true);
  jobsError = signal<string | null>(null);
  recommendationsError = signal<string | null>(null);

  ringReady = signal(false);

  PAGE_SIZE = 10;
  currentPage = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);

  rangeStart = computed(() => (this.currentPage() - 1) * this.PAGE_SIZE + 1);
  rangeEnd = computed(() => Math.min(this.currentPage() * this.PAGE_SIZE, this.totalCount()));

  filteredJobs = computed(() => {
    const jobs = this.jobs();
    switch (this.activeFilter()) {
      case 'remote':
        return jobs.filter(job => job.location.toLowerCase().includes('remote'));
      case 'saved': {
        const savedIds = this.jobsService.getSavedJobIds();
        return jobs.filter(job => savedIds.includes(job.id));
      }
      default:
        return jobs;
    }
  });

  constructor(private jobsService: JobsService) {}

  ngOnInit(): void {
    this.loadRecommendations();
    this.loadJobs();
  }

  loadJobs(): void {
    this.jobsLoading.set(true);
    this.jobsError.set(null);

    const filter = this.activeFilter();
    const isRemote = filter === 'remote' ? true : undefined;
    const savedJobIds = filter === 'saved' ? this.jobsService.getSavedJobIds() : undefined;

    this.jobsService.getJobs(this.currentPage(), this.PAGE_SIZE, isRemote, savedJobIds).subscribe({
      next: (result) => {
        this.jobs.set(result.jobs);
        this.totalPages.set(result.totalPages);
        this.totalCount.set(result.totalCount);
        this.jobsLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load jobs', err);
        this.jobsError.set('Failed to load jobs.');
        this.jobsLoading.set(false);
      }
    });
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadJobs();
    }
  }

  prevPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadJobs();
    }
  }

  loadRecommendations(): void {
    this.recommendationsLoading.set(true);
    this.recommendationsError.set(null);

    this.jobsService.getRecommendations().subscribe({
      next: (result) => {
        this.recommendations.set(result?.recommendations ?? []);
        this.recommendationsGeneratedAt.set(result?.generatedAt ?? null);
        this.recommendationsLoading.set(false);
        setTimeout(() => this.ringReady.set(true), 50);
      },
      error: (err) => {
        console.error('Failed to load recommendations', err);
        this.recommendationsError.set('Failed to load recommendations.');
        this.recommendationsLoading.set(false);
      }
    });
  }

  setView(view: 'recommendations' | 'all'): void {
    this.activeView.set(view);
    if (view !== 'recommendations') {
      this.ringReady.set(false);
    } else {
      setTimeout(() => this.ringReady.set(true), 50);
    }
  }

  setFilter(filter: 'all' | 'remote' | 'saved'): void {
    this.activeFilter.set(filter);
    this.currentPage.set(1);
    this.loadJobs();
  }

  isSaved(jobId: number): boolean {
    return this.jobsService.isSaved(jobId);
  }

  toggleSave(jobId: number): void {
    if (this.isSaved(jobId)) {
      this.jobsService.unsaveJob(jobId);
    } else {
      this.jobsService.saveJob(jobId);
    }
  }

  applyToRecommendation(job: JobRecommendation): void {
    if (job.externalUrl) {
      window.open(job.externalUrl, '_blank', 'noopener,noreferrer');
    } else {
      alert('No external link yet — clear the recommendations cache and try again.');
    }
  }

  applyToJob(job: Job): void {
    if (job.externalUrl) {
      window.open(job.externalUrl, '_blank', 'noopener,noreferrer');
    } else {
      alert('No external link yet — clear the recommendations cache and try again.');
    }
  }

  matchBadgeClass(score: number): string {
    if (score >= 65) return 'badge-high';
    if (score >= 60) return 'badge-medium';
    return 'badge-low';
  }

  getRingOffset(score: number): string {
    const r = 28;
    const c = 2 * Math.PI * r;
    return String(c * (1 - score / 100));
  }

  getInitials(company: string): string {
    return company.substring(0, 2).toUpperCase();
  }

  formatSalary(salary: number): string {
    if (salary <= 0) return 'Comp.';
    if (salary >= 1000) return `${(salary / 1000).toFixed(0)}k`;
    return salary.toString();
  }

  displaySalary(salary: string | number): string {
    const num = typeof salary === 'string' ? parseFloat(salary) : salary;
    if (isNaN(num) || num <= 0) return 'Comp.';
    if (num >= 1000) return `${(num / 1000).toFixed(0)}k`;
    return num.toString();
  }

  expandedExplanations = signal<Set<number>>(new Set());

  toggleExplanation(jobId: number): void {
    this.expandedExplanations.update(set => {
      const newSet = new Set(set);
      if (newSet.has(jobId)) {
        newSet.delete(jobId);
      } else {
        newSet.add(jobId);
      }
      return newSet;
    });
  }

  isExplanationExpanded(jobId: number): boolean {
    return this.expandedExplanations().has(jobId);
  }
}
