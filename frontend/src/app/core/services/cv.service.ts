import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class CvService {
  private http = inject(HttpClient);

private apiUrl = 'https://localhost:7222/api/cv';

uploadCV(file: File, userId: string) {

    const formData = new FormData();

    formData.append('file', file);

    return this.http.post(
      `${this.apiUrl}/upload?userId=${userId}`,
      formData
    );}

  getUserCVs(userId: string) {
  return this.http.get<any[]>(
    `${this.apiUrl}/user/${userId}`
  );
}

  getCvText(cvId: number): Observable<{ extractedData: string }> {
    return this.http.get<{ extractedData: string }>(
      `${this.apiUrl}/${cvId}/text`
    ).pipe(timeout(7000));
  }

deleteCV(id:number){
  return this.http.delete(
    `${this.apiUrl}/${id}`
  );
} 

getUserId(): string {

  const token = localStorage.getItem('token');

  if (!token) {
    return '';
  }

  const decoded: any = jwtDecode(token);

  return decoded[
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
  ];
}

}