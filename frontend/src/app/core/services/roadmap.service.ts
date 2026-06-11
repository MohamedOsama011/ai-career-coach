import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, timeout } from 'rxjs/operators';
import { RoadmapStep } from '../models/roadmap.model';

@Injectable({
  providedIn: 'root'
})
export class RoadmapService {
  private apiUrl = 'http://localhost:5068/api/roadmap';

  constructor(private http: HttpClient) {}

  getRoadmapSteps(track: string = 'Frontend'): Observable<RoadmapStep[]> {
    return this.http.get<any[]>(`${this.apiUrl}?track=${track}`).pipe(
      timeout(7000),
      map(response => {
        if (response && response.length > 0 && response[0].steps) {
          return response[0].steps.map((step: any, idx: number) => {
            const weekNum = (idx + 1) * 2 - 1;
            const formattedWeek = `WEEK ${weekNum < 10 ? '0' + weekNum : weekNum}`;

            return {
              id: step.id || idx,
              roadmapId: step.roadmapId || response[0].id,
              title: step.title,
              description: step.description,
              level: step.level || '',
              resources: step.resources || [],
              orderIndex: step.orderIndex || idx,
              week: formattedWeek,
              status: 'upcoming'
            };
          });
        }
        return [];
      }),
      catchError((err) => {
        console.error('Failed to fetch roadmap from API', err);
        return of([]);
      })
    );
  }
}
