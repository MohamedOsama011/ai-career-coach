import { Component, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { JobsService } from '../../core/services/jobs.service';
import { JobCard } from '../../shared/components/job-card/job-card';
import { Job, JobRecommendation } from '../../core/models/job.model';

@Component({
  selector: 'app-jobs',
  imports: [JobCard, DatePipe],
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

    this.jobsService.getJobs().subscribe({
      next: (data) => {
        this.jobs.set(data);
        this.jobsLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load jobs', err);
        this.jobsError.set('Failed to load jobs.');
        this.jobsLoading.set(false);
      }
    });
  }

  loadRecommendations(): void {
    this.recommendationsLoading.set(true);
    this.recommendationsError.set(null);

    this.jobsService.getRecommendations().subscribe({
      next: (result) => {
        this.recommendations.set(result?.recommendations ?? []);
        this.recommendationsGeneratedAt.set(result?.generatedAt ?? null);
        this.recommendationsLoading.set(false);
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
  }

  setFilter(filter: 'all' | 'remote' | 'saved'): void {
    this.activeFilter.set(filter);
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

  onApply(title: string, company: string): void {
    alert(`Successfully applied for the "${title}" position at ${company}!`);
  }

  matchBadgeClass(score: number): string {
    if (score >= 80) return 'badge-high';
    if (score >= 60) return 'badge-medium';
    return 'badge-low';
  }

  getInitials(company: string): string {
    return company.substring(0, 2).toUpperCase();
  }
}
