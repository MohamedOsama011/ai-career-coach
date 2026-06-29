import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-interview-progress',
  imports: [CommonModule],
  templateUrl: './interview-progress.html',
  styleUrl: './interview-progress.css',
})
export class InterviewProgress {
  current = input<number>(0);
  max = input<number>(6);
  isCompleted = input<boolean>(false);

  stepArray = computed(() => Array.from({ length: this.max() }, (_, i) => i + 1));

  percent = computed(() => {
    const m = this.max();
    if (m === 0) return 0;
    return Math.min(100, Math.round((this.current() / m) * 100));
  });

  stepClass(step: number): string {
    const current = this.current();
    if (step < current) return 'completed';
    if (step === current) return 'current';
    return 'future';
  }
}
