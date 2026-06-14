import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { 
  DashboardService, 
  DashboardMetrics, 
  DashboardSkill, 
  DashboardEvent, 
  DashboardRecommendation 
} from '../../core/services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  metrics?: DashboardMetrics;
  skills: DashboardSkill[] = [];
  events: DashboardEvent[] = [];
  recommendations: DashboardRecommendation[] = [];
  
  // Array of 12 elements to render roadmap progress bar segments
  roadmapSegments = Array(12).fill(0);

  constructor(
    private dashboardService: DashboardService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.dashboardService.getMetrics().subscribe(data => this.metrics = data);
    this.dashboardService.getSkillsGap().subscribe(data => this.skills = data);
    this.dashboardService.getUpcomingEvents().subscribe(data => this.events = data);
    this.dashboardService.getRecommendations().subscribe(data => this.recommendations = data);
  }

  startInterview(): void {
    this.router.navigate(['/interview']);
  }

  viewJobs(): void {
    this.router.navigate(['/jobs']);
  }

  viewRoadmap(): void {
    this.router.navigate(['/roadmap']);
  }

  viewSkills(): void {
    this.router.navigate(['/skills']);
  }

  viewCV(): void {
    this.router.navigate(['/cv']);
  }
}
