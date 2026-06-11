import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Job } from '../models/job.model';


@Injectable({
  providedIn: 'root'
})
export class JobsService {
  private apiUrl = 'https://localhost:7222/api/job';
  
  

  private savedJobIdsSubject = new BehaviorSubject<number[]>(this.loadSavedJobIds());
  savedJobIds$ = this.savedJobIdsSubject.asObservable();

  constructor(private http: HttpClient) {}

  private loadSavedJobIds(): number[] {
    const saved = localStorage.getItem('savedJobIds');
    return saved ? JSON.parse(saved) : [];
  }

  getSavedJobIds(): number[] {
    return this.savedJobIdsSubject.value;
  }

  saveJob(id: number): void {
    const current = this.getSavedJobIds();
    if (!current.includes(id)) {
      const updated = [...current, id];
      localStorage.setItem('savedJobIds', JSON.stringify(updated));
      this.savedJobIdsSubject.next(updated);
    }
  }

  unsaveJob(id: number): void {
    const current = this.getSavedJobIds();
    const updated = current.filter(savedId => savedId !== id);
    localStorage.setItem('savedJobIds', JSON.stringify(updated));
    this.savedJobIdsSubject.next(updated);
  }

  isSaved(id: number): boolean {
    return this.getSavedJobIds().includes(id);
  }

  getJobs(): Observable<Job[]> {
    
    return this.http.get<{ items: any[] }>(this.apiUrl).pipe(
      map(response => {
        if (response && response.items && response.items.length > 0) {
          // Map backend JobDto to our frontend Job model
          return response.items.map(item => {
            const logo = item.company ? item.company.substring(0, 2).toUpperCase() : 'JB';
            // Calculate a mock match percentage based on skills or title length
            const match = Math.min(99, Math.max(65, 95 - (item.title.length % 15)));
            // Format currency
            const salaryStr = item.salary > 0 
              ? `$${Math.round(item.salary / 1000)}k` 
              : 'Competitive';

            return {
              id: item.id,
              title: item.title,
              company: item.company,
              location: item.location,
              requiredSkills: item.requiredSkills || [],
              salary: salaryStr,
              postedAt: item.postedAt,
              matchPercentage: match,
              logoInitials: logo
            };
          });
        }
        return [];
      }),
      catchError(() => {
        console.error('Failed to fetch jobs from API');
        return of([]);
      })
    );
  }
}
