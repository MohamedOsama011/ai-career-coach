import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { CvFeedback } from "../models/cv-feedback.model";
import { Injectable } from "@angular/core";

@Injectable({ providedIn: 'root' })
export class AiService {
  private api = 'https://localhost:7222/api/ai';

  constructor(private http: HttpClient) {}

  getCvFeedback(userId: string): Observable<CvFeedback> {
    return this.http.get<CvFeedback>(`${this.api}/cv-feedback?userId=${userId}`);
  }
}
