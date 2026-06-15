import { Component, OnInit, signal, computed } from '@angular/core';
import { JobsService} from '../../core/services/jobs.service';
import { JobCard } from '../../shared/components/job-card/job-card';
import { Job } from '../../core/models/job.model';

@Component({
  selector: 'app-jobs',
  imports: [JobCard],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css',
})
export class Jobs implements OnInit {
  jobs = signal<Job[]>([]);
  activeFilter = signal<'all' | 'remote' | 'saved'>('all');
  filteredJobs = computed(() => {
    const jobs = this.jobs();
    switch (this.activeFilter()) {
      case 'remote':
        return jobs.filter(j => j.location.toLowerCase().includes('remote'));
      case 'saved': {
        const savedIds = this.jobsService.getSavedJobIds();
        return jobs.filter(j => savedIds.includes(j.id));
      }
      default:
        return jobs;
    }
  });

  constructor(private jobsService: JobsService) {}

  ngOnInit(): void {
    this.loadJobs();
  }

  loadJobs(): void {
    this.jobsService.getJobs().subscribe({
      next: (data) => this.jobs.set(data),
      error: (err) => console.error('Failed to load jobs', err)
    });
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

  onApply(job: Job): void {
    alert(`Successfully applied for the "${job.title}" position at ${job.company}!`);
  }
}
