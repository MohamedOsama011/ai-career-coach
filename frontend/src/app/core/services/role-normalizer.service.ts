import { Injectable } from '@angular/core';

const ROLE_REPLACEMENTS: ReadonlyArray<readonly [RegExp, string]> = [
  [/\b(dot\s*net|dotnet)\b/gi, '.NET'],
  [/\b(c\s*sharp|csharp)\b/gi, 'C#'],
  [/\b(f\s*sharp|fsharp)\b/gi, 'F#'],
  [/\b(five\s*js|vuejs|vue\.js)\b/gi, 'Vue.js'],
  [/\b(reactjs|react\.js)\b/gi, 'React.js'],
  [/\b(nodejs|node\.js)\b/gi, 'Node.js'],
  [/\b(nextjs|next\.js)\b/gi, 'Next.js'],
  [/\b(angularjs)\b/gi, 'AngularJS'],
  [/\b(ai\/ml)\b/gi, 'AI/ML'],
  [/\b(devops)\b/gi, 'DevOps'],
  [/\b(front\s*end)\b/gi, 'Front-End'],
  [/\b(back\s*end)\b/gi, 'Back-End'],
  [/\b(full\s*stack)\b/gi, 'Full-Stack']
];

@Injectable({
  providedIn: 'root'
})
export class RoleNormalizerService {
  normalize(role: string | null | undefined): string {
    if (!role) return '';
    let result = role.trim().replace(/\s+/g, ' ');
    for (const [pattern, replacement] of ROLE_REPLACEMENTS) {
      result = result.replace(pattern, replacement);
    }
    return result;
  }
}
