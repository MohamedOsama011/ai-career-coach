import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { CvFeedback } from "../models/cv-feedback.model";
import { Injectable } from "@angular/core";


import { API_BASE_URL } from '../api-config';

@Injectable({ providedIn: 'root' })
export class AiService {
  private api = `${API_BASE_URL}/api/ai`;
  private pdfApi = `${API_BASE_URL}/api/pdf`;

  constructor(private http: HttpClient) {}

  getCvFeedback(): Observable<CvFeedback> {
    return this.http.get<CvFeedback>(`${this.api}/cv-feedback`);
  }

  downloadCvAnalysisReport(): Observable<Blob> {
    return this.http.get(`${this.pdfApi}/cv-report`, { responseType: 'blob' });
  }

  downloadRoadmapReport(): Observable<Blob> {
    return this.http.get(`${this.pdfApi}/roadmap-report`, { responseType: 'blob' });
  }

  downloadModifiedCv(modifiedText: string): Observable<Blob> {
    return this.http.post(`${this.pdfApi}/modified-cv`, { modifiedText }, { responseType: 'blob' });
  }
}
