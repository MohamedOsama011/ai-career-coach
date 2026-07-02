import { Injectable, signal } from '@angular/core';

interface SpeechRecognitionEvent {
  results: SpeechRecognitionResultList;
  resultIndex: number;
}

interface SpeechRecognitionErrorEvent {
  error: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class SpeechRecognitionService {
  private recognition: any = null;
  private readonly SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;

  readonly isSupported = signal(!!this.SpeechRecognition);
  readonly isListening = signal(false);
  readonly transcript = signal('');
  readonly interimTranscript = signal('');

  private onResultCallback: ((text: string) => void) | null = null;
  private onInterimCallback: ((text: string) => void) | null = null;
  private onErrorCallback: ((error: string) => void) | null = null;
  private onEndCallback: (() => void) | null = null;

  start(): void {
    if (!this.isSupported() || this.isListening()) return;

    this.recognition = new this.SpeechRecognition();
    this.recognition.continuous = true;
    this.recognition.interimResults = true;
    this.recognition.lang = 'en-US';

    this.recognition.onresult = (event: SpeechRecognitionEvent) => {
      let finalTranscript = '';
      let interimText = '';

      for (let i = event.resultIndex; i < event.results.length; i++) {
        const result = event.results[i];
        if (result.isFinal) {
          finalTranscript += result[0].transcript;
        } else {
          interimText += result[0].transcript;
        }
      }

      if (finalTranscript) {
        this.transcript.update(t => t + finalTranscript);
        this.onResultCallback?.(this.transcript());
      }

      if (interimText) {
        this.interimTranscript.set(interimText);
        this.onInterimCallback?.(interimText);
      }
    };

    this.recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
      console.error('Speech recognition error:', event.error);
      this.onErrorCallback?.(event.error);
      this.isListening.set(false);
    };

    this.recognition.onend = () => {
      this.isListening.set(false);
      this.onEndCallback?.();
    };

    try {
      this.recognition.start();
      this.isListening.set(true);
      this.transcript.set('');
      this.interimTranscript.set('');
    } catch (err) {
      console.error('Failed to start speech recognition:', err);
      this.onErrorCallback?.('start-failed');
    }
  }

  stop(): void {
    if (!this.recognition || !this.isListening()) return;
    this.recognition.stop();
    this.isListening.set(false);
  }

  reset(): void {
    this.transcript.set('');
    this.interimTranscript.set('');
  }

  onResult(callback: (text: string) => void): void {
    this.onResultCallback = callback;
  }

  onInterim(callback: (text: string) => void): void {
    this.onInterimCallback = callback;
  }

  onError(callback: (error: string) => void): void {
    this.onErrorCallback = callback;
  }

  onEnd(callback: () => void): void {
    this.onEndCallback = callback;
  }
}
