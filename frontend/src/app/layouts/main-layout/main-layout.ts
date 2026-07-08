import { Component, inject, signal, HostListener } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from './sidebar';
import { Navbar } from './navbar';
import { ChatAssistant } from '../../shared/components/chat-assistant/chat-assistant';
import { UpgradeModal } from '../../shared/components/upgrade-modal/upgrade-modal';
import { CareerProfileStore } from '../../core/store/career-profile-store';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, Sidebar, Navbar, ChatAssistant, UpgradeModal],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css',
})
export class MainLayout {
  readonly store = inject(CareerProfileStore);
  readonly sidebarOpen = signal(false);

  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeSidebar();
  }
}
