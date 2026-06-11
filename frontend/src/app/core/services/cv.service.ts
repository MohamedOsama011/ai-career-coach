import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class CvService {
  private http = inject(HttpClient);

private apiUrl = 'http://localhost:5068/api/CV';

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

deleteCV(id:number){
  return this.http.delete(
    `${this.apiUrl}/${id}`
  );
} 
}