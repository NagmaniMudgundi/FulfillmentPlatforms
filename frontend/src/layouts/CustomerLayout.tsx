import { useState } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router";
import { Menu, Package, Search, ShoppingCart, X } from "lucide-react";
import { useTranslation } from "react-i18next";
import { useAuth } from "@/auth/AuthContext";
import { useCart } from "@/cart/CartContext";
import { Button } from "@/components/ui/button";
import { ThemeToggle } from "@/components/ThemeToggle";
import { LanguageSwitch } from "@/components/LanguageSwitch";

// one place for the nav link style: indigo when active, grey otherwise
const navClass = ({ isActive }: { isActive: boolean }) =>
  isActive ? "text-primary" : "text-muted-foreground hover:text-foreground";

// The frame of every customer page: top bar + the current page below it
export function CustomerLayout() {
  const { t } = useTranslation();
  const { user, isAdmin, logout } = useAuth();
  const { count } = useCart();
  const navigate = useNavigate();
  const [searchText, setSearchText] = useState("");
  const [menuOpen, setMenuOpen] = useState(false);          // the phone menu: open or closed

  function search(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const q = searchText.trim();
    navigate(q ? `/?search=${encodeURIComponent(q)}` : "/");
    setMenuOpen(false);
  }

  const closeMenu = () => setMenuOpen(false);

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-20 border-b border-border bg-card/90 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-[1600px] items-center gap-3 px-4 md:gap-4 md:px-8">
          {/* logo → home (the name only from "sm" upwards) */}
          <Link to="/" className="flex items-center gap-2" onClick={closeMenu}>
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <Package className="h-5 w-5" />
            </span>
            <span className="hidden text-lg font-extrabold sm:inline">FulfillmentPlatform</span>
          </Link>

          {/* main navigation: laptop only */}
          <nav className="ml-4 hidden gap-5 text-sm font-medium md:flex">
            <NavLink to="/" end className={navClass}>{t("nav.shop")}</NavLink>
            <NavLink to="/orders" className={navClass}>{t("nav.orders")}</NavLink>
          </nav>

          {/* search: large screens only (on phones it's in the menu) */}
          <form onSubmit={search} className="relative ml-auto hidden w-72 lg:block">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
            <input
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              placeholder={t("nav.search")}
              className="h-9 w-full rounded-lg border border-border bg-muted pl-9 pr-3 text-sm outline-none focus:border-primary"
            />
          </form>

          {/* language + theme: laptop only */}
          <div className="ml-auto hidden items-center gap-2 md:flex lg:ml-0">
            <LanguageSwitch />
            <ThemeToggle />
          </div>

          {/* cart: always visible */}
          <Link
            to="/cart"
            aria-label={t("nav.cart")}
            onClick={closeMenu}
            className="relative ml-auto flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground md:ml-0"
          >
            <ShoppingCart className="h-4 w-4" />
            {count > 0 && (
              <span className="absolute -right-1.5 -top-1.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-amber-400 px-1 text-[11px] font-extrabold text-slate-900">
                {count}
              </span>
            )}
          </Link>

          {/* user area: laptop only */}
          <div className="hidden items-center gap-2 md:flex">
            {user ? (
              <>
                {isAdmin && (
                  <Button asChild variant="outline" size="sm"><Link to="/admin">{t("nav.admin")}</Link></Button>
                )}
                <span className="hidden text-sm font-semibold xl:block">{t("nav.hello", { name: user.firstName })}</span>
                <Button variant="ghost" size="sm" onClick={logout}>{t("nav.logout")}</Button>
              </>
            ) : (
              <Button asChild variant="outline" size="sm"><Link to="/login">{t("nav.signIn")}</Link></Button>
            )}
          </div>

          {/* ☰ menu button: phone only */}
          <button
            type="button"
            aria-label="Menu"
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen(!menuOpen)}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-border md:hidden"
          >
            {menuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>

        {/* the phone menu panel */}
        {menuOpen && (
          <div className="space-y-4 border-t border-border bg-card px-4 py-4 md:hidden">
            <form onSubmit={search} className="relative">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
              <input
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                placeholder={t("nav.search")}
                className="h-9 w-full rounded-lg border border-border bg-muted pl-9 pr-3 text-sm outline-none focus:border-primary"
              />
            </form>

            <nav className="flex flex-col gap-1 text-base font-semibold">
              <NavLink to="/" end onClick={closeMenu} className={({ isActive }) => `rounded-lg px-3 py-2 ${isActive ? "bg-accent text-primary" : ""}`}>
                {t("nav.shop")}
              </NavLink>
              <NavLink to="/orders" onClick={closeMenu} className={({ isActive }) => `rounded-lg px-3 py-2 ${isActive ? "bg-accent text-primary" : ""}`}>
                {t("nav.orders")}
              </NavLink>
              {isAdmin && (
                <NavLink to="/admin" onClick={closeMenu} className="rounded-lg px-3 py-2">{t("nav.admin")}</NavLink>
              )}
            </nav>

            <div className="flex items-center justify-between border-t border-border pt-4">
              <div className="flex gap-2">
                <LanguageSwitch />
                <ThemeToggle />
              </div>
              {user ? (
                <Button variant="outline" size="sm" onClick={() => { logout(); closeMenu(); }}>{t("nav.logout")}</Button>
              ) : (
                <Button asChild size="sm"><Link to="/login" onClick={closeMenu}>{t("nav.signIn")}</Link></Button>
              )}
            </div>
          </div>
        )}
      </header>

      {/* the current page appears HERE (less side space on phones) */}
      <main className="mx-auto max-w-[1600px] px-4 py-6 md:px-8 md:py-8">
        <Outlet />
      </main>
    </div>
  );
}