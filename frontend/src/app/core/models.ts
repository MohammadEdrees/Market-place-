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
}

export interface ProductInput {
  name: string;
  sku: string;
  category: string;
  price: number;
  stock: number;
}
