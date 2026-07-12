import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  computed,
  effect,
  inject,
  signal
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ChatService } from '../../../core/services/chat.service';
import { AuthService } from '../../../core/services/auth.service';
import { MarkdownService } from '../../../core/services/markdown.service';
import {
  ChatSession,
  ChatSessionSummary,
  TOOL_LABELS
} from '../../../core/models/chat.model';

const TOOL_ICONS: Record<string, string> = {
      get_recommended_jobs: 'travel_explore',
      get_personal_roadmap: 'route',
  analyze_cv: 'fact_check',
  get_user_profile: 'account_circle'
};

const STATUS_MESSAGES = ['Thinking…', 'Searching jobs…', 'Analyzing CV…'];
const STATUS_INTERVAL_MS = 2000;
const TEXTAREA_MAX_HEIGHT = 180;

@Component({
  selector: 'app-chat-assistant',
  imports: [],
  templateUrl: './chat-assistant.html',
  styleUrl: './chat-assistant.css',
})
export class ChatAssistant implements AfterViewInit, OnDestroy {
  private readonly chatService = inject(ChatService);
  private readonly authService = inject(AuthService);
  private readonly markdownService = inject(MarkdownService);

  readonly isOpen = signal(false);
  readonly showSessionsList = signal(false);
  readonly sessions = signal<ChatSessionSummary[]>([]);
  readonly loadingSessions = signal(false);
  readonly activeSession = signal<ChatSession | null>(null);
  readonly currentMessage = signal('');
  readonly optimisticUserMessage = signal<string | null>(null);
  readonly sending = signal(false);
  readonly error = signal<string | null>(null);
  readonly statusIndex = signal(0);

  private statusTimerId: ReturnType<typeof setInterval> | null = null;
  private sessionsLoadedOnce = false;

  readonly visible = computed(() => this.authService.isLoggedIn());
  readonly messages = computed(() => {
    const server = this.activeSession()?.messages ?? [];
    const opt = this.optimisticUserMessage();
    if (this.sending() && opt) {
      return [...server, { role: 'user' as const, content: opt }];
    }
    return server;
  });
  readonly canSend = computed(() => {
    const text = this.currentMessage().trim();
    return text.length > 0 && !this.sending() && this.visible();
  });
  readonly userInitials = computed(() => this.authService.getUserInitials());
  readonly statusMessage = computed(() => STATUS_MESSAGES[this.statusIndex() % STATUS_MESSAGES.length]);
  readonly activeSessionTitle = computed(() => this.activeSession()?.title ?? 'New chat');
  readonly activeSessionRelative = computed(() => {
    const iso = this.activeSession()?.updatedAt;
    return iso ? this.formatRelative(iso) : '';
  });
  readonly hasMessages = computed(() => this.messages().length > 0);

  @ViewChild('messageList') messageList?: ElementRef<HTMLElement>;
  @ViewChild('textarea') textarea?: ElementRef<HTMLTextAreaElement>;

  readonly TOOL_LABELS = TOOL_LABELS;

  constructor() {
    effect(() => {
      this.messages();
      setTimeout(() => this.scrollToBottom());
    });

    effect(() => {
      if (this.isOpen() && this.visible() && !this.sessionsLoadedOnce) {
        this.loadSessions();
      }
    });
  }

  ngAfterViewInit(): void {
    this.scrollToBottom();
  }

  ngOnDestroy(): void {
    this.stopStatusCycler();
  }

  toggle(): void {
    this.isOpen() ? this.close() : this.open();
  }

  open(): void {
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
    this.showSessionsList.set(false);
  }

  newChat(): void {
    this.activeSession.set(null);
    this.currentMessage.set('');
    this.optimisticUserMessage.set(null);
    this.error.set(null);
    this.showSessionsList.set(false);
  }

  openSessionsList(): void {
    this.showSessionsList.set(true);
    if (this.sessions().length === 0) {
      this.loadSessions();
    }
  }

  backFromSessions(): void {
    this.showSessionsList.set(false);
  }

  loadSessions(): void {
    this.loadingSessions.set(true);
    this.chatService.getUserSessions().subscribe({
      next: (list) => {
        this.sessions.set(list);
        this.loadingSessions.set(false);
        this.sessionsLoadedOnce = true;
      },
      error: () => {
        this.sessions.set([]);
        this.loadingSessions.set(false);
        this.sessionsLoadedOnce = true;
      }
    });
  }

  openSession(id: number): void {
    this.showSessionsList.set(false);
    this.error.set(null);
    this.chatService.getSession(id).subscribe({
      next: (session) => {
        this.activeSession.set(session);
        this.optimisticUserMessage.set(null);
        this.sending.set(false);
        this.stopStatusCycler();
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Could not load this session.');
      }
    });
  }

  updateMessage(e: Event): void {
    const target = e.target as HTMLTextAreaElement;
    this.currentMessage.set(target.value);
    this.autoResize();
    if (this.error()) {
      this.error.set(null);
    }
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey && !e.ctrlKey && !e.metaKey) {
      e.preventDefault();
      this.sendMessage();
    } else if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      this.sendMessage();
    }
  }

  async sendMessage(): Promise<void> {
    if (!this.canSend()) return;
    const text = this.currentMessage().trim();
    this.currentMessage.set('');
    this.error.set(null);
    this.optimisticUserMessage.set(text);
    this.sending.set(true);
    this.startStatusCycler();

    let session = this.activeSession();
    try {
      if (!session) {
        session = await firstValueFrom(this.chatService.createSession());
        this.activeSession.set(session);
      }
      const updated = await firstValueFrom(
        this.chatService.sendMessage(session.id, text)
      );
      this.activeSession.set(updated);
      this.upsertSessionInList({
        id: updated.id,
        title: updated.title,
        createdAt: updated.createdAt,
        updatedAt: updated.updatedAt
      });
    } catch (err: unknown) {
      const e = err as { error?: { message?: string }; message?: string };
      this.error.set(e?.error?.message ?? e?.message ?? 'Failed to send. Please try again.');
    } finally {
      this.sending.set(false);
      this.optimisticUserMessage.set(null);
      this.stopStatusCycler();
    }
  }

  dismissError(): void {
    this.error.set(null);
  }

  useSuggestion(text: string): void {
    this.currentMessage.set(text);
    setTimeout(() => this.textarea?.nativeElement?.focus());
  }

  renderMessage(content: string): string {
    return this.markdownService.render(content);
  }

  toolLabel(name: string): string {
    return TOOL_LABELS[name] ?? name;
  }

  toolIcon(name: string): string {
    return TOOL_ICONS[name] ?? 'label';
  }

  formatRelative(iso: string): string {
    const ms = new Date(iso.endsWith('Z') ? iso : iso + 'Z').getTime();
    if (Number.isNaN(ms)) return '';
    const diff = Math.floor((Date.now() - ms) / 1000);
    if (diff < 5) return 'just now';
    if (diff < 60) return `${diff} sec ago`;
    const min = Math.floor(diff / 60);
    if (min < 60) return `${min} min ago`;
    const hr = Math.floor(min / 60);
    if (hr < 24) return `${hr} hr ago`;
    const day = Math.floor(hr / 24);
    if (day < 7) return `${day} d ago`;
    return new Date(ms).toLocaleDateString();
  }

  trackBySessionId(_index: number, item: ChatSessionSummary): number {
    return item.id;
  }

  trackByMessageIndex(index: number): number {
    return index;
  }

  private startStatusCycler(): void {
    this.statusIndex.set(0);
    this.stopStatusCycler();
    this.statusTimerId = setInterval(() => {
      this.statusIndex.update(i => i + 1);
    }, STATUS_INTERVAL_MS);
  }

  private stopStatusCycler(): void {
    if (this.statusTimerId) {
      clearInterval(this.statusTimerId);
      this.statusTimerId = null;
    }
    this.statusIndex.set(0);
  }

  private upsertSessionInList(summary: ChatSessionSummary): void {
    this.sessions.update(list => {
      const idx = list.findIndex(s => s.id === summary.id);
      if (idx >= 0) {
        const copy = [...list];
        copy[idx] = summary;
        return copy;
      }
      return [summary, ...list];
    });
  }

  private autoResize(): void {
    const ta = this.textarea?.nativeElement;
    if (!ta) return;
    ta.style.height = 'auto';
    const newHeight = Math.min(ta.scrollHeight, TEXTAREA_MAX_HEIGHT);
    ta.style.height = `${newHeight}px`;
  }

  private scrollToBottom(): void {
    const el = this.messageList?.nativeElement;
    if (!el) return;
    el.scrollTop = el.scrollHeight;
  }
}
