import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CvService } from '../../core/services/cv.service';
import { AuthService } from '../../core/services/auth.service';
import { OnInit } from '@angular/core';

@Component({
  selector: 'app-cv',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv.html',
  styleUrl: './cv.css'
})
export class Cv implements OnInit{

  constructor(
    private cvService: CvService,
    private authService: AuthService
  ) {}

  showCVs = false;
  isUploading = false;
  uploadSuccess = false;
  uploadError = '';
  cvs: any[] = [];

  userId = '';


  overallScore = 74;
  keywordMatch = 81;
  impactStatements = 62;
  formatting = 83;
  leadershipSignals = 65;

  strengths = [
    'Quantified impact with measurable outcomes across multiple projects',
    'Strong technical keyword density aligned with software engineering roles',
    'Concise and professional summary section',
    'Projects demonstrate practical experience and problem solving'
  ];

  recommendations = [
    'Add leadership outcomes and team collaboration examples',
    'Surface system design and architecture experience more prominently',
    'Replace generic wording with measurable business impact',
    'Expand project descriptions with technologies and responsibilities'
  ];

  fileName = 'No CV Uploaded';
  lastScanned = '-';

  selectedFile?: File;
  fileUrl?: string;
  cvId?: number;

  async onFileSelected(event: Event) {

    this.uploadSuccess = false;
    this.uploadError = '';

    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];

    this.cvService
      .uploadCV(file, this.userId)
      .subscribe({

        next: (res: any) => {

          this.uploadSuccess = true;

          this.fileName = file.name;

          this.lastScanned = new Date().toLocaleString();

          this.loadCVs();

          input.value = '';

          setTimeout(() => {
            this.uploadSuccess = false;
          }, 3000);
        },

        error: () => {
          this.uploadError = 'Upload failed';
        }
      });
  }

  loadCVs() {

    this.cvService
      .getUserCVs(this.userId)
      .subscribe({

        next: (response) => {

          this.cvs = response;

          this.showCVs = true;

          console.log(this.cvs);
        },

        error: (error) => {

          console.error(error);
        }
      });
  }

  deleteCV(cvId: number) {

    this.cvService
      .deleteCV(cvId)
      .subscribe({

        next: () => {

          this.cvs =
            this.cvs.filter(
              x => x.cvId !== cvId
            );
        },

        error: (error) => {

          console.error(error);
        }
      });
  }

downloadCV(cv: any) {

  if (!cv?.downloadUrl) return;

  window.open(
    `http://localhost:5068${cv.downloadUrl}`,
    '_blank'
  );
}

  downloadReport() {

    if (!this.cvs.length) return;

    const latestCV = this.cvs.reduce((latest, current) =>
      new Date(current.uploadedAt) >
      new Date(latest.uploadedAt)
        ? current
        : latest
    );

    this.downloadCV(latestCV);
  }

  formatDate(dateString: string): string {

    const date = new Date(dateString);

    const cairoTime =
      new Date(date.getTime() + (3 * 60 * 60 * 1000));

    return cairoTime.toLocaleString();
  }

  getOriginalFileName(fileName: string): string {

    const index = fileName.indexOf('_');

    return index > -1
      ? fileName.substring(index + 1)
      : fileName;
  }

private getUserIdFromToken(): string {

  const token = localStorage.getItem('authToken');

  if (!token) {
    return '';
  }

  const payload = JSON.parse(
    atob(token.split('.')[1])
  );

  return payload[
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
  ] || payload['nameid'] || '';
}

ngOnInit(): void {

  this.userId = this.getUserIdFromToken();

  this.loadCVs();
}
}