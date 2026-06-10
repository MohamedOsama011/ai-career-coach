import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-cv',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv.html',
  styleUrl: './cv.css'
})
export class Cv {

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

  onFileSelected(event: Event) {

    const input = event.target as HTMLInputElement;

    if (!input.files?.length) return;

    const file = input.files[0];

    this.selectedFile = file;
    this.fileName = file.name;
    this.lastScanned = 'Just now';

    if (this.fileUrl) {
      URL.revokeObjectURL(this.fileUrl);
    }

    this.fileUrl = URL.createObjectURL(file);
  }

  downloadReport() {

    if (!this.fileUrl) {
      alert('Please upload a CV first');
      return;
    }

    const link = document.createElement('a');

    link.href = this.fileUrl;
    link.download = this.fileName;

    link.click();
  }
}