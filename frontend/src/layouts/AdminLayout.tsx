import { useState } from "react";
import { NavLink, Outlet } from "react-router";
import { LayoutDashboard, LogOut, Menu, Package, SquareKanban, X } from "lucide-react";
import { useTranslation } from "react-i18next";
import { useAuth } from "@/auth/AuthContext";
import { ThemeToggle } from "@/components/ThemeToggle";
import { LanguageSwitch } from "@/components/LanguageSwitch";

// the sidebar menu: labelKey = the dictionary key
const menu = [
  { to: "/admin", labelKey: "admin.dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin/board", labelKey: "admin.board", icon: SquareKanban, end: false },
  { to: "/admin/products", labelKey: "admin.products", icon: Package, end: false },
];

// The frame of every admin page: dark sidebar + the current page
export function AdminLayout() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const [sidebarOpen, setSidebarOpen] = useState(false);     // phone: is the sidebar open?

  const close = () => setSidebarOpen(false);

  return (
    <div className="flex min-h-screen bg-background">
      {/* phone: a dark layer behind the open sidebar (click = close) */}
      {sidebarOpen && <div className="fixed inset-0 z-30 bg-black/50 lg:hidden" onClick={close} />}

      {/* sidebar: phone = slides in from the left · laptop = always visible */}
      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col gap-8 bg-sidebar p-4 text-sidebar-foreground transition-transform lg:static lg:translate-x-0 ${
          sidebarOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="flex items-center justify-between px-2">
          <span className="text-lg font-extrabold text-white">{t("admin.title")}</span>
          <button type="button" aria-label="Close menu" onClick={close} className="lg:hidden">
            <X className="h-5 w-5" />
          </button>
        </div>

        <nav className="flex flex-col gap-1">
          {menu.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              onClick={close}
              className={({ isActive }) =>
                `flex h-11 items-center gap-3 rounded-lg px-3 text-sm font-medium ${
                  isActive ? "bg-sidebar-accent text-white" : "hover:bg-sidebar-accent/60"
                }`
              }
            >
              <item.icon className="h-4 w-4" />
              {t(item.labelKey)}
            </NavLink>
          ))}
        </nav>

        {/* who is logged in + sign out */}
        <div className="mt-auto space-y-2 rounded-xl bg-sidebar-accent p-3 text-sm">
          <p className="truncate font-semibold text-white">{user?.email}</p>
          <button type="button" onClick={logout} className="flex items-center gap-2 text-sidebar-foreground/80 hover:text-white">
            <LogOut className="h-4 w-4" />
            {t("nav.logout")}
          </button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        {/* top bar: ☰ on phones + language + theme */}
        <div className="flex items-center gap-2 border-b border-border bg-card px-4 py-3 md:px-8">
          <button
            type="button"
            aria-label="Menu"
            onClick={() => setSidebarOpen(true)}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-border lg:hidden"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="ml-auto flex gap-2">
            <LanguageSwitch />
            <ThemeToggle />
          </div>
        </div>
        <main className="flex-1 p-4 md:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}