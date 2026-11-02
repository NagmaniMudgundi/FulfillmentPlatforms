import { useState } from "react";
import { Link } from "react-router";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Check, Package } from "lucide-react";
import { api } from "@/lib/api";
import { formatDateTime, formatPrice } from "@/lib/format";
import { useLiveState } from "@/realtime/RealtimeContext";
import type { OrderResponse, OrderStatus, PagedResult } from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";

const STEPS: OrderStatus[] = ["Pending", "Processing", "Shipped", "Delivered"];
const FILTERS: (OrderStatus | "")[] = ["", "Pending", "Processing", "Shipped", "Delivered", "Cancelled"];

// the small "Live" pill: green = connected
function LivePill() {
  const { t } = useTranslation();
  const live = useLiveState();
  const color = live === "live" ? "bg-emerald-500" : live === "offline" ? "bg-slate-400" : "bg-amber-500";
  return (
    <span className="inline-flex items-center gap-2 rounded-full border border-border bg-card px-3 py-1.5 text-sm font-semibold">
      <span className={`h-2 w-2 rounded-full ${color} ${live === "live" ? "animate-pulse" : ""}`} />
      {t(`live.${live}`)}
    </span>
  );
}

export function OrdersPage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const queryClient = useQueryClient();

  const [status, setStatus] = useState<OrderStatus | "">("");
  const [page, setPage] = useState(1);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  // the key starts with "orders" → the RealtimeProvider can invalidate it
  const orders = useQuery({
    queryKey: ["orders", { status, page }],
    queryFn: () => api<PagedResult<OrderResponse>>(`/api/orders?page=${page}&pageSize=10${status ? `&status=${status}` : ""}`),
    placeholderData: keepPreviousData,
  });

  const cancel = useMutation({
    mutationFn: (id: number) => api<OrderResponse>(`/api/orders/${id}/cancel`, { method: "POST", body: JSON.stringify({}) }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      toast.success(t("orders.cancelled"));
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const list = orders.data?.items ?? [];
  const selected = list.find((o) => o.id === selectedId) ?? list[0];       // default: the newest order

  if (orders.isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-muted" />;
  if (orders.error) return <p className="text-destructive">❌ {orders.error.message}</p>;

  return (
    <div className="space-y-6">
      {/* title + live pill */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight">{t("orders.title")}</h1>
          <p className="text-muted-foreground">{t("orders.subtitle")}</p>
        </div>
        <LivePill />
      </div>

      {/* status filter */}
      <div className="flex flex-wrap gap-2">
        {FILTERS.map((f) => (
          <button
            key={f || "all"}
            type="button"
            onClick={() => { setStatus(f); setPage(1); setSelectedId(null); }}
            className={`h-9 rounded-full border px-4 text-sm font-semibold ${
              status === f ? "border-foreground bg-foreground text-background" : "border-border bg-card text-muted-foreground hover:text-foreground"
            }`}
          >
            {f ? t(`status.${f}`) : t("catalog.all")}
          </button>
        ))}
      </div>

      {list.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-border p-10 text-center md:p-16">
          <Package className="mx-auto h-12 w-12 text-muted-foreground" />
          <p className="mt-4 text-lg font-bold">{t("orders.empty")}</p>
          <Button asChild className="mt-6"><Link to="/">{t("cart.continue")}</Link></Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[380px_1fr]">
          {/* LEFT: the order list */}
          <div className="min-w-0 space-y-3">
            {list.map((o) => (
              <button
                key={o.id}
                type="button"
                onClick={() => setSelectedId(o.id)}
                className={`w-full space-y-2 rounded-2xl border bg-card p-4 text-left transition ${
                  selected?.id === o.id ? "border-primary ring-2 ring-primary/20" : "border-border hover:border-primary/40"
                }`}
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="font-bold">{t("success.order", { id: o.id })}</span>
                  <StatusBadge status={o.status} />
                </div>
                <div className="flex justify-between gap-2 text-sm text-muted-foreground">
                  <span className="min-w-0 truncate">{o.shopName} · {formatDateTime(o.createdAt, lang)}</span>
                  <span className="shrink-0 font-bold text-foreground">{formatPrice(o.totalAmount, lang)}</span>
                </div>
              </button>
            ))}

            {/* pages */}
            {orders.data && orders.data.totalPages > 1 && (
              <div className="flex items-center justify-between gap-2 pt-2">
                <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>{t("catalog.prev")}</Button>
                <span className="text-sm text-muted-foreground">{t("catalog.pageOf", { page, total: orders.data.totalPages })}</span>
                <Button variant="outline" size="sm" disabled={page >= orders.data.totalPages} onClick={() => setPage(page + 1)}>{t("catalog.next")}</Button>
              </div>
            )}
          </div>

          {/* RIGHT: the selected order */}
          {selected && (
            <OrderDetail
              order={selected}
              onCancel={() => { if (confirm(t("orders.confirmCancel"))) cancel.mutate(selected.id); }}
              cancelling={cancel.isPending}
            />
          )}
        </div>
      )}
    </div>
  );
}

// the detail panel: progress, history, items
function OrderDetail({ order, onCancel, cancelling }: { order: OrderResponse; onCancel: () => void; cancelling: boolean }) {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const currentStep = STEPS.indexOf(order.status);
  const timeOf = (s: OrderStatus) => order.statusHistory.find((h) => h.status === s)?.changedAt;

  return (
    <section className="min-w-0 space-y-8 rounded-3xl border border-border bg-card p-5 md:p-8">
      {/* header */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <div className="flex flex-wrap items-center gap-3">
            <h2 className="text-2xl font-extrabold">{t("success.order", { id: order.id })}</h2>
            <StatusBadge status={order.status} />
          </div>
          <p className="text-sm text-muted-foreground">
            {t("orders.placed", { date: formatDateTime(order.createdAt, lang) })} ·{" "}
            <Link to={`/shop/${order.shopSlug}`} className="font-semibold hover:text-primary">{order.shopName}</Link>
          </p>
        </div>
        {order.status === "Pending" && (
          <Button variant="outline" onClick={onCancel} disabled={cancelling} className="border-destructive/40 text-destructive hover:bg-destructive/10">
            {t("orders.cancel")}
          </Button>
        )}
      </div>

      {/* progress: 4 steps, or a red banner when cancelled */}
      {order.status === "Cancelled" ? (
        <p className="rounded-xl bg-rose-50 px-4 py-3 font-semibold text-rose-800 dark:bg-rose-500/10 dark:text-rose-300">
          {t("orders.cancelledBanner")}
        </p>
      ) : (
        <ol className="grid grid-cols-4 gap-2 sm:gap-3">
          {STEPS.map((step, i) => {
            const done = i <= currentStep;
            const time = timeOf(step);
            return (
              <li key={step} className="min-w-0 space-y-2 sm:space-y-3">
                <div className="flex items-center gap-1 sm:gap-2">
                  <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold sm:h-9 sm:w-9 ${
                    done ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground"
                  }`}>
                    {done ? <Check className="h-4 w-4" /> : i + 1}
                  </span>
                  {i < STEPS.length - 1 && <span className={`h-1 flex-1 rounded-full ${i < currentStep ? "bg-primary" : "bg-muted"}`} />}
                </div>
                <p className={`text-xs font-semibold sm:text-sm ${done ? "" : "text-muted-foreground"}`}>{t(`status.${step}`)}</p>
                <p className="hidden text-xs text-muted-foreground sm:block">{time ? formatDateTime(time, lang) : "—"}</p>
              </li>
            );
          })}
        </ol>
      )}

      {/* history timeline: newest first */}
      <div className="space-y-4">
        <h3 className="font-bold">{t("orders.history")}</h3>
        {[...order.statusHistory].reverse().map((h, i) => (
          <div key={i} className="flex gap-4">
            <span className="mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full bg-primary" />
            <div className="min-w-0">
              <p className="text-sm font-semibold">
                {t(`status.${h.status}`)} <span className="font-normal text-muted-foreground">· {formatDateTime(h.changedAt, lang)}</span>
              </p>
              {h.note && <p className="break-words text-sm text-muted-foreground">{h.note}</p>}
            </div>
          </div>
        ))}
      </div>

      {/* items: the table scrolls sideways inside its box on small phones */}
      <div className="overflow-x-auto rounded-2xl border border-border">
        <table className="w-full min-w-[420px] text-sm">
          <thead className="bg-muted text-left text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">{t("orders.product")}</th>
              <th className="px-4 py-3 font-semibold">{t("orders.qty")}</th>
              <th className="px-4 py-3 font-semibold">{t("orders.unitPrice")}</th>
              <th className="px-4 py-3 text-right font-semibold">{t("orders.lineTotal")}</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map((item) => (
              <tr key={item.productId} className="border-t border-border">
                <td className="px-4 py-3">
                  <Link to={`/product/${item.productId}`} className="hover:text-primary">{item.productName}</Link>
                </td>
                <td className="px-4 py-3">{item.quantity}</td>
                <td className="px-4 py-3">{formatPrice(item.unitPrice, lang)}</td>
                <td className="px-4 py-3 text-right">{formatPrice(item.unitPrice * item.quantity, lang)}</td>
              </tr>
            ))}
            <tr className="border-t border-border font-extrabold">
              <td className="px-4 py-3" colSpan={3}>{t("cart.total")}</td>
              <td className="px-4 py-3 text-right">{formatPrice(order.totalAmount, lang)}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  );
}