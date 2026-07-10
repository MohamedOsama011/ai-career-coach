import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, computed, effect, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InterviewMessageDto, InterviewSessionDto } from '../../../core/models/interview.model';
import { InterviewService } from '../../../core/services/interview.service';
import { InterviewProgress } from '../interview-progress/interview-progress';
import { MarkdownService } from '../../../core/services/markdown.service';
import { AuthService } from '../../../core/services/auth.service';
import { RoleNormalizerService } from '../../../core/services/role-normalizer.service';
import { SpeechRecognitionService } from '../../../core/services/speech-recognition.service';
import { SpeechSynthesisService } from '../../../core/services/speech-synthesis.service';

export interface StreamingBubble {
  text: string;
  usedFallback: boolean;
}

const TRACK_LABELS: Record<string, string> = {
  Behavioral: 'Behavioral',
  TechnicalCoding: 'Technical Coding',
  SystemDesign: 'System Design'
};

const TRACK_ICONS: Record<string, string> = {
  Behavioral: 'psychology',
  TechnicalCoding: 'code',
  SystemDesign: 'architecture'
};

const SOFT_CAP = 500;
const HARD_CAP = 2000;

@Component({
  selector: 'app-interview-chat',
  imports: [CommonModule, FormsModule, InterviewProgress],
  templateUrl: './interview-chat.html',
  styleUrl: './interview-chat.css',
})
export class InterviewChat implements AfterViewInit, OnDestroy {
  private readonly markdownService = inject(MarkdownService);
  private readonly authService = inject(AuthService);
  private readonly roleNormalizer = inject(RoleNormalizerService);
  private readonly interviewService = inject(InterviewService);
  private readonly speechRecognition = inject(SpeechRecognitionService);
  private readonly speechSynthesis = inject(SpeechSynthesisService);

  private autoSendTimer: ReturnType<typeof setTimeout> | null = null;

  session = input<InterviewSessionDto | null>(null);
  inputText = input<string>('');
  isBotTyping = input<boolean>(false);
  error = input<string | null>(null);
  lastSubmittedAnswer = input<string | null>(null);
  streamingBubble = input<StreamingBubble | null>(null);

  inputTextChange = output<string>();
  sendMessage = output<void>();
  retryLastAnswer = output<void>();
  exit = output<void>();
  loadScorecard = output<void>();

  @ViewChild('scrollAnchor') scrollAnchor!: ElementRef<HTMLElement>;
  @ViewChild('answerTextarea') answerTextarea!: ElementRef<HTMLTextAreaElement>;
  @ViewChild('transcriptDialogue') transcriptDialogue!: ElementRef<HTMLElement>;

  messages = computed(() => this.session()?.messages ?? []);
  isCompleted = computed(() => this.session()?.status === 'Completed');
  questionsAsked = computed(() => this.session()?.questionsAsked ?? 0);
  maxQuestions = computed(() => this.session()?.maxQuestions ?? 6);
  showStreamingBubble = computed(() => this.streamingBubble() !== null);

  userInitials = computed(() => this.authService.getUserInitials());
  userName = computed(() => this.authService.getUserFullName());

  displayRole = computed(() => this.roleNormalizer.normalize(this.session()?.targetRole));

  currentQuestion = computed(() => {
    const msgs = this.messages();
    for (let i = msgs.length - 1; i >= 0; i--) {
      if (msgs[i].role === 'Interviewer') {
        return msgs[i];
      }
    }
    return null;
  });

  completionPercent = computed(() => {
    const max = this.maxQuestions();
    if (max === 0) return 0;
    return Math.round((this.questionsAsked() / max) * 100);
  });

  encouragementMessage = computed(() => {
    const asked = this.questionsAsked();
    const max = this.maxQuestions();
    if (asked === 0) return "Let's start with something manageable.";
    if (asked >= max) return "All done! Let's review your performance.";
    const pct = asked / max;
    if (pct < 0.25) return "You're getting into a rhythm.";
    if (pct < 0.5) return "Nice momentum — keep it up.";
    if (pct < 0.75) return "Halfway there — you're doing great.";
    return "Almost there. Finish strong.";
  });

  firstBotMessageId = computed(() => {
    for (const msg of this.messages()) {
      if (msg.role === 'Interviewer') return msg.id;
    }
    return null;
  });

  charCount = computed(() => this.inputText().length);
  isOverSoftCap = computed(() => this.charCount() >= SOFT_CAP);
  isAtHardCap = computed(() => this.charCount() >= HARD_CAP);

  readonly softCap = SOFT_CAP;
  readonly hardCap = HARD_CAP;

  draftSaved = signal(false);
  private draftHideTimer: ReturnType<typeof setTimeout> | null = null;

  readonly viewingTurnIndex = signal<number | null>(null);
  readonly isQuestionExpanded = signal(false);

  readonly hint = signal<string | null>(null);
  readonly hintLoading = signal(false);
  readonly hintError = signal<string | null>(null);
  readonly hintVisible = signal(false);

  readonly isNearBottom = signal(true);
  readonly hasNewContentBelow = signal(false);
  private shouldScrollAfterSend = false;

  readonly isRecording = computed(() => this.speechRecognition.isListening());
  readonly isSpeaking = computed(() => this.speechSynthesis.isSpeaking());
  readonly ttsEnabled = computed(() => this.speechSynthesis.enabled());
  readonly sttSupported = computed(() => this.speechRecognition.isSupported());
  readonly ttsSupported = computed(() => this.speechSynthesis.isSupported());
  readonly sttError = signal<string | null>(null);

  canGoBack = computed(() => this.questionsAsked() >= 1);
  canGoForward = computed(() => this.viewingTurnIndex() !== null);

  isViewingPast = computed(() => this.viewingTurnIndex() !== null);

  displayedMessages = computed(() => {
    const idx = this.viewingTurnIndex();
    if (idx === null) return this.messages();
    return this.messages().filter(m => !m.turnNumber || m.turnNumber <= idx);
  });

  historyLabel = computed(() => {
    const idx = this.viewingTurnIndex();
    if (idx === null) return 'Live';
    return `Viewing turn ${idx} of ${this.questionsAsked()}`;
  });

  private readonly now = signal(Date.now());
  private readonly timerId = setInterval(() => this.now.set(Date.now()), 1000);

  sessionStartMs = computed(() => {
    const iso = this.session()?.createdAt;
    if (!iso) return null;
    const ms = new Date(iso.endsWith('Z') ? iso : iso + 'Z').getTime();
    return Number.isNaN(ms) ? null : ms;
  });

  private static readonly MAX_ELAPSED_DISPLAY_SEC = 7200;

  isElapsedCapped = computed(() => {
    const start = this.sessionStartMs();
    if (start === null) return false;
    return Math.floor((this.now() - start) / 1000) > InterviewChat.MAX_ELAPSED_DISPLAY_SEC;
  });

  elapsedSeconds = computed(() => {
    const start = this.sessionStartMs();
    if (start === null) return 0;
    return Math.min(
      InterviewChat.MAX_ELAPSED_DISPLAY_SEC,
      Math.max(0, Math.floor((this.now() - start) / 1000))
    );
  });

  elapsedDisplay = computed(() => {
    if (this.isElapsedCapped()) return '—';
    return this.formatElapsed(this.elapsedSeconds());
  });

  estimatedRemainingSeconds = computed(() => {
    const start = this.sessionStartMs();
    if (start === null) return null;
    const asked = this.questionsAsked();
    if (asked === 0) return null;
    const avgPerQ = this.elapsedSeconds() / asked;
    if (avgPerQ > 600) return null;
    const remaining = this.maxQuestions() - asked;
    return Math.max(0, Math.floor(avgPerQ * remaining));
  });

  remainingDisplay = computed(() => {
    const sec = this.estimatedRemainingSeconds();
    if (sec === null) {
      if (this.questionsAsked() === 0) return '~10 min';
      return '—';
    }
    if (sec < 60) return '<1 min left';
    return `~${Math.ceil(sec / 60)} min left`;
  });

  trackIcon = (track: string | undefined): string =>
    TRACK_ICONS[track ?? ''] ?? 'label';

  trackLabel = (track: string | undefined): string =>
    TRACK_LABELS[track ?? ''] ?? track ?? '';

  renderMessage(content: string | null | undefined): string {
    return this.markdownService.render(content);
  }

  renderStreamingMessage(content: string | null | undefined): string {
    return this.markdownService.render(content);
  }

  isFirstBotMessage(id: number): boolean {
    return this.firstBotMessageId() === id;
  }

  viewPrevious(): void {
    const current = this.viewingTurnIndex();
    const max = this.questionsAsked();
    if (current === null) {
      this.viewingTurnIndex.set(max);
    } else if (current > 1) {
      this.viewingTurnIndex.set(current - 1);
    }
  }

  viewNext(): void {
    const current = this.viewingTurnIndex();
    const max = this.questionsAsked();
    if (current === null) return;
    if (current >= max) {
      this.viewingTurnIndex.set(null);
    } else {
      this.viewingTurnIndex.set(current + 1);
    }
  }

  backToLive(): void {
    this.viewingTurnIndex.set(null);
  }

  toggleQuestionCard(): void {
    this.isQuestionExpanded.update(v => !v);
  }

  requestHint(): void {
    const sessionId = this.session()?.id;
    if (!sessionId || this.hintLoading() || this.isCompleted() || this.isViewingPast()) return;

    this.hintLoading.set(true);
    this.hintError.set(null);

    this.interviewService.requestHint(sessionId).subscribe({
      next: (res) => {
        this.hint.set(res.hint);
        this.hintVisible.set(true);
        this.hintLoading.set(false);
      },
      error: (err) => {
        this.hintError.set(err.error?.message ?? 'Could not get a hint right now. Please try again.');
        this.hintLoading.set(false);
      }
    });
  }

  dismissHint(): void {
    this.hintVisible.set(false);
  }

  toggleRecording(): void {
    if (!this.sttSupported() || this.isCompleted() || this.isViewingPast()) return;

    if (this.isRecording()) {
      this.speechRecognition.stop();
    } else {
      this.speechSynthesis.stop();
      this.sttError.set(null);
      this.speechRecognition.reset();
      this.speechRecognition.start();
    }
  }

  toggleTts(): void {
    this.speechSynthesis.toggleEnabled();
  }

  private setupSpeechRecognition(): void {
    this.speechRecognition.onResult((text) => {
      this.inputTextChange.emit(text);
      if (this.autoSendTimer) clearTimeout(this.autoSendTimer);
      this.autoSendTimer = setTimeout(() => {
        if (text.trim() && !this.isBotTyping() && !this.isCompleted()) {
          this.speechRecognition.stop();
          this.onSendMessage();
        }
      }, 500);
    });

    this.speechRecognition.onInterim((text) => {
      const currentFinal = this.speechRecognition.transcript();
      this.inputTextChange.emit(currentFinal + text);
    });

    this.speechRecognition.onError((error) => {
      console.error('Speech recognition error:', error);
      this.sttError.set(error === 'not-allowed' ? 'Microphone access denied' : 'Speech recognition failed');
      setTimeout(() => this.sttError.set(null), 3000);
    });

    this.speechRecognition.onEnd(() => {
      this.sttError.set(null);
    });
  }

  private draftStorageKey(): string | null {
    const id = this.session()?.id;
    return id ? `interview-draft-${id}` : null;
  }

  private saveDraft(): void {
    const key = this.draftStorageKey();
    if (!key) return;
    const text = this.inputText();
    if (text.length === 0) {
      localStorage.removeItem(key);
      this.draftSaved.set(false);
      return;
    }
    localStorage.setItem(key, text);
    this.draftSaved.set(true);
    if (this.draftHideTimer) clearTimeout(this.draftHideTimer);
    this.draftHideTimer = setTimeout(() => this.draftSaved.set(false), 1500);
  }

  private restoreDraft(): void {
    const key = this.draftStorageKey();
    if (!key) return;
    const saved = localStorage.getItem(key);
    if (saved && this.inputText() === '') {
      this.inputTextChange.emit(saved);
    }
  }

  private clearDraft(): void {
    const key = this.draftStorageKey();
    if (!key) return;
    localStorage.removeItem(key);
    this.draftSaved.set(false);
  }

  private autoResize(): void {
    const textarea = this.answerTextarea?.nativeElement;
    if (!textarea) return;
    textarea.style.height = 'auto';
    const newHeight = Math.min(textarea.scrollHeight, 180);
    textarea.style.height = `${newHeight}px`;
  }

  private scrollToBottom(immediate = false): void {
    const anchor = this.scrollAnchor?.nativeElement;
    if (!anchor) return;
    anchor.scrollIntoView({ behavior: immediate ? 'instant' : 'smooth', block: 'end' });
    this.hasNewContentBelow.set(false);
  }

  onScroll(): void {
    const container = this.transcriptDialogue?.nativeElement;
    if (!container) return;
    const threshold = 80;
    const distanceFromBottom = container.scrollHeight - container.scrollTop - container.clientHeight;
    const nearBottom = distanceFromBottom <= threshold;
    this.isNearBottom.set(nearBottom);
    if (nearBottom) {
      this.hasNewContentBelow.set(false);
    }
  }

  onScrollToBottom(): void {
    this.isNearBottom.set(true);
    this.hasNewContentBelow.set(false);
    setTimeout(() => this.scrollToBottom());
  }

  constructor() {
    this.setupSpeechRecognition();

    effect(() => {
      this.messages();
      this.streamingBubble();
      this.displayedMessages();
      const nearBottom = this.isNearBottom();
      const sendPending = this.shouldScrollAfterSend;
      if (nearBottom || sendPending) {
        this.shouldScrollAfterSend = false;
        setTimeout(() => this.scrollToBottom(true));
      } else {
        this.hasNewContentBelow.set(true);
      }
    });

    effect(() => {
      const text = this.inputText();
      const isCompleted = this.isCompleted();
      const isViewingPast = this.isViewingPast();
      if (isCompleted || isViewingPast) return;
      if (text.length === 0) {
        this.draftSaved.set(false);
        return;
      }
      const handle = setTimeout(() => this.saveDraft(), 400);
      return () => clearTimeout(handle);
    });

    effect(() => {
      const id = this.session()?.id;
      if (id) {
        this.restoreDraft();
        this.viewingTurnIndex.set(null);
        this.hint.set(null);
        this.hintError.set(null);
        this.hintVisible.set(false);
      }
    });
  }

  ngAfterViewInit(): void {
    this.scrollToBottom();
    this.autoResize();
    this.onScroll();
  }

  ngOnDestroy(): void {
    clearInterval(this.timerId);
    if (this.draftHideTimer) clearTimeout(this.draftHideTimer);
    if (this.autoSendTimer) clearTimeout(this.autoSendTimer);
    this.speechRecognition.stop();
    this.speechSynthesis.stop();
  }

  private formatElapsed(totalSeconds: number): string {
    const m = Math.floor(totalSeconds / 60);
    const s = totalSeconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  }

  messageSender(role: string): 'bot' | 'user' {
    return role === 'Interviewer' ? 'bot' : 'user';
  }

  onInputTextChange(value: string): void {
    this.inputTextChange.emit(value);
    this.autoResize();
    if (this.isSpeaking()) {
      this.speechSynthesis.stop();
    }
  }

  onSendMessage(): void {
    this.clearDraft();
    this.shouldScrollAfterSend = true;
    this.sendMessage.emit();
    setTimeout(() => this.autoResize());
  }

  onRetryLastAnswer(): void { this.retryLastAnswer.emit(); }
  onExit(): void { this.exit.emit(); }
  onLoadScorecard(): void { this.loadScorecard.emit(); }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.onSendMessage();
    }
  }
}
