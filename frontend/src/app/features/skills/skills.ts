import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
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
  private route = inject(ActivatedRoute);
  private store = inject(CareerProfileStore);

  categories = signal<SkillsCategoryDto[]>([]);
  loading = signal(true);
  rescanning = signal(false);
  errorMessage = signal('');
  noRoadmap = signal(false);
  hasServerError = signal(false);

  sortMode = signal<SkillsSortMode>(CareerProfileStore.readSortMode());

  contextJobTitle = signal<string>('');
  highlightedSkills = signal<Set<string>>(new Set());
  bannerDismissed = signal(false);

  sortedCategories = computed(() =>
    CareerProfileStore.sortCategories(this.categories(), this.sortMode())
  );

  hasContext = computed(() => !!this.contextJobTitle() && this.highlightedSkills().size > 0);
  showBanner = computed(() => this.hasContext() && !this.bannerDismissed());

  skillsNotInRoadmap = computed<string[]>(() => {
    const highlight = this.highlightedSkills();
    if (highlight.size === 0) return [];
    const inRoadmap = new Set<string>();
    for (const cat of this.categories()) {
      for (const s of cat.skills) {
        inRoadmap.add(s.skillName.toLowerCase());
      }
    }
    return Array.from(highlight).filter((s) => !inRoadmap.has(s.toLowerCase()));
  });

  ngOnInit(): void {
    const fromJob = this.route.snapshot.queryParamMap.get('fromJob');
    const highlightParam = this.route.snapshot.queryParamMap.get('highlight');
    if (fromJob) this.contextJobTitle.set(fromJob);
    if (highlightParam) {
      const set = new Set<string>(
        highlightParam
          .split(',')
          .map((s) => s.trim())
          .filter((s) => s.length > 0),
      );
      if (set.size > 0) this.highlightedSkills.set(set);
    }
    this.loadSkillsAnalysis();
    this.store.refreshGateStatus();
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
    if (!this.store.canUse('rescan')) {
      this.store.showUpgradeModal('rescan');
      return;
    }

    this.rescanning.set(true);
    this.skillsService.rescanGapAnalysis().subscribe({
      next: (updated) => {
        this.categories.set(updated.gapAnalysis);
        this.rescanning.set(false);
      },
      error: (err) => {
        this.rescanning.set(false);
        if (err.status === 403 && err.error?.code === 'LIMIT_REACHED') {
          this.store.showUpgradeModal('rescan', err.error.used, err.error.limit);
        }
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

  isHighlighted(skillName: string): boolean {
    const set = this.highlightedSkills();
    if (set.size === 0) return false;
    return set.has(skillName) || Array.from(set).some((s) => s.toLowerCase() === skillName.toLowerCase());
  }

  dismissBanner(): void {
    this.bannerDismissed.set(true);
  }
}
