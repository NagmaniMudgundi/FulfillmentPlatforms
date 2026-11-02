import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { api, setAuthToken, setUnauthorizedHandler } from "@/lib/api";
import type { LoginResponse } from "@/types/api";

const STORAGE_KEY = "auth";

export type AuthUser = LoginResponse;

interface AuthContextValue {
  user: AuthUser | null;
  isAdmin: boolean;
  isCustomer: boolean;
  login: (email: string, password: string) => Promise<AuthUser>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// read the saved login; ignore it if the token has expired
function loadSavedUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const user = JSON.parse(raw) as AuthUser;
    if (new Date(user.expiresAt) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return user;
  } catch {
    return null;
  }
}

// Provides "who is logged in" to the whole app
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();

  const [user, setUser] = useState<AuthUser | null>(() => {
    const saved = loadSavedUser();
    setAuthToken(saved?.token ?? null);          // api.ts sends the token from the first request on
    return saved;
  });

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setAuthToken(null);
    setUser(null);
    queryClient.clear();                         // forget the previous user's cached data
  }, [queryClient]);

  const login = useCallback(async (email: string, password: string) => {
    const result = await api<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    });
    queryClient.clear();
    localStorage.setItem(STORAGE_KEY, JSON.stringify(result));
    setAuthToken(result.token);
    setUser(result);
    return result;
  }, [queryClient]);

  // the API rejected our token → log out
  useEffect(() => {
    setUnauthorizedHandler(logout);
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  // log out automatically when the token expires
  useEffect(() => {
    if (!user) return;
    const msLeft = new Date(user.expiresAt).getTime() - Date.now();
    const timer = setTimeout(logout, msLeft);
    return () => clearTimeout(timer);            // cleanup: cancel the old timer
  }, [user, logout]);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    login,
    logout,
    isAdmin: user?.roles.includes("TenantAdmin") ?? false,
    isCustomer: user?.roles.includes("Customer") ?? false,
  }), [user, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// use it in any component: const { user, login, logout } = useAuth();
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}