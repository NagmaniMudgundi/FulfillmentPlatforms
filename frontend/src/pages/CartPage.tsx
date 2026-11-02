import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Minus, Plus, ShoppingBag, Trash2 } from "lucide-react";
import { api } from "@/lib/api";
import { formatPrice } from "@/lib/format";
import { categoryIcon } from "@/lib/categories";
import { useCart } from "@/cart/CartContext";
import { useAuth } from "@/auth/AuthContext";
import type { CheckoutResponse } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export function CartPage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const { items, groups, count, total, setQuantity, remove, clear } = useCart();
  const { user, isCustomer } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [address, setAddress] = useState({ street: "", postalCode: "", city: "" });

  // POST /api/orders/checkout: React Query for CHANGING data
  const checkout = useMutation({
    mutationFn: () =>
      api<CheckoutResponse>("/api/orders/checkout", {
        method: "POST",
        body: JSON.stringify({
          items: items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
          ...address,
        }),
      }),
    onSuccess: (receipt) => {
      clear();                                                       // empty the cart
      queryClient.invalidateQueries({ queryKey: ["catalog"] });      // stock has changed
      queryClient.invalidateQueries({ queryKey: ["product"] });
      queryClient.invalidateQueries({ queryKey: ["orders"] });       // new orders exist
      navigate("/checkout/success", { state: receipt });             // show the receipt
    },
  });

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!user) {
      navigate("/login", { state: { from: "/cart" } });              // log in first, then come back
      return;
    }
    checkout.mutate();
  }

  // empty cart
  if (count === 0) {
    return (
      <div className="mx-auto max-w-lg rounded-3xl border border-dashed border-border p-10 text-center md:p-16">
        <ShoppingBag className="mx-auto h-12 w-12 text-muted-foreground" />
        <h1 className="mt-4 text-2xl font-extrabold">{t("cart.empty")}</h1>
        <Button asChild className="mt-6"><Link to="/">{t("cart.continue")}</Link></Button>
      </div>
    );
  }

  const vat = total - total / 1.19;                                  // the 19 % MwSt included in the prices

  return (
    <form onSubmit={handleSubmit} className="grid grid-cols-1 gap-8 lg:grid-cols-[1fr_400px]">
      {/* LEFT: one card per shop = one future order */}
      <section className="min-w-0 space-y-5">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight">{t("cart.title")}</h1>
          <p className="text-muted-foreground">{t("cart.summary", { count, shops: groups.length })}</p>
        </div>

        {groups.map((group, index) => {
          const color = group.shopBrandColor ?? "#4338CA";
          return (
            <div key={group.shopSlug} className="overflow-hidden rounded-2xl border border-border bg-card">
              <div className="flex flex-wrap items-center gap-3 border-b border-border px-4 py-3 sm:px-5" style={{ background: `${color}12` }}>
                <span className="flex h-8 w-8 items-center justify-center rounded-lg font-extrabold text-white" style={{ background: color }}>
                  {group.shopName[0]}
                </span>
                <span className="font-bold">{t("cart.soldBy", { shop: group.shopName })}</span>
                <span className="ml-auto rounded-full border px-2.5 py-0.5 text-xs font-bold" style={{ borderColor: color, color }}>
                  {t("cart.becomesOrder", { number: index + 1 })}
                </span>
              </div>

              {group.items.map((item) => {
                const Icon = categoryIcon(item.categorySlug);
                return (
                  <div key={item.productId} className="flex items-center gap-3 border-b border-border px-4 py-4 last:border-b-0 sm:gap-4 sm:px-5">
                    <span className="hidden h-16 w-16 shrink-0 items-center justify-center rounded-xl sm:flex" style={{ background: `${color}1A`, color }}>
                      <Icon className="h-7 w-7" strokeWidth={1.5} />
                    </span>
                    <div className="min-w-0 flex-1">
                      <Link to={`/product/${item.productId}`} className="font-semibold hover:text-primary">{item.name}</Link>
                      <p className="text-sm text-muted-foreground">{formatPrice(item.price, lang)}</p>
                    </div>
                    <div className="flex shrink-0 items-center rounded-lg border border-border">
                      <button type="button" aria-label="−" className="flex h-9 w-8 items-center justify-center sm:w-9"
                        onClick={() => setQuantity(item.productId, item.quantity - 1)}>
                        <Minus className="h-3.5 w-3.5" />
                      </button>
                      <span className="w-6 text-center text-sm font-bold sm:w-7">{item.quantity}</span>
                      <button type="button" aria-label="+" className="flex h-9 w-8 items-center justify-center sm:w-9"
                        onClick={() => setQuantity(item.productId, item.quantity + 1)}>
                        <Plus className="h-3.5 w-3.5" />
                      </button>
                    </div>
                    <span className="w-20 shrink-0 text-right font-bold sm:w-24">{formatPrice(item.price * item.quantity, lang)}</span>
                    <button type="button" aria-label={t("cart.remove")} onClick={() => remove(item.productId)}
                      className="shrink-0 text-muted-foreground hover:text-destructive">
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                );
              })}

              <div className="flex justify-between gap-3 px-4 py-3 text-sm text-muted-foreground sm:px-5">
                <span>{t("cart.subtotal", { shop: group.shopName })}</span>
                <span className="font-bold text-foreground">{formatPrice(group.subtotal, lang)}</span>
              </div>
            </div>
          );
        })}
      </section>

      {/* RIGHT: address + summary + order button */}
      <aside className="min-w-0 space-y-5">
        <div className="space-y-4 rounded-2xl border border-border bg-card p-5 md:p-6">
          <h2 className="text-lg font-extrabold">{t("cart.address")}</h2>
          <Input required placeholder={t("cart.street")} value={address.street}
            onChange={(e) => setAddress({ ...address, street: e.target.value })} />
          <div className="grid grid-cols-[110px_1fr] gap-3">
            <Input required placeholder={t("cart.postalCode")} inputMode="numeric" pattern="\d{5}" maxLength={5}
              title={t("cart.postalCodeHint")} value={address.postalCode}
              onChange={(e) => setAddress({ ...address, postalCode: e.target.value })} />
            <Input required placeholder={t("cart.city")} value={address.city}
              onChange={(e) => setAddress({ ...address, city: e.target.value })} />
          </div>
        </div>

        <div className="space-y-3 rounded-2xl border border-border bg-card p-5 md:p-6">
          <h2 className="text-lg font-extrabold">{t("cart.overview")}</h2>
          <div className="flex justify-between text-sm text-muted-foreground">
            <span>{t("cart.items", { count })}</span><span>{formatPrice(total, lang)}</span>
          </div>
          <div className="flex justify-between text-sm text-muted-foreground">
            <span>{t("cart.vat")}</span><span>{formatPrice(vat, lang)}</span>
          </div>
          <div className="flex justify-between border-t border-border pt-3 text-xl font-extrabold">
            <span>{t("cart.total")}</span><span>{formatPrice(total, lang)}</span>
          </div>
          <p className="rounded-lg bg-accent px-3 py-2 text-sm font-semibold text-accent-foreground">💳 {t("shop.invoice")}</p>

          {user && !isCustomer && (
            <p className="text-sm text-destructive">{t("cart.adminCantBuy")}</p>
          )}
          {checkout.error && (
            <p role="alert" className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">{checkout.error.message}</p>
          )}

          <Button type="submit" className="h-12 w-full text-base" disabled={checkout.isPending || (!!user && !isCustomer)}>
            {checkout.isPending
              ? t("cart.placing")
              : user
                ? t("cart.placeOrders", { count: groups.length, total: formatPrice(total, lang) })
                : t("cart.loginToOrder")}
          </Button>
        </div>

        {/* what happens next (your backend, explained to the customer) */}
        <div className="space-y-3 rounded-2xl bg-slate-900 p-5 text-sm text-slate-200 md:p-6 dark:bg-card">
          <h3 className="font-bold text-white">{t("cart.nextTitle")}</h3>
          {[t("cart.next1"), t("cart.next2"), t("cart.next3")].map((line, i) => (
            <p key={i} className="flex gap-3">
              <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-md bg-indigo-500/30 text-xs font-bold text-indigo-200">{i + 1}</span>
              {line}
            </p>
          ))}
        </div>
      </aside>
    </form>
  );
}