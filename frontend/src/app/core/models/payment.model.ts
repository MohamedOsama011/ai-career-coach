export interface GeneralResponse<T = any> {
  success: boolean;
  data: T;
}

export interface PlanLimits {
  interviewSessions: number;
  roadmapGenerations: number;
  jobRecommendations: number;
  roadmapRescan: boolean;
}

export interface SubscriptionPlan {
  id: number;
  name: string;
  price: number;
  durationMonths: number;
  limitsJson?: string | null;
}

export interface CreateSubscriptionRequest {
  name: string;
  price: number;
  durationMonths: number;
  limitsJson?: string | null;
}

export interface CreatePaymentRequest {
  planId: string;
}

export interface PaymentMethod {
  payment_method_id: number;
  name_en: string;
  name_ar: string;
  name: string;
  logo: string;
}

export interface CreatePaymentResponse {
  success: boolean;
  data: PaymentMethod[];
  userSubscriptionId: string;
}

export interface PaymentRedirectData {
  intent_key: string;
  Payment_Data: {
    RedirectTo: string;
  };
}

export interface ExecutePaymentResponse {
  status: string;
  message: string;
  data: PaymentRedirectData;
}

export interface PaymentDto {
  id: number;
  status: 'Pending' | 'Paid' | 'Failed';
  amount: number;
  invoiceNumber: string | null;
  paymentMethod: string | null;
  transactionId: string | null;
  createdAt: string;
}

export interface UserSubscriptionDto {
  id: number;
  userId: string;
  subscriptionId: number;
  isActive: boolean;
  status: 'Pending' | 'Active' | 'Cancelled' | 'Expired';
  startDate: string | null;
  endDate: string | null;
  quantity: number;
  createdAt: string;
  subscription: SubscriptionPlan | null;
  payments: PaymentDto[];
  user: SubscriberUserInfo | null;
}

export interface SubscriberUserInfo {
  id: string;
  email: string;
  fullName: string;
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

export interface PagedPaymentHistoryDto {
  items: PaymentInvoiceDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
}

export interface RevenueSummaryDto {
  currency: string;
  totalRevenue: number;
  monthlyRecurringRevenue: number;
  averageRevenuePerUser: number;
  churnRate: number;
  totalSubscribers: number;
  activeSubscribers: number;
  pendingSubscribers: number;
  cancelledSubscribers: number;
  expiredSubscribers: number;
}

export interface MonthlyRevenuePoint {
  month: string;
  monthLabel: string;
  revenue: number;
  transactionCount: number;
}

export interface PlanBreakdown {
  planId: number;
  planName: string;
  subscriberCount: number;
  activeCount: number;
  revenue: number;
  color: string;
}

export interface RecentTransaction {
  paymentId: number;
  userName: string;
  userEmail: string;
  planName: string;
  amount: number;
  status: string;
  createdAt: string;
}

export interface RevenueAnalyticsDto {
  summary: RevenueSummaryDto;
  revenueByMonth: MonthlyRevenuePoint[];
  subscriptionsByPlan: PlanBreakdown[];
  recentTransactions: RecentTransaction[];
}

export interface SubscriberUserDetail {
  id: string;
  email: string;
  fullName: string;
  phone: string | null;
  joinDate: string;
  cvCount: number;
}

export interface SubscriptionDetail {
  id: number;
  planName: string;
  status: 'Pending' | 'Active' | 'Cancelled' | 'Expired';
  isActive: boolean;
  startDate: string | null;
  endDate: string | null;
  daysRemaining: number | null;
  amount: number;
  currency: string;
}

export interface AuditLogDto {
  id: number;
  adminUserId: string;
  adminUserName: string;
  action: string;
  userSubscriptionId: number | null;
  targetUserId: string | null;
  oldValues: string | null;
  newValues: string | null;
  notes: string | null;
  createdAt: string;
}

export interface SubscriberDetailDto {
  user: SubscriberUserDetail;
  subscription: SubscriptionDetail;
  recentPayments: PaymentInvoiceDto[];
  auditLogEntries: AuditLogDto[];
  recentSessions: SubscriberSessionDto[];
  cvs: SubscriberCvDto[];
  roadmaps: SubscriberRoadmapDto[];
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

export interface ExtendSubscriptionRequest {
  additionalDays: number;
  notes?: string;
}
