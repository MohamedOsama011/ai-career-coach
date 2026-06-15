import { Component, OnInit } from '@angular/core';
import { SkillsService, SkillCategory } from '../../core/services/skills.service';

@Component({
  selector: 'app-skills',
  imports: [],
  templateUrl: './skills.html',
  styleUrl: './skills.css',
})
export class Skills implements OnInit {
  categories: SkillCategory[] = [];

  constructor(private skillsService: SkillsService) {}

  ngOnInit(): void {
    this.loadSkillsAnalysis();
  }

  loadSkillsAnalysis(): void {
    this.skillsService.getSkillsAnalysis().subscribe({
      next: (data) => {
        this.categories = data;
      },
      error: (err) => {
        console.error('Failed to load skills gap analysis', err);
      }
    });
  }
}
