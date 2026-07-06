import { Injectable, computed, signal } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { forkJoin, of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AiService } from '../services/ai.service';
import { RoadmapService } from '../services/roadmap.service';
import { InterviewService } from '../services/interview.service';
import { JobsService } from '../services/jobs.service';
import { UserSubscriptionService } from '../services/user-subscription.service';
import { SubscriptionGateService, SubscriptionGateStatus, GateFeature } from '../services/subscription-gate.service';
import { ProfileResponse } from '../models/user.model';
import { CvFeedback } from '../models/cv-feedback.model';
import { UserRoadmapDto, SkillGapItemDto, SkillsCategoryDto } from '../models/roadmap.model';
import { InterviewSessionDto, InterviewHistoryItemDto } from '../models/interview.model';
import { JobRecommendationResult } from '../models/job.model';
import { UserSubscriptionDto, PaymentInvoiceDto, PagedPaymentHistoryDto } from '../models/payment.model';

export type SkillsSortMode = 'priority' | 'alphabetical';
const SKILLS_SORT_KEY = 'skillsSort';

@Injectable({ providedIn: 'root' })
export class CareerProfileStore {
  private static readonly PRIORITY_ORDER: Record<string, number> = { High: 0, Medium: 1, Low: 2 };
  private static readonly LEVEL_RANK: Record<string, number> = {
    None: 0, Beginner: 1, Intermediate: 2, Advanced: 3, Expert: 4
  };

  static sortByPriority<T extends SkillGapItemDto>(skills: T[]): T[] {
    return [...skills].sort((a, b) => {
      const pa = CareerProfileStore.PRIORITY_ORDER[a.priority] ?? 99;
      const pb = CareerProfileStore.PRIORITY_ORDER[b.priority] ?? 99;
      if (pa !== pb) return pa - pb;
      return (CareerProfileStore.LEVEL_RANK[a.currentLevel] ?? 0)
           - (CareerProfileStore.LEVEL_RANK[b.currentLevel] ?? 0);
    });
  }

  static sortByName<T extends SkillGapItemDto>(skills: T[]): T[] {
    return [...skills].sort((a, b) => a.skillName.localeCompare(b.skillName));
  }

  static sortCategories(categories: SkillsCategoryDto[], mode: SkillsSortMode): SkillsCategoryDto[] {
    return categories.map(cat => ({
      ...cat,
      skills: mode === 'priority'
        ? CareerProfileStore.sortByPriority(cat.skills)
        : CareerProfileStore.sortByName(cat.skills)
    }));
  }

  static readSortMode(): SkillsSortMode {
    return localStorage.getItem(SKILLS_SORT_KEY) === 'alphabetical' ? 'alphabetical' : 'priority';
  }

  static writeSortMode(mode: SkillsSortMode): void {
    localStorage.setItem(SKILLS_SORT_KEY, mode);
  }

  private readonly _profile = signal<ProfileResponse | null>(null);
  private readonly _cvFeedback = signal<CvFeedback | null>(null);
  private readonly _userRoadmap = signal<UserRoadmapDto | null>(null);
  private readonly _activeSession = signal<InterviewSessionDto | null>(null);
  private readonly _interviewHistory = signal<InterviewHistoryItemDto[]>([]);
  private readonly _jobRecommendations = signal<JobRecommendationResult | null>(null);
  private readonly _activeSubscription = signal<UserSubscriptionDto | null>(null);
  private readonly _gateStatus = signal<SubscriptionGateStatus | null>(null);
  private readonly _upgradeModalOpen = signal<{ feature: GateFeature; used?: number; limit?: number } | null>(null);
  private readonly _paymentHistory = signal<PaymentInvoiceDto[]>([]);

  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly cvScore = computed(() => this._cvFeedback()?.overallScore ?? null);
  readonly hasCV = computed(() => (this._profile()?.cvCount ?? 0) > 0);
  readonly skillsGap = computed(() => this._userRoadmap()?.gapAnalysis ?? []);
  readonly lastInterview = computed(() => this._interviewHistory()[0] ?? null);
  readonly interviewGrade = computed(() => this.lastInterview()?.letterGrade ?? null);
  readonly interviewSessionsCount = computed(() => this._interviewHistory().length);
  readonly topRecommendations = computed(() => this._jobRecommendations()?.recommendations ?? []);
  readonly hasActiveSub = computed(() => this._activeSubscription() !== null);
  readonly planName = computed(() => this._activeSubscription()?.subscription?.name ?? null);
  readonly gateStatus = this._gateStatus.asReadonly();
  readonly upgradeModalOpen = this._upgradeModalOpen.asReadonly();

  readonly profile = this._profile.asReadonly();
  readonly cvFeedback = this._cvFeedback.asReadonly();
  readonly userRoadmap = this._userRoadmap.asReadonly();
  readonly activeSession = this._activeSession.asReadonly();
  readonly interviewHistory = this._interviewHistory.asReadonly();
  readonly jobRecommendations = this._jobRecommendations.asReadonly();
  readonly activeSubscription = this._activeSubscription.asReadonly();
  readonly paymentHistory = this._paymentHistory.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  constructor(
    private readonly authService: AuthService,
    private readonly aiService: AiService,
    private readonly roadmapService: RoadmapService,
    private readonly interviewService: InterviewService,
    private readonly jobsService: JobsService,
    private readonly userSubscriptionService: UserSubscriptionService,
    private readonly gateService: SubscriptionGateService
  ) {}

  canUse(feature: GateFeature): boolean {
    return this.gateService.canUse(this._gateStatus(), feature);
  }

  showUpgradeModal(feature: GateFeature, used?: number, limit?: number): void {
    this._upgradeModalOpen.set({ feature, used, limit });
  }

  dismissUpgradeModal(): void {
    this._upgradeModalOpen.set(null);
  }

  refreshAll(): void {
    this._loading.set(true);
    this._error.set(null);

    forkJoin({
      profile: this.authService.getProfile().pipe(catchError(() => of(null))),
      cvFeedback: this.aiService.getCvFeedback().pipe(catchError(() => of(null))),
      userRoadmap: this.roadmapService.getMyRoadmap().pipe(catchError(() => of(null))),
      activeSession: this.interviewService.getActiveSession().pipe(catchError(() => of(null))),
      interviewHistory: this.interviewService.getHistory().pipe(catchError(() => of([]))),
      jobRecommendations: this.jobsService.getRecommendations().pipe(catchError(() => of(null)))
    }).subscribe({
      next: (r) => {
        this._profile.set(r.profile);
        this._cvFeedback.set(r.cvFeedback);
        this._userRoadmap.set(r.userRoadmap);
        this._activeSession.set(r.activeSession);
        this._interviewHistory.set(r.interviewHistory);
        this._jobRecommendations.set(r.jobRecommendations);
      },
      error: (err) => this._error.set(err?.message ?? 'Failed to load profile data'),
      complete: () => this._loading.set(false)
    });

    this.refreshActiveSubscription();
    this.refreshGateStatus();
  }

  refreshActiveSubscription(): void {
    this.userSubscriptionService.getMy().pipe(catchError(() => of(null))).subscribe({
      next: (res) => {
        const subs = (res?.data as UserSubscriptionDto[] | null) ?? [];
        const now = Date.now();
        this._activeSubscription.set(
          subs.find(s =>
            s.isActive
            && s.endDate
            && new Date(s.endDate).getTime() > now
          ) ?? null
        );
      }
    });
  }

  refreshPaymentHistory(): void {
    this.userSubscriptionService.getPaymentHistory(1, 5).pipe(catchError(() => of(null))).subscribe({
      next: (res) => {
        const data = res?.data as PagedPaymentHistoryDto | null;
        this._paymentHistory.set(data?.items ?? []);
      }
    });
  }

  refreshGateStatus(): void {
    this.gateService.getStatus().subscribe({
      next: (status) => this._gateStatus.set(status)
    });
  }

  refreshProfile(): void {
    this.authService.getProfile().pipe(catchError(() => of(null))).subscribe({
      next: (p) => this._profile.set(p)
    });
  }

  refreshCvFeedback(): void {
    this.aiService.getCvFeedback().pipe(catchError(() => of(null))).subscribe({
      next: (fb) => this._cvFeedback.set(fb)
    });
  }

  refreshRoadmap(): void {
    this.roadmapService.getMyRoadmap().pipe(catchError(() => of(null))).subscribe({
      next: (r) => this._userRoadmap.set(r)
    });
  }

  refreshInterview(): void {
    forkJoin({
      active: this.interviewService.getActiveSession().pipe(catchError(() => of(null))),
      history: this.interviewService.getHistory().pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ active, history }) => {
        this._activeSession.set(active);
        this._interviewHistory.set(history);
      }
    });
  }

  refreshJobs(): void {
    this.jobsService.getRecommendations().pipe(catchError(() => of(null))).subscribe({
      next: (j) => this._jobRecommendations.set(j)
    });
  }

  onCvUploaded(isNew: boolean): void {
    if (!isNew) return;
    this.refreshCvFeedback();
    this.refreshRoadmap();
    this.refreshJobs();
    this.refreshProfile();
    this.refreshGateStatus();
  }

  refreshPayments(): void {
    this.refreshPaymentHistory();
  }
}
