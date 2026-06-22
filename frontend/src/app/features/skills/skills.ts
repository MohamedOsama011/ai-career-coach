import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { SkillsService } from '../../core/services/skills.service';
import { SkillsCategoryDto } from '../../core/models/roadmap.model';

@Component({
  selector: 'app-skills',
  imports: [],
  templateUrl: './skills.html',
  styleUrl: './skills.css',
})
export class Skills implements OnInit {
  categories: SkillsCategoryDto[] = [];
  loading = true;
  errorMessage = '';
  noRoadmap = false;
  hasServerError = false;

  constructor(private skillsService: SkillsService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadSkillsAnalysis();
  }

  loadSkillsAnalysis(): void {
    this.loading = true;
    this.errorMessage = '';
    this.noRoadmap = false;
    this.hasServerError = false;

    this.skillsService.getSkillsAnalysis().subscribe({
      next: (data) => {
        this.loading = false;
        this.categories = data || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        this.categories = [];
        const status = err.status;
        if (status === 404) {
          this.noRoadmap = true;
          this.errorMessage = 'لم يتم إنشاء خطة بعد. اذهب إلى صفحة المسار الوظيفي لإنشاء واحدة.';
        } else if (status === 0) {
          this.hasServerError = true;
          this.errorMessage = 'تعذر الاتصال بالخادم. تأكد من تشغيل Backend ثم حاول مرة أخرى.';
        } else {
          this.hasServerError = true;
          this.errorMessage = `حدث خطأ (${status || 'غير معروف'}). حاول مرة أخرى لاحقاً.`;
        }
        this.cdr.detectChanges();
      }
    });
  }

  levelToPercent(level: string): number {
    const map: Record<string, number> = { 'None': 0, 'Beginner': 25, 'Intermediate': 50, 'Advanced': 75, 'Expert': 100 };
    return map[level] ?? 0;
  }

  priorityClass(priority: string): string {
    return priority === 'High' ? 'high' : priority === 'Medium' ? 'medium' : 'low';
  }
}
