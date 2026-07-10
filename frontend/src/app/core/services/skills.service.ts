import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, timeout } from 'rxjs/operators';
import { SkillsCategoryDto, UserRoadmapDto } from '../models/roadmap.model';


import { API_BASE_URL } from '../api-config';

@Injectable({
  providedIn: 'root'
})
export class SkillsService {
  private apiUrl = `${API_BASE_URL}/api/roadmap`;

  constructor(private http: HttpClient) {}

  getSkillsAnalysis(): Observable<SkillsCategoryDto[]> {
    return this.http.get<UserRoadmapDto>(`${this.apiUrl}/my-roadmap`).pipe(
      timeout(7000),
      map(roadmap => roadmap.gapAnalysis)
    );
  }

  rescanGapAnalysis(): Observable<UserRoadmapDto> {
    return this.http.post<UserRoadmapDto>(`${this.apiUrl}/rescan-gaps`, {})
      .pipe(timeout(30000));
  }
}
