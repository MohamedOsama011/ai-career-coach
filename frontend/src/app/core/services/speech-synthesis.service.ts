import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SpeechSynthesisService {
  private readonly synth = window.speechSynthesis;
  private currentUtterance: SpeechSynthesisUtterance | null = null;
  private queue: string[] = [];

  readonly isSupported = signal('speechSynthesis' in window);
  readonly isSpeaking = signal(false);
  readonly enabled = signal(this.loadEnabled());
  readonly voices = signal<SpeechSynthesisVoice[]>([]);
  readonly selectedVoice = signal<string>(this.loadSelectedVoice());

  constructor() {
    if (this.isSupported()) {
      this.loadVoices();
      this.synth.onvoiceschanged = () => this.loadVoices();
    }
  }

  private loadVoices(): void {
    const availableVoices = this.synth.getVoices();
    this.voices.set(availableVoices);
  }

  speak(text: string): void {
    if (!this.isSupported() || !this.enabled() || !text.trim()) return;

    if (this.isSpeaking()) {
      this.queue.push(text);
      return;
    }

    this.actuallySpeak(text);
  }

  private actuallySpeak(text: string): void {
    this.stop();

    const utterance = new SpeechSynthesisUtterance(text);
    const voice = this.voices().find(v => v.name === this.selectedVoice());
    if (voice) {
      utterance.voice = voice;
    }
    utterance.rate = 1.0;
    utterance.pitch = 1.0;

    utterance.onstart = () => this.isSpeaking.set(true);
    utterance.onend = () => {
      this.isSpeaking.set(false);
      this.currentUtterance = null;
      if (this.queue.length > 0) {
        const next = this.queue.shift()!;
        this.actuallySpeak(next);
      }
    };
    utterance.onerror = () => {
      this.isSpeaking.set(false);
      this.currentUtterance = null;
      if (this.queue.length > 0) {
        const next = this.queue.shift()!;
        this.actuallySpeak(next);
      }
    };

    this.currentUtterance = utterance;
    this.synth.speak(utterance);
  }

  stop(): void {
    if (!this.isSupported()) return;
    this.synth.cancel();
    this.queue = [];
    this.isSpeaking.set(false);
    this.currentUtterance = null;
  }

  toggleEnabled(): void {
    this.enabled.update(v => !v);
    this.saveEnabled(this.enabled());
    if (!this.enabled()) {
      this.stop();
    }
  }

  setVoice(voiceName: string): void {
    this.selectedVoice.set(voiceName);
    this.saveSelectedVoice(voiceName);
  }

  private loadEnabled(): boolean {
    try {
      return localStorage.getItem('tts-enabled') === 'true';
    } catch {
      return false;
    }
  }

  private saveEnabled(value: boolean): void {
    try {
      localStorage.setItem('tts-enabled', value.toString());
    } catch {
      // localStorage not available
    }
  }

  private loadSelectedVoice(): string {
    try {
      return localStorage.getItem('tts-voice') || '';
    } catch {
      return '';
    }
  }

  private saveSelectedVoice(voiceName: string): void {
    try {
      localStorage.setItem('tts-voice', voiceName);
    } catch {
      // localStorage not available
    }
  }
}
