import { AfterViewInit, Component, ElementRef, ViewChild, computed, effect, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InterviewMessageDto, InterviewSessionDto } from '../../../core/models/interview.model';

export interface StreamingBubble {
  text: string;
  usedFallback: boolean;
}

@Component({
  selector: 'app-interview-chat',
  imports: [CommonModule, FormsModule],
  templateUrl: './interview-chat.html',
  styleUrl: './interview-chat.css',
})
export class InterviewChat implements AfterViewInit {
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

  messages = computed(() => this.session()?.messages ?? []);
  isCompleted = computed(() => this.session()?.status === 'Completed');
  questionsAsked = computed(() => this.session()?.questionsAsked ?? 0);
  maxQuestions = computed(() => this.session()?.maxQuestions ?? 6);
  stepArray = computed(() => Array.from({ length: this.maxQuestions() }, (_, i) => i + 1));
  showStreamingBubble = computed(() => this.streamingBubble() !== null);

  constructor() {
    effect(() => {
      this.messages();
      this.streamingBubble();
      setTimeout(() => {
        this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' });
      });
    });
  }

  ngAfterViewInit(): void {
    this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }

  messageSender(role: string): 'bot' | 'user' {
    return role === 'Interviewer' ? 'bot' : 'user';
  }

  stepClass(step: number): string {
    const current = this.questionsAsked();
    if (step < current) return 'completed';
    if (step === current) return 'current';
    return 'future';
  }

  onInputTextChange(value: string): void { this.inputTextChange.emit(value); }
  onSendMessage(): void { this.sendMessage.emit(); }
  onRetryLastAnswer(): void { this.retryLastAnswer.emit(); }
  onExit(): void { this.exit.emit(); }
  onLoadScorecard(): void { this.loadScorecard.emit(); }
  onEnterKey(event: Event): void {
    event.preventDefault();
    this.onSendMessage();
  }
}
