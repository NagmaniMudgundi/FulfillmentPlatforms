import { Link, Navigate, useLocation } from "react-router";
import { useTranslation } from "react-i18next";
import { CheckCircle2 } from "lucide-react";
import { formatPrice } from "@/lib/format";
import type { CheckoutResponse } from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";

export function CheckoutSuccessPage() {
  const { t, i18n } = useTranslation();
  const receipt = useLocation().state as CheckoutResponse | null;      // passed by CartPage via navigate(…, { state })

  // opened directly without a checkout → back to the shop
  if (!receipt) return <Navigate to="/" replace />;

  return (
    <div className="mx-auto max-w-2xl space-y-8 py-6 text-center">
      <CheckCircle2 className="mx-auto h-16 w-16 text-emerald-500" />
      <div className="space-y-2">
        <h1 className="text-4xl font-extrabold tracking-tight">{t("success.title")}</h1>
        <p className="text-lg text-muted-foreground">{t("success.text", { count: receipt.orders.length })}</p>
      </div>

      <div className="space-y-3 text-left">
        {receipt.orders.map((order) => (
          <div key={order.id} className="flex items-center gap-4 rounded-2xl border border-border bg-card p-5">
            <div className="flex-1">
              <p className="font-bold">{t("success.order", { id: order.id })} · {order.shopName}</p>
              <p className="text-sm text-muted-foreground">{t("cart.items", { count: order.items.reduce((s, i) => s + i.quantity, 0) })}</p>
            </div>
            <StatusBadge status={order.status} />
            <span className="w-24 text-right font-bold">{formatPrice(order.totalAmount, i18n.language)}</span>
          </div>
        ))}
        <div className="flex justify-between px-5 pt-2 text-xl font-extrabold">
          <span>{t("cart.total")}</span>
          <span>{formatPrice(receipt.grandTotal, i18n.language)}</span>
        </div>
      </div>

      <p className="text-sm text-muted-foreground">📧 {t("success.email")}</p>

      <div className="flex justify-center gap-3">
        <Button asChild><Link to="/orders">{t("success.track")}</Link></Button>
        <Button asChild variant="outline"><Link to="/">{t("cart.continue")}</Link></Button>
      </div>
    </div>
  );
}