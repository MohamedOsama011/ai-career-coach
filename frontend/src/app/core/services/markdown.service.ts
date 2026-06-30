import { Injectable } from '@angular/core';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

@Injectable({
  providedIn: 'root'
})
export class MarkdownService {
  private static readonly BIG_O_PATTERN = /\bO\s*\(\s*([a-zA-Z0-9_]+)\s*\)/g;
  private static readonly BIG_O_NAMESPACED = /\bO\s*\(\s*log\s+([a-zA-Z0-9_]+)\s*\)/g;
  private static readonly THETA_PATTERN = /[Θθ]\s*\(\s*([a-zA-Z0-9_]+)\s*\)/g;

  constructor() {
    marked.setOptions({
      gfm: true,
      breaks: true,
      async: false
    });
  }

  render(text: string | null | undefined): string {
    if (!text) return '';
    const normalized = this.normalizeMath(text);
    const rawHtml = marked.parse(normalized, { async: false }) as string;
    return DOMPurify.sanitize(rawHtml, {
      ALLOWED_TAGS: [
        'p', 'br', 'strong', 'em', 'code', 'pre', 'ul', 'ol', 'li',
        'a', 'blockquote', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
        'span'
      ],
      ALLOWED_ATTR: ['href', 'target', 'rel', 'class']
    });
  }

  private normalizeMath(text: string): string {
    return text
      .replace(MarkdownService.BIG_O_NAMESPACED, 'O(log $1)')
      .replace(MarkdownService.BIG_O_PATTERN, 'O($1)')
      .replace(MarkdownService.THETA_PATTERN, 'Θ($1)');
  }
}

