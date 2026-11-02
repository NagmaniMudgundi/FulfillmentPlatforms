import { lazy, Suspense } from "react";
import { Route, Routes } from "react-router";
import { CustomerLayout } from "@/layouts/CustomerLayout";
import { AdminLayout } from "@/layouts/AdminLayout";
import { HomePage } from "@/pages/HomePage";
import { ShopPage } from "@/pages/ShopPage";
import { ProductPage } from "@/pages/ProductPage";
import { CartPage } from "@/pages/CartPage";
import { CheckoutSuccessPage } from "@/pages/CheckoutSuccessPage";
import { OrdersPage } from "@/pages/OrdersPage";
import { LoginPage } from "@/pages/LoginPage";
import { RegisterPage } from "@/pages/RegisterPage";
import { ComingSoon } from "@/components/ComingSoon";
import { RequireRole } from "@/auth/RequireRole";

// admin pages are loaded ONLY when an admin opens them (charts + drag & drop are big libraries)
const DashboardPage = lazy(() => import("@/pages/admin/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const BoardPage = lazy(() => import("@/pages/admin/BoardPage").then((m) => ({ default: m.BoardPage })));
const ProductsPage = lazy(() => import("@/pages/admin/ProductsPage").then((m) => ({ default: m.ProductsPage })));

// The map of the app: which URL shows which page, inside which frame
export default function App() {
  return (
    // while a lazy page is downloading, show a small placeholder
    <Suspense fallback={<div className="h-96 animate-pulse rounded-3xl bg-muted" />}>
      <Routes>
        {/* customer pages: with the top bar */}
        <Route element={<CustomerLayout />}>
          <Route index element={<HomePage />} />
          <Route path="shop/:slug" element={<ShopPage />} />
          <Route path="product/:id" element={<ProductPage />} />
          <Route path="cart" element={<CartPage />} />

          {/* only for logged-in customers */}
          <Route path="checkout/success" element={<RequireRole role="Customer"><CheckoutSuccessPage /></RequireRole>} />
          <Route path="orders" element={<RequireRole role="Customer"><OrdersPage /></RequireRole>} />
        </Route>

        {/* login + register: full screen, no top bar */}
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />

        {/* the whole admin area only for shop admins */}
        <Route path="admin" element={<RequireRole role="TenantAdmin"><AdminLayout /></RequireRole>}>
          <Route index element={<DashboardPage />} />
          <Route path="board" element={<BoardPage />} />
          <Route path="products" element={<ProductsPage />} />
        </Route>

        {/* any other URL */}
        <Route path="*" element={<ComingSoon title="Page not found" />} />
      </Routes>
    </Suspense>
  );
}