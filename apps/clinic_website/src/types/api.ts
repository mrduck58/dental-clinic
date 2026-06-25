// Các interface khớp với DTO trả về từ backend .NET (apps/api)

export interface ServiceDto {
  id: string;
  name: string;
  price: number;
  durationMinutes: number;
  isActive: boolean;
  description: string;
  viewCount: number;
  imageUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PostDto {
  id: string;
  title: string;
  category: string;
  author: string;
  content: string;
  thumbnailUrl?: string | null;
  isPublished: boolean;
  serviceId?: string | null;
  serviceName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  publishedAt?: string | null;
}

export interface DentistDto {
  id: string;
  fullName?: string | null;
  specialty?: string | null;
  profilePictureUrl?: string | null;
  yearsOfExperience?: number | null;
  bio?: string | null;
}

export interface FeedbackDto {
  id: string;
  customerName: string;
  rating: number;
  comment: string;
  status: string;
  replyText?: string | null;
  repliedAt?: string | null;
  createdAt: string;
}

export interface PromotionDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  discountType: string; // "Percentage" | "FixedAmount"
  discountValue: number;
  serviceIds: string[];
  serviceNames: string[];
  startDate: string; // YYYY-MM-DD
  endDate: string; // YYYY-MM-DD
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

// ── Thông tin giới thiệu phòng khám (trang chủ / giới thiệu) ─────────────────
export interface MilestoneDto {
  year: number;
  description: string;
}

export interface FeatureDto {
  title: string;
  description: string;
}

export interface TreatmentStepDto {
  title: string;
  description: string;
}

export interface StatisticDto {
  value: string;
  label: string;
}

export interface ClinicInfoDto {
  id: string;
  aboutTitle: string;
  aboutDescription: string; // các đoạn cách nhau bằng "\n\n"
  foundedYear: number;
  aboutImageUrl?: string | null;
  phone: string;
  email: string;
  address: string;
  milestones: MilestoneDto[];
  certifications: string[];
  features: FeatureDto[];
  treatmentSteps: TreatmentStepDto[];
  statistics: StatisticDto[];
  createdAt: string;
  updatedAt?: string | null;
}
