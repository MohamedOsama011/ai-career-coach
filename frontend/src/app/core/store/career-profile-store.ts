import { Injectable, computed, signal } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { forkJoin, of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AiService } from '../services/ai.service';
import { RoadmapService } from '../services/roadmap.service';
import { InterviewService } from '../services/interview.service';
import { JobsService } from '../services/jobs.service';
import { ProfileResponse } from '../models/user.model';
import { CvFeedback } from '../models/cv-feedback.model';
import { UserRoadmapDto } from '../models/roadmap.model';
import { InterviewSessionDto, InterviewHistoryItemDto } from '../models/interview.model';
import { JobRecommendationResult } from '../models/job.model';

@Injectable({ providedIn: 'root' })
export class CareerProfileStore {
  private readonly _profile = signal<ProfileResponse | null>(null);
  private readonly _cvFeedback = signal<CvFeedback | null>(null);
  private readonly _userRoadmap = signal<UserRoadmapDto | null>(null);
  private readonly _activeSession = signal<InterviewSessionDto | null>(null);
  private readonly _interviewHistory = signal<InterviewHistoryItemDto[]>([]);
  private readonly _jobRecommendations = signal<JobRecommendationResult | null>(null);

  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly cvScore = computed(() => this._cvFeedback()?.overallScore ?? null);
  readonly hasCV = computed(() => (this._profile()?.cvCount ?? 0) > 0);
  readonly skillsGap = computed(() => this._userRoadmap()?.gapAnalysis ?? []);
  readonly lastInterview = computed(() => this._interviewHistory()[0] ?? null);
  readonly interviewGrade = computed(() => this.lastInterview()?.letterGrade ?? null);
  readonly interviewSessionsCount = computed(() => this._interviewHistory().length);
  readonly topRecommendations = computed(() => this._jobRecommendations()?.recommendations ?? []);

  readonly profile = this._profile.asReadonly();
  readonly cvFeedback = this._cvFeedback.asReadonly();
  readonly userRoadmap = this._userRoadmap.asReadonly();
  readonly activeSession = this._activeSession.asReadonly();
  readonly interviewHistory = this._interviewHistory.asReadonly();
  readonly jobRecommendations = this._jobRecommendations.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  constructor(
    private readonly authService: AuthService,
    private readonly aiService: AiService,
    private readonly roadmapService: RoadmapService,
    private readonly interviewService: InterviewService,
    private readonly jobsService: JobsService
  ) {}

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
  }
}
