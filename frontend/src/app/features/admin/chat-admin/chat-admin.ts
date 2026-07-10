import { Component, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { ChatSessionAdminDto, ChatMessageAdminDto } from '../../../core/models/admin.model';

@Component({
  selector: 'app-chat-admin',
  imports: [DatePipe],
  templateUrl: './chat-admin.html',
  styleUrl: './chat-admin.css',
})
export class ChatAdmin {
  private adminService = inject(AdminService);

  sessions = signal<ChatSessionAdminDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = signal(0);

  selectedSession = signal<ChatSessionAdminDto | null>(null);
  messages = signal<ChatMessageAdminDto[]>([]);
  messagesLoading = signal(false);

  hasPreviousPage = computed(() => this.page() > 1);
  hasNextPage = computed(() => this.page() < this.totalPages());

  constructor() {
    this.loadSessions();
  }

  loadSessions(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService.getChatSessions({ page: this.page(), pageSize: this.pageSize() }).subscribe({
      next: (res) => {
        this.sessions.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
      },
      error: () => this.error.set('Failed to load chat sessions'),
      complete: () => this.loading.set(false),
    });
  }

  selectSession(session: ChatSessionAdminDto): void {
    this.selectedSession.set(session);
    this.messagesLoading.set(true);
    this.adminService.getChatMessages(session.id).subscribe({
      next: (msgs) => this.messages.set(msgs),
      error: () => this.error.set('Failed to load messages'),
      complete: () => this.messagesLoading.set(false),
    });
  }

  backToList(): void {
    this.selectedSession.set(null);
    this.messages.set([]);
    this.loadSessions();
  }

  prevPage(): void {
    if (this.hasPreviousPage()) {
      this.page.update(p => p - 1);
      this.loadSessions();
    }
  }

  nextPage(): void {
    if (this.hasNextPage()) {
      this.page.update(p => p + 1);
      this.loadSessions();
    }
  }
}
