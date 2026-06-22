import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, timeout } from 'rxjs/operators';
import { SkillsCategoryDto, UserRoadmapDto } from '../models/roadmap.model';

@Injectable({
  providedIn: 'root'
})
export class SkillsService {
  private apiUrl = 'https://localhost:7222/api/roadmap';

  constructor(private http: HttpClient) {}

  getSkillsAnalysis(): Observable<SkillsCategoryDto[]> {
    return this.http.get<UserRoadmapDto>(`${this.apiUrl}/my-roadmap`).pipe(
      timeout(7000),
      map(roadmap => roadmap.gapAnalysis)
    );
  }
}
