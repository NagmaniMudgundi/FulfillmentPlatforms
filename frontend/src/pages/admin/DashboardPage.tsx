import { Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { AlertTriangle, Euro, Percent, ShoppingBag, TrendingUp } from "lucide-react";
import { api } from "@/lib/api";
import { formatDateTime, formatPrice } from "@/lib/format";
import { useAuth } from "@/auth/AuthContext";
import { useLiveState } from "@/realtime/RealtimeContext";
import type { DashboardResponse, OrderResponse, OrderStatus, PagedResult } from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";

const STATUS_ORDER: OrderStatus[] = ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"];
const STATUS_COLORS: Record<OrderStatus, string> = {
  Pending: "#F59E0B", Processing: "#3B82F6", Shipped: "#8B5CF6", Delivered: "#10B981", Cancelled: "#F43F5E",
};

export function DashboardPage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const { user } = useAuth();
  const live = useLiveState();

  // the key "dashboard" → the RealtimeProvider refreshes it on every event
  const dashboard = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => api<DashboardResponse>("/api/admin/dashboard"),
  });

  const latest = useQuery({
    queryKey: ["orders", { latest: true }],
    queryFn: () => api<PagedResult<OrderResponse>>("/api/orders?pageSize=6"),
  });

  if (dashboard.isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-muted" />;
  if (dashboard.error || !dashboard.data) return <p className="text-destructive">❌ {dashboard.error?.message}</p>;

  const d = dashboard.data;

  // greeting by time of day: "Guten Morgen, Max"
  const hour = new Date().getHours();
  const greeting = hour < 12 ? t("dashboard.morning") : hour < 18 ? t("dashboard.day") : t("dashboard.evening");

  // chart data: "2026-10-28T00:00:00Z" → "28.10."
  const chartData = d.revenueByDay.map((r) => ({
    day: new Date(r.date).toLocaleDateString(lang === "de" ? "de-DE" : "en-GB", { day: "2-digit", month: "2-digit" }),
    revenue: r.revenue,
  }));

  const totalOrders = STATUS_ORDER.reduce((sum, s) => sum + (d.ordersByStatus[s] ?? 0), 0);

  const kpis = [
    { icon: Euro, label: t("dashboard.revenue"), value: formatPrice(d.revenue14Days, lang) },
    { icon: ShoppingBag, label: t("dashboard.orders"), value: d.orders14Days.toString() },
    { icon: TrendingUp, label: t("dashboard.avgOrder"), value: formatPrice(d.averageOrderValue, lang) },
    { icon: Percent, label: t("dashboard.cancelRate"), value: `${(d.cancellationRate * 100).toFixed(1)} %` },
  ];

  return (
    <div className="space-y-6">
      {/* header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-muted-foreground">{greeting}, {user?.firstName} 👋</p>
          <h1 className="text-3xl font-extrabold tracking-tight">{t("dashboard.title")}</h1>
        </div>
        <span className="inline-flex items-center gap-2 rounded-full border border-border bg-card px-3 py-1.5 text-sm font-semibold">
          <span className={`h-2 w-2 rounded-full ${live === "live" ? "animate-pulse bg-emerald-500" : "bg-slate-400"}`} />
          {t(`live.${live}`)}
        </span>
      </div>

      {/* KPI cards */}
      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {kpis.map((k) => (
          <div key={k.label} className="space-y-3 rounded-2xl border border-border bg-card p-5">
            <div className="flex items-center justify-between text-sm font-medium text-muted-foreground">
              {k.label}
              <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-accent text-accent-foreground">
                <k.icon className="h-4 w-4" />
              </span>
            </div>
            <p className="text-3xl font-extrabold tracking-tight">{k.value}</p>
            <p className="text-xs text-muted-foreground">{t("dashboard.last14")}</p>
          </div>
        ))}
      </section>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        {/* revenue chart */}
        <section className="min-w-0 space-y-4 rounded-2xl border border-border bg-card p-5 md:p-6 xl:col-span-2">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <h2 className="text-lg font-bold">{t("dashboard.revenueChart")}</h2>
            <span className="text-2xl font-extrabold">{formatPrice(d.revenue14Days, lang)}</span>
          </div>
          <div className="h-64 md:h-72">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#94A3B833" />
                <XAxis dataKey="day" tick={{ fill: "#94A3B8", fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fill: "#94A3B8", fontSize: 11 }} axisLine={false} tickLine={false} width={44} />
                <Tooltip
                  cursor={{ fill: "#6366F11A" }}
                  formatter={(value) => [formatPrice(Number(value), lang), t("dashboard.revenueLabel")]}
                />
                <Bar dataKey="revenue" radius={[6, 6, 0, 0]}>
                  {/* today = darker */}
                  {chartData.map((_, i) => (
                    <Cell key={i} fill={i === chartData.length - 1 ? "#4338CA" : "#A5B4FC"} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </section>

        {/* orders by status */}
        <section className="min-w-0 space-y-5 rounded-2xl border border-border bg-card p-5 md:p-6">
          <h2 className="text-lg font-bold">{t("dashboard.byStatus")}</h2>
          {/* one stacked bar: each status takes its share */}
          <div className="flex h-3 overflow-hidden rounded-full bg-muted">
            {STATUS_ORDER.map((s) => (
              <span key={s} style={{ width: `${totalOrders ? (d.ordersByStatus[s] / totalOrders) * 100 : 0}%`, background: STATUS_COLORS[s] }} />
            ))}
          </div>
          <ul className="space-y-3">
            {STATUS_ORDER.map((s) => (
              <li key={s} className="flex items-center gap-3 text-sm">
                <span className="h-2.5 w-2.5 rounded-sm" style={{ background: STATUS_COLORS[s] }} />
                <span className="flex-1">{t(`status.${s}`)}</span>
                <span className="font-bold">{d.ordersByStatus[s] ?? 0}</span>
              </li>
            ))}
          </ul>
          <Link to="/admin/board" className="inline-block text-sm font-semibold text-primary">{t("dashboard.openBoard")} →</Link>
        </section>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        {/* latest orders */}
        <section className="min-w-0 space-y-4 rounded-2xl border border-border bg-card p-5 md:p-6 xl:col-span-2">
          <h2 className="text-lg font-bold">{t("dashboard.latest")}</h2>
          {latest.data?.items.length === 0 && <p className="text-sm text-muted-foreground">{t("dashboard.noOrders")}</p>}
          <ul className="divide-y divide-border">
            {latest.data?.items.map((o) => (
              <li key={o.id} className="flex flex-wrap items-center gap-x-4 gap-y-1 py-3 text-sm">
                <span className="w-16 font-bold">#{o.id}</span>
                <span className="flex-1 text-muted-foreground">{formatDateTime(o.createdAt, lang)}</span>
                <span className="hidden text-muted-foreground sm:inline">{t("cart.items", { count: o.items.reduce((s, i) => s + i.quantity, 0) })}</span>
                <StatusBadge status={o.status} />
                <span className="w-24 text-right font-bold">{formatPrice(o.totalAmount, lang)}</span>
              </li>
            ))}
          </ul>
        </section>

        {/* low stock */}
        <section className="min-w-0 space-y-4 rounded-2xl border border-border bg-card p-5 md:p-6">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-bold">{t("dashboard.lowStock")}</h2>
            {d.lowStock.length > 0 && <AlertTriangle className="h-5 w-5 text-amber-500" />}
          </div>
          {d.lowStock.length === 0 && <p className="text-sm text-muted-foreground">{t("dashboard.stockOk")}</p>}
          <ul className="space-y-4">
            {d.lowStock.map((p) => (
              <li key={p.id} className="space-y-1.5">
                <div className="flex justify-between gap-2 text-sm">
                  <span className="min-w-0 truncate font-semibold">{p.name}</span>
                  <span className={`shrink-0 font-bold ${p.stockQuantity <= 2 ? "text-rose-600" : "text-amber-600"}`}>
                    {t("dashboard.left", { count: p.stockQuantity })}
                  </span>
                </div>
                <div className="h-1.5 overflow-hidden rounded-full bg-muted">
                  <div className={`h-full ${p.stockQuantity <= 2 ? "bg-rose-500" : "bg-amber-500"}`} style={{ width: `${(p.stockQuantity / 5) * 100}%` }} />
                </div>
              </li>
            ))}
          </ul>
          <Link to="/admin/products" className="inline-block text-sm font-semibold text-primary">{t("dashboard.manageProducts")} →</Link>
        </section>
      </div>
    </div>
  );
}