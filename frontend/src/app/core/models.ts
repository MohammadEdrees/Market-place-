/** Models mirrored from the .NET API (backend/Models). */

export interface MetricCard {
  key: string;
  label: string;
  value: string;
  delta: string;
  isUp: boolean;
  icon: string;
}

export interface TrendPoint {
  label: string;
  revenue: number;
  orders: number;
}

export interface CategoryShare {
  label: string;
  value: number;
}

export interface RecentOrder {
  id: number;
  customer: string;
  product: string;
  total: number;
  status: string;
  date: string;
}

export interface DashboardResponse {
  metrics: MetricCard[];
  revenueTrend: TrendPoint[];
  salesByCategory: CategoryShare[];
  recentOrders: RecentOrder[];
  lowStockCount: number;
}

export interface Product {
  id: number;
  name: string;
  sku: string;
  category: string;
  price: number;
  stock: number;
  sold: number;
  createdAt: string;
  status: string;
  /** Owner of the listing; `null` for platform demo items. */
  sellerId?: number | null;
}

export interface ProductInput {
  name: string;
  sku: string;
  category: string;
  price: number;
  stock: number;
}

/** Service listing offered by a provider/seller that clients can reserve. */
export interface ServiceListing {
  id: number;
  providerId: number;
  title: string;
  description: string;
  category: string;
  cost: number;
  contactInfo: string;
  location: string;
  offers: string;
  isActive: boolean;
  createdAt: string;
}

export interface ServiceInput {
  title: string;
  description: string;
  category: string;
  cost: number;
  contactInfo: string;
  location: string;
  offers: string;
}

/** An order: a product purchase (`Product`) or a service reservation (`Service`). */
export interface MarketOrder {
  id: number;
  customer: string;
  product: string;
  category: string;
  total: number;
  status: string;
  date: string;
  kind: 'Product' | 'Service';
  productId?: number | null;
  serviceId?: number | null;
  buyerId?: number | null;
}

/** Public user profile as returned by `/api/users` (never includes the password hash). */
export interface UserProfile {
  id: number;
  email: string;
  name: string;
  role: string;
  type: string;
  phone?: string | null;
  location?: string | null;
  bio?: string | null;
}

/** Contact fields a user may edit about themselves (`PUT /api/users/me`). */
export interface UserUpdateInput {
  name?: string | null;
  phone?: string | null;
  location?: string | null;
  bio?: string | null;
}

/** Signed-in user as returned by the auth endpoints. */
export interface AuthUser {
  id: number;
  email: string;
  name: string;
  role: string;
  type?: string;
  phone?: string | null;
  location?: string | null;
  bio?: string | null;
}

/** Response of `POST /api/auth/login`. */
export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: AuthUser;
}
