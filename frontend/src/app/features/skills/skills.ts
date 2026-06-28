import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SkillsService } from '../../core/services/skills.service';
import { CareerProfileStore, SkillsSortMode } from '../../core/store/career-profile-store';
import { SkillsCategoryDto } from '../../core/models/roadmap.model';

@Component({
  selector: 'app-skills',
  imports: [CommonModule],
  templateUrl: './skills.html',
  styleUrl: './skills.css',
})
export class Skills implements OnInit {
  private skillsService = inject(SkillsService);

  categories = signal<SkillsCategoryDto[]>([]);
  loading = signal(true);
  rescanning = signal(false);
  errorMessage = signal('');
  noRoadmap = signal(false);
  hasServerError = signal(false);

  sortMode = signal<SkillsSortMode>(CareerProfileStore.readSortMode());

  sortedCategories = computed(() =>
    CareerProfileStore.sortCategories(this.categories(), this.sortMode())
  );

  ngOnInit(): void {
    this.loadSkillsAnalysis();
  }

  loadSkillsAnalysis(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.noRoadmap.set(false);
    this.hasServerError.set(false);

    this.skillsService.getSkillsAnalysis().subscribe({
      next: (data) => {
        this.loading.set(false);
        this.categories.set(data || []);
      },
      error: (err) => {
        this.loading.set(false);
        this.categories.set([]);
        const status = err.status;
        if (status === 404) {
          this.noRoadmap.set(true);
          this.errorMessage.set('لم يتم إنشاء خطة بعد. اذهب إلى صفحة المسار الوظيفي لإنشاء واحدة.');
        } else if (status === 0) {
          this.hasServerError.set(true);
          this.errorMessage.set('تعذر الاتصال بالخادم. تأكد من تشغيل Backend ثم حاول مرة أخرى.');
        } else {
          this.hasServerError.set(true);
          this.errorMessage.set(`حدث خطأ (${status || 'غير معروف'}). حاول مرة أخرى لاحقاً.`);
        }
      }
    });
  }

  setSort(mode: SkillsSortMode): void {
    this.sortMode.set(mode);
    CareerProfileStore.writeSortMode(mode);
  }

  onSortChange(event: Event): void {
    this.setSort((event.target as HTMLSelectElement).value as SkillsSortMode);
  }

  rescan(): void {
    this.rescanning.set(true);
    this.skillsService.rescanGapAnalysis().subscribe({
      next: (updated) => {
        this.categories.set(updated.gapAnalysis);
        this.rescanning.set(false);
      },
      error: () => {
        this.rescanning.set(false);
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
