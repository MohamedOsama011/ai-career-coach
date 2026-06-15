import { Component, OnInit, signal } from '@angular/core';
import { RoadmapService } from '../../core/services/roadmap.service';
import { RoadmapStep } from '../../core/models/roadmap.model';

@Component({
  selector: 'app-roadmap',
  imports: [],
  templateUrl: './roadmap.html',
  styleUrl: './roadmap.css',
})
export class Roadmap implements OnInit {
  steps = signal<RoadmapStep[]>([]);
  goalTitle = signal('Senior Frontend Engineer');
  errorMessage = signal('');

  constructor(private roadmapService: RoadmapService) {}

  ngOnInit(): void {
    this.loadRoadmap();
  }

  loadRoadmap(): void {
    this.roadmapService.getRoadmapSteps().subscribe({
      next: (data) => this.steps.set(data),
      error: (err) => {
        console.error('Failed to load roadmap', err);
        this.errorMessage.set('Failed to load roadmap data. Please try again later.');
      }
    });
  }

  modifyGoal(): void {
    const newGoal = prompt('Enter your target role:', this.goalTitle());
    if (newGoal && newGoal.trim()) {
      this.goalTitle.set(newGoal.trim());
    }
  }
}
