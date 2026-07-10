export interface NotificationDto {
  id: number;
  title: string;
  body: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  timeAgo: string;
}

export interface PaginatedNotificationsDto {
  items: NotificationDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  unreadCount: number;
}

export interface UnreadCountDto {
  count: number;
}

export interface BroadcastRequest {
  targetType: string;
  targetValue?: string;
  title: string;
  body: string;
  type: string;
}
