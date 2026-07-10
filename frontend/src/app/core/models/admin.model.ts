export interface DashboardStatistics {
  users: number;
  admins: number;
  cVs: number;
  interviews: number;
  totalRevenue: number;
  activeSubscriptions: number;
}

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  careerGoal: string;
  role: string;
}

export interface CVAdmin {
  id: number;
  userName: string;
  userEmail: string;
  fileName: string;
  uploadDate: string;
}

export interface UserManagement {
  id: string;
  fullName: string;
  email: string;
  role: string;
  careerGoal?: string;
  hasCv: boolean;
  interviewsCount: number;
  plan: string;
  amountPaid: number;
  createdAt: string;
}

export interface ChangeRoleRequest {
  role: string;
}

export interface AdminRoadmapStepDto {
  id: number;
  title: string;
  description: string;
  level: string;
  resources: string[];
  orderIndex: number;
}

export interface RoadmapTemplateDto {
  id: number;
  track: string;
  title: string;
  description: string;
  orderIndex: number;
  stepsCount: number;
  hasEmbedding: boolean;
  embeddingComputedAt?: string;
  steps: AdminRoadmapStepDto[];
}

export interface AdminCreateRoadmapStepDto {
  title: string;
  description: string;
  level: string;
  resources: string[];
  orderIndex: number;
}

export interface CreateRoadmapTemplateDto {
  track: string;
  title: string;
  description: string;
  orderIndex: number;
  steps: AdminCreateRoadmapStepDto[];
}

export interface UpdateRoadmapTemplateDto extends CreateRoadmapTemplateDto {}

export interface TestMatchResultDto {
  templateId: number;
  templateName: string;
  score: number;
}

export interface InterviewSessionAdminDto {
  id: number;
  userId: string;
  userName: string;
  userEmail: string;
  track: string;
  difficulty: string;
  targetRole: string;
  status: string;
  questionsAsked: number;
  maxQuestions: number;
  createdAt: string;
  completedAt?: string;
  duration: string;
  messageCount: number;
  hasScorecard: boolean;
}

export interface PaginatedSessionsResult {
  items: InterviewSessionAdminDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SyncLogDto {
  id: number;
  syncedAt: string;
  status: string;
  fetchedCount: number;
  newCount: number;
  skippedCount: number;
  embeddedCount: number;
  errorCount: number;
  errorMessages?: string;
  durationMs: number;
}

export interface UserInfoDto {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  role: string;
  careerGoal: string | null;
  createdAt: string;
}

export interface UserInterviewInfo {
  totalCount: number;
  recentSessions: SubscriberSessionDto[];
}

export interface UserDetailDto {
  user: UserInfoDto;
  cVs: SubscriberCvDto[];
  interviews: UserInterviewInfo;
  roadmaps: SubscriberRoadmapDto[];
  payments: PaymentInvoiceDto[];
}

export interface SubscriberSessionDto {
  id: number;
  track: string;
  difficulty: string;
  targetRole: string;
  status: string;
  questionsAsked: number;
  maxQuestions: number;
  createdAt: string;
}

export interface SubscriberCvDto {
  cvId: number;
  fileName: string;
  uploadedAt: string;
}

export interface SubscriberRoadmapDto {
  id: number;
  targetRole: string;
  templateTrack: string;
  createdAt: string;
}

export interface HealthComponentStatus {
  status: 'healthy' | 'unhealthy';
  message?: string;
  latencyMs?: number;
}

export interface StorageHealthStatus {
  status: 'healthy' | 'warning' | 'unhealthy';
  message?: string;
  usedPercent: number;
  usedBytes: number;
  totalBytes: number;
}

export interface HealthCheckDto {
  db: HealthComponentStatus;
  llm: HealthComponentStatus;
  jobProvider: HealthComponentStatus;
  storage: StorageHealthStatus;
  uptime: string;
  version: string;
  lastSyncTime?: string;
  lastSyncSuccess: boolean;
}

export interface PaymentInvoiceDto {
  paymentId: number;
  invoiceNumber: string | null;
  planName: string;
  amount: number;
  currency: string;
  paidAt: string;
  paymentMethod: string | null;
  transactionId: string | null;
  status: string;
}

export interface AuditLogEntry {
  id: number;
  adminUserId: string | null;
  adminUserName: string;
  action: string;
  targetType: string;
  targetId: string | null;
  details: string | null;
  timestamp: string;
}

export interface PaginatedAuditLogs {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface MonthlyPoint {
  month: string;
  count: number;
}

export interface DailyPoint {
  date: string;
  count: number;
}

export interface SimpleCount {
  label: string;
  count: number;
}

export interface ReportsDto {
  usersOverTime: MonthlyPoint[];
  interviewsPerDay: DailyPoint[];
  topRequestedRoles: SimpleCount[];
  popularSkills: SimpleCount[];
}

export interface ChatSessionAdminDto {
  id: number;
  userId: string;
  userName: string;
  userEmail: string;
  title?: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface PaginatedChatSessionsDto {
  items: ChatSessionAdminDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ChatMessageAdminDto {
  id: number;
  role: string;
  content?: string;
  toolName?: string;
  orderIndex: number;
  createdAt: string;
}
