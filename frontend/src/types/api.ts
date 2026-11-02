// These mirror the C# DTOs in the backend. JSON uses camelCase: ShopName → shopName.

// one page of results (C#: PagedResult<T>)
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// a shop (C#: ShopResponse)
export interface ShopResponse {
  id: number;
  name: string;
  slug: string;
  description: string | null;
  city: string | null;
  brandColor: string | null;
}

// a product in the public catalog (C#: CatalogProductResponse)
export interface CatalogProductResponse {
  id: number;
  name: string;
  description: string | null;
  price: number;
  imageUrl: string | null;
  categoryName: string;
  categorySlug: string;
  shopName: string;
  shopSlug: string;
  shopBrandColor: string | null;
  inStock: boolean;
  onlyLeft: number | null;
}

// a category button (C#: CategoryResponse)
export interface CategoryResponse {
  id: number;
  name: string;
  slug: string;
  productCount: number;
}
// POST /api/auth/login (C#: LoginResponse)
export interface LoginResponse {
  token: string;
  expiresAt: string;
  email: string;
  firstName: string;
  roles: string[];
  tenantId: number | null;
}

export type OrderStatus = "Pending" | "Processing" | "Shipped" | "Delivered" | "Cancelled";

export interface OrderItemResponse {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface OrderStatusHistoryResponse {
  status: OrderStatus;
  changedAt: string;
  note: string | null;
}

// one order of ONE shop (C#: OrderResponse)
export interface OrderResponse {
  id: number;
  checkoutId: string;
  shopName: string;
  shopSlug: string;
  status: OrderStatus;
  totalAmount: number;
  createdAt: string;
  items: OrderItemResponse[];
  statusHistory: OrderStatusHistoryResponse[];
}

// the receipt of one checkout (C#: CheckoutResponse)
export interface CheckoutResponse {
  checkoutId: string;
  orders: OrderResponse[];
  grandTotal: number;
}// what the SignalR hub sends (C#: OrderStatusChangedEvent)
export interface OrderStatusChangedEvent {
  orderId: number;
  tenantId: number;
  userId: string;
  oldStatus: string;            // "None" = a brand-new order
  newStatus: OrderStatus;
  note: string | null;
  changedAt: string;
}// GET /api/admin/dashboard (C#: DashboardResponse)
export interface DashboardResponse {
  ordersByStatus: Record<OrderStatus, number>;
  revenue14Days: number;
  orders14Days: number;
  averageOrderValue: number;
  cancellationRate: number;                     // 0.048 = 4.8 %
  revenueByDay: { date: string; revenue: number }[];
  lowStock: { id: number; name: string; stockQuantity: number }[];
}
// the admin's view of a product (C#: ProductResponse), with internal data like SKU and exact stock
export interface ProductResponse {
  id: number;
  tenantId: number;
  name: string;
  sku: string;
  price: number;
  stockQuantity: number;
  categoryId: number;
  description: string | null;
  imageUrl: string | null;
  isActive: boolean;
}