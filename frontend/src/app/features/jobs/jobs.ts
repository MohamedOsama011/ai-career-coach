import { Component, OnInit } from '@angular/core';
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
  jobs: Job[] = [];
  filteredJobs: Job[] = [];
  activeFilter: 'all' | 'remote' | 'saved' = 'all';

  constructor(private jobsService: JobsService) {}

  ngOnInit(): void {
    this.loadJobs();
  }

  loadJobs(): void {
    this.jobsService.getJobs().subscribe({
      next: (data) => {
        this.jobs = data;
        this.applyFilter();
      },
      error: (err) => {
        console.error('Failed to load jobs', err);
      }
    });
  }

  setFilter(filter: 'all' | 'remote' | 'saved'): void {
    this.activeFilter = filter;
    this.applyFilter();
  }

  applyFilter(): void {
    if (this.activeFilter === 'all') {
      this.filteredJobs = this.jobs;
    } else if (this.activeFilter === 'remote') {
      this.filteredJobs = this.jobs.filter(j => 
        j.location.toLowerCase().includes('remote')
      );
    } else if (this.activeFilter === 'saved') {
      const savedIds = this.jobsService.getSavedJobIds();
      this.filteredJobs = this.jobs.filter(j => savedIds.includes(j.id));
    }
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
    // Refresh lists
    this.applyFilter();
  }

  onApply(job: Job): void {
    alert(`Successfully applied for the "${job.title}" position at ${job.company}!`);
  }
}
