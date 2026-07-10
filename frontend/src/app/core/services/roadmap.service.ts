import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, timeout } from 'rxjs/operators';
import { GenerateRoadmapRequestDto, RoadmapTemplateDto, UserRoadmapDto } from '../models/roadmap.model';


import { API_BASE_URL } from '../api-config';

@Injectable({
  providedIn: 'root'
})
export class RoadmapService {
  private apiUrl = `${API_BASE_URL}/api/roadmap`;

  constructor(private http: HttpClient) {}

  getTemplates(track?: string): Observable<RoadmapTemplateDto[]> {
    const params = track ? `?track=${encodeURIComponent(track)}` : '';
    return this.http.get<RoadmapTemplateDto[]>(`${this.apiUrl}${params}`).pipe(
      timeout(7000),
      catchError(() => of([]))
    );
  }

  generateRoadmap(req: GenerateRoadmapRequestDto): Observable<UserRoadmapDto> {
    return this.http.post<UserRoadmapDto>(`${this.apiUrl}/generate`, req).pipe(
      timeout(30000)
    );
  }

  getMyRoadmap(): Observable<UserRoadmapDto | null> {
    return this.http.get<UserRoadmapDto>(`${this.apiUrl}/my-roadmap`).pipe(
      timeout(7000),
      catchError((err) => {
        if (err.status === 404) return of(null);
        throw err;
      })
    );
  }
}
