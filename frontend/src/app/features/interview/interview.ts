import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InterviewService, ChatMessage, InterviewTrack } from '../../core/services/interview.service';

@Component({
  selector: 'app-interview',
  imports: [CommonModule, FormsModule],
  templateUrl: './interview.html',
  styleUrl: './interview.css',
})
export class Interview implements OnInit {
  tracks: InterviewTrack[] = [];
  messages: ChatMessage[] = [];
  activeTrackId: string = 'behavioral';
  inputText: string = '';
  isListening: boolean = true;
  isBotTyping: boolean = false;

  lastScore = {
    grade: 'B+',
    track: 'Behavioral',
    feedback: 'Strong situation setup. Add measurable outcomes to your answers.'
  };

  private scoresMap: Record<string, typeof this.lastScore> = {
    behavioral: {
      grade: 'B+',
      track: 'Behavioral',
      feedback: 'Strong situation setup. Add measurable outcomes to your answers.'
    },
    technical: {
      grade: 'A-',
      track: 'Technical Coding',
      feedback: 'Excellent optimal complexity explanation. Keep optimizing boundary conditions.'
    },
    system: {
      grade: 'B',
      track: 'System Design',
      feedback: 'Good database selection analysis. Detail the load balancing strategy more.'
    }
  };

  constructor(private interviewService: InterviewService) {}

  ngOnInit(): void {
    this.loadTracks();
    this.loadMessages();
  }

  loadTracks(): void {
    this.interviewService.getTracks().subscribe(data => {
      this.tracks = data;
    });
  }

  loadMessages(): void {
    this.messages = this.interviewService.getInitialMessages(this.activeTrackId);
  }

  selectTrack(trackId: string): void {
    if (this.activeTrackId === trackId) return;
    this.activeTrackId = trackId;
    this.loadMessages();
    this.lastScore = this.scoresMap[trackId] || this.scoresMap['behavioral'];
    this.isListening = true;
    this.isBotTyping = false;
  }

  sendMessage(): void {
    const text = this.inputText.trim();
    if (!text || this.isBotTyping) return;

    // Append User Message
    const userMsg: ChatMessage = {
      id: Date.now(),
      sender: 'user',
      text: text,
      timestamp: new Date()
    };
    this.messages.push(userMsg);
    this.inputText = '';
    this.isListening = false;
    this.isBotTyping = true;

    // Simulate AI response
    this.interviewService.simulateBotReply(text).subscribe(replyText => {
      const botMsg: ChatMessage = {
        id: Date.now() + 1,
        sender: 'bot',
        text: replyText,
        timestamp: new Date()
      };
      this.messages.push(botMsg);
      this.isBotTyping = false;
      this.isListening = true;
    });
  }

  toggleListening(): void {
    this.isListening = !this.isListening;
  }

  startSession(): void {
    this.loadMessages();
    this.isListening = true;
    this.isBotTyping = false;
    alert('Started a new mock interview session!');
  }

  endSession(): void {
    alert(`Session ended. Your temporary score is ${this.lastScore.grade}. Feedback: ${this.lastScore.feedback}`);
  }
}
