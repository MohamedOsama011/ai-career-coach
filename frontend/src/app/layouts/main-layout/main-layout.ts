import { Component, inject } from '@angular/core';
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
}
