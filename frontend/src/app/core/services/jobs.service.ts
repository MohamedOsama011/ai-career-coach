import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, timeout } from 'rxjs/operators';
import { Job, JobRecommendationResult, SyncResultDto, SyncStatusDto, UpdateJobDto } from '../models/job.model';


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

  getJobs(page: number = 1, pageSize: number = 10, isRemote?: boolean, savedJobIds?: number[]): Observable<{ jobs: Job[], totalCount: number, page: number, totalPages: number, hasNext: boolean, hasPrev: boolean }> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (isRemote !== undefined) params = params.set('isRemote', isRemote);
    if (savedJobIds && savedJobIds.length > 0) params = params.set('jobIds', savedJobIds.join(','));

    return this.http.get<{ items: any[], totalCount: number, page: number, totalPages: number, hasNext: boolean, hasPrev: boolean }>(this.apiUrl, { params }).pipe(
      timeout(7000),
      map(response => {
        const jobs = (response.items || []).map(item => {
          const logo = item.company ? item.company.substring(0, 2).toUpperCase() : 'JB';
          const match = Math.min(99, Math.max(65, 95 - (item.title.length % 15)));
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
            logoInitials: logo,
            companyLogoUrl: item.companyLogoUrl || undefined,
            externalUrl: item.externalUrl || undefined,
            contractType: item.contractType || undefined,
            isRemote: item.isRemote ?? false,
            category: item.category || undefined,
            source: item.source || undefined
          };
        });
        return {
          jobs,
          totalCount: response.totalCount,
          page: response.page,
          totalPages: response.totalPages,
          hasNext: response.hasNext,
          hasPrev: response.hasPrev
        };
      })
    );
  }

  getRecommendations(): Observable<JobRecommendationResult> {
    return this.http.get<JobRecommendationResult>(`${this.apiUrl}/recommendations`).pipe(
      timeout(7000)
    );
  }

  syncJobs(): Observable<SyncResultDto> {
    return this.http.post<SyncResultDto>(`${this.apiUrl}/sync`, {}).pipe(
      timeout(30000)
    );
  }

  createJob(dto: any): Observable<Job> {
    return this.http.post<Job>(this.apiUrl, dto).pipe(
      timeout(7000)
    );
  }

  getSyncStatus(): Observable<SyncStatusDto> {
    return this.http.get<SyncStatusDto>(`${this.apiUrl}/sync-status`).pipe(
      timeout(7000)
    );
  }

  updateJob(id: number, dto: UpdateJobDto): Observable<Job> {
    return this.http.put<Job>(`${this.apiUrl}/${id}`, dto).pipe(
      timeout(7000)
    );
  }

  deleteJob(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      timeout(7000)
    );
  }
}
