import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CvService } from '../../core/services/cv.service';

@Component({
  selector: 'app-cv',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv.html',
  styleUrl: './cv.css'
})
export class Cv {

  constructor(
    private cvService: CvService
  ) {}

showCVs = false;
isUploading = false;
uploadSuccess = false;
uploadError = '';
cvs: any[] = [];

  userId = '354dbafd-a999-436b-b48c-86991baf8dab';

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
    this.uploadSuccess = true;
    return;
  }

  const file = input.files[0];

  this.cvService.uploadCV(file, this.userId)
    .subscribe({
      next: (res: any) => {

        this.uploadSuccess = true;

        this.fileName = res.fileName; 

        this.lastScanned = new Date().toLocaleString();

        this.loadCVs();

        input.value = '';

        setTimeout(() => {
          this.uploadSuccess = false;
        }, 3000);
      },

      error: (err) => {
        this.uploadError = 'Upload failed';
      }
    });
}

downloadReport() {
  if (!this.cvs || this.cvs.length === 0) return;

  const latestCV = this.cvs.reduce((latest, current) => {
    return new Date(current.uploadedAt) > new Date(latest.uploadedAt)
      ? current
      : latest;
  });

  const url = `http://localhost:5068/cvs/${latestCV.fileName}`;
  window.open(url, '_blank');
}

loadCVs() {

  this.cvService
    .getUserCVs(this.userId)
    .subscribe({

      next: (response) => {

        this.cvs = response;
        this.showCVs = true;

        console.log(this.cvs);
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
    if (!cv?.fileName) return;

    const url = `http://localhost:5068/cvs/${cv.fileName}`;
    window.open(url, '_blank');
  }

formatDate(dateString: string): string {

  const date = new Date(dateString);

  const offsetInMs = 3 * 60 * 60 * 1000;

  const cairoTime = new Date(date.getTime() + offsetInMs);

  return cairoTime.toLocaleString();
}

}