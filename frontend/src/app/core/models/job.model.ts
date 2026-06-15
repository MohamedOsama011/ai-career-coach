export interface Job {
  id: number;
  title: string;
  company: string;
  location: string;
  requiredSkills: string[];
  salary: string;
  postedAt: string;
  matchPercentage: number;
  logoInitials: string;
  saved?: boolean;
}