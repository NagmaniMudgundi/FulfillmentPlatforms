import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router";
import { useAuth } from "./AuthContext";

// [Authorize(Roles = …)] for pages
export function RequireRole({ role, children }: { role: "Customer" | "TenantAdmin"; children: ReactNode }) {
  const { user } = useAuth();
  const location = useLocation();

  // not logged in → login page, and remember where we wanted to go
  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  // logged in, but the wrong role → to "their" area
  if (!user.roles.includes(role)) {
    return <Navigate to={user.roles.includes("TenantAdmin") ? "/admin" : "/"} replace />;
  }

  return <>{children}</>;
}