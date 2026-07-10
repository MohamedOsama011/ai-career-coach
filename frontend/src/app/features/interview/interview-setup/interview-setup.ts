import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InterviewOptionsDto, InterviewOptionItem } from '../../../core/models/interview.model';

@Component({
  selector: 'app-interview-setup',
  imports: [CommonModule, FormsModule],
  templateUrl: './interview-setup.html',
  styleUrl: './interview-setup.css',
})
export class InterviewSetup {
  options = input<InterviewOptionsDto | null>(null);
  selectedTrack = input<string>('');
  selectedDifficulty = input<string>('');
  targetRole = input<string>('');
  loading = input<boolean>(false);
  error = input<string | null>(null);

  selectedTrackChange = output<string>();
  selectedDifficultyChange = output<string>();
  targetRoleChange = output<string>();
  startSession = output<void>();

  tracks = computed<InterviewOptionItem[]>(() => this.options()?.tracks ?? []);
  difficulties = computed<InterviewOptionItem[]>(() => this.options()?.difficulties ?? []);
  focusAreas = computed<string[]>(() => this.options()?.focusAreas ?? []);
  canStart = computed(() =>
    !!this.selectedTrack() && !!this.selectedDifficulty() && !!this.targetRole().trim()
  );

  onSelectTrack(value: string): void { this.selectedTrackChange.emit(value); }
  onSelectDifficulty(value: string): void { this.selectedDifficultyChange.emit(value); }
  onTargetRoleChange(value: string): void { this.targetRoleChange.emit(value); }
  onStartSession(): void { this.startSession.emit(); }
  onEnterKey(event: Event): void {
    event.preventDefault();
    this.onStartSession();
  }
}
