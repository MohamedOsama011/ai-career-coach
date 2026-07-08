import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  RoadmapTemplateDto,
  CreateRoadmapTemplateDto,
  UpdateRoadmapTemplateDto,
  TestMatchResultDto,
} from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminRoadmapService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7222/api/admin/roadmap-templates';

  getAll(): Observable<RoadmapTemplateDto[]> {
    return this.http.get<RoadmapTemplateDto[]>(this.apiUrl);
  }

  getById(id: number): Observable<RoadmapTemplateDto> {
    return this.http.get<RoadmapTemplateDto>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateRoadmapTemplateDto): Observable<RoadmapTemplateDto> {
    return this.http.post<RoadmapTemplateDto>(this.apiUrl, dto);
  }

  update(id: number, dto: UpdateRoadmapTemplateDto): Observable<RoadmapTemplateDto> {
    return this.http.put<RoadmapTemplateDto>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  testMatch(id: number, sampleCvText?: string): Observable<TestMatchResultDto> {
    return this.http.post<TestMatchResultDto>(`${this.apiUrl}/${id}/test-match`, { sampleCvText });
  }
}
