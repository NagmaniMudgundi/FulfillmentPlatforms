import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import { AuthProvider } from "@/auth/AuthContext";
import { CartProvider } from "@/cart/CartContext";
import { RealtimeProvider } from "@/realtime/RealtimeContext";
import App from "./App";
import "./index.css";
import "./i18n";

// one cache for all API data in the app
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <RealtimeProvider>                                {/* ⬅️ NEW: live connection (needs Auth + Query) */}
            <CartProvider>
              <App />
              <Toaster richColors position="top-right" />
            </CartProvider>
          </RealtimeProvider>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>
);