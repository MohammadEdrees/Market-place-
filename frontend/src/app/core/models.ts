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

/** Envelope returned by the paginated list endpoints. */
export interface PagedResponse<T> {
  /** Rows for the requested page. */
  items: T[];
  /** Total rows matching the filters, across all pages. */
  total: number;
  /** 1-based page number actually returned. */
  page: number;
  /** Rows per page actually applied. */
  pageSize: number;
}

/** Paging and sorting query parameters accepted by the list endpoints. */
export interface PageParams {
  /** 1-based page number. */
  page?: number;
  /** Rows per page (server clamps to 100). */
  pageSize?: number;
  /** Whitelisted sort column of the endpoint. */
  sortBy?: string;
  /** Sort direction (server default `asc`; orders default to newest-first when `sortBy` is absent). */
  sortDir?: 'asc' | 'desc';
}

/** Query accepted by `GET /api/products`. */
export interface ProductQuery extends PageParams {
  search?: string;
  category?: string;
  sellerId?: number;
}

/** Query accepted by `GET /api/services`. */
export interface ServiceQuery extends PageParams {
  search?: string;
  category?: string;
  providerId?: number;
}

/** Query accepted by `GET /api/orders`. */
export interface OrderQuery extends PageParams {
  search?: string;
  kind?: string;
  status?: string;
}

/** Query accepted by `GET /api/users`. */
export interface UserQuery extends PageParams {
  search?: string;
  role?: string;
  type?: string;
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
