import { useState } from "react";
import { Link, useParams } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { FileText, Minus, Plus, ShoppingCart, Truck } from "lucide-react";
import { api } from "@/lib/api";
import { formatDeliveryDate, formatPrice } from "@/lib/format";
import { categoryIcon } from "@/lib/categories";
import { useCart } from "@/cart/CartContext";
import type { CatalogProductResponse, PagedResult } from "@/types/api";
import { ProductCard } from "@/components/ProductCard";
import { Button } from "@/components/ui/button";

export function ProductPage() {
  const { t, i18n } = useTranslation();
  const { id = "" } = useParams();                          // /product/:id
  const { add } = useCart();
  const [quantity, setQuantity] = useState(1);

  const product = useQuery({
    queryKey: ["product", id],
    queryFn: () => api<CatalogProductResponse>(`/api/catalog/products/${id}`),
  });

  // "more from this shop": only loads after the product has arrived
  const more = useQuery({
    queryKey: ["catalog", "more", product.data?.shopSlug],
    queryFn: () => api<PagedResult<CatalogProductResponse>>(`/api/catalog/products?shop=${product.data!.shopSlug}&pageSize=5`),
    enabled: !!product.data,
  });

  if (product.isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-muted" />;

  if (product.error || !product.data) {
    return (
      <div className="rounded-2xl border border-dashed border-border p-10 text-center md:p-16">
        <h1 className="text-2xl font-extrabold">{t("product.notFound")}</h1>
        <Link to="/" className="mt-4 inline-block font-semibold text-primary">← {t("shop.backHome")}</Link>
      </div>
    );
  }

  const p = product.data;
  const color = p.shopBrandColor ?? "#4338CA";
  const Icon = categoryIcon(p.categorySlug);
  const maxQuantity = p.onlyLeft ?? 100;
  const lang = i18n.language;

  function handleAdd() {
    add(p, quantity);
    toast.success(t("product.added"), { description: `${quantity} × ${p.name}` });
    setQuantity(1);
  }

  return (
    <div className="space-y-10 md:space-y-14">
      {/* breadcrumb */}
      <nav className="flex flex-wrap gap-2 text-sm text-muted-foreground">
        <Link to="/" className="hover:text-foreground">{t("nav.shop")}</Link>
        <span>/</span>
        <Link to={`/shop/${p.shopSlug}`} className="hover:text-foreground">{p.shopName}</Link>
        <span>/</span>
        <span className="font-semibold text-foreground">{p.name}</span>
      </nav>

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-2 lg:gap-12">
        {/* big image area */}
        <div
          className="flex aspect-square items-center justify-center rounded-3xl"
          style={{ background: `radial-gradient(circle at 30% 20%, ${color}14, ${color}33)`, color }}
        >
          {p.imageUrl ? (
            <img src={p.imageUrl} alt={p.name} className="h-full w-full rounded-3xl object-cover" />
          ) : (
            <Icon className="h-32 w-32 md:h-48 md:w-48" strokeWidth={0.9} />
          )}
        </div>

        {/* information + buy box */}
        <div className="min-w-0 space-y-7">
          <div className="space-y-3">
            <Link to={`/shop/${p.shopSlug}`} className="text-sm font-bold uppercase tracking-wider" style={{ color }}>
              {p.shopName} · {t(`categories.${p.categorySlug}`, { defaultValue: p.categoryName })}
            </Link>
            <h1 className="text-3xl font-extrabold tracking-tight md:text-4xl">{p.name}</h1>
            <div className="flex flex-wrap items-baseline gap-3">
              <span className="text-3xl font-extrabold">{formatPrice(p.price, lang)}</span>
              <span className="text-sm text-muted-foreground">{t("product.vat")}</span>
            </div>
          </div>

          {/* stock */}
          {!p.inStock ? (
            <p className="rounded-xl bg-muted px-4 py-3 font-semibold">{t("catalog.soldOut")}</p>
          ) : p.onlyLeft !== null ? (
            <p className="rounded-xl border border-amber-300 bg-amber-50 px-4 py-3 font-semibold text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-300">
              {t("home.onlyLeft", { count: p.onlyLeft })}
            </p>
          ) : (
            <p className="font-semibold text-emerald-600">● {t("product.inStock")}</p>
          )}

          {/* quantity + add to cart (stacked on very small phones) */}
          <div className="flex flex-col gap-3 sm:flex-row">
            <div className="flex items-center justify-between rounded-xl border border-border bg-card sm:justify-start">
              <button type="button" aria-label="−" className="flex h-12 w-12 items-center justify-center"
                onClick={() => setQuantity((q) => Math.max(1, q - 1))}>
                <Minus className="h-4 w-4" />
              </button>
              <span className="w-8 text-center font-bold">{quantity}</span>
              <button type="button" aria-label="+" className="flex h-12 w-12 items-center justify-center"
                onClick={() => setQuantity((q) => Math.min(maxQuantity, q + 1))}>
                <Plus className="h-4 w-4" />
              </button>
            </div>
            <Button className="h-12 flex-1 text-base" disabled={!p.inStock} onClick={handleAdd}>
              <ShoppingCart className="h-5 w-5" />
              {t("product.addToCart")} · {formatPrice(p.price * quantity, lang)}
            </Button>
          </div>

          {/* delivery + payment */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="flex gap-3 rounded-xl border border-border bg-card p-4">
              <Truck className="h-5 w-5 shrink-0 text-primary" />
              <div>
                <p className="text-xs text-muted-foreground">{t("product.delivery")}</p>
                <p className="font-bold">{formatDeliveryDate(lang)}</p>
              </div>
            </div>
            <div className="flex gap-3 rounded-xl border border-border bg-card p-4">
              <FileText className="h-5 w-5 shrink-0 text-primary" />
              <div>
                <p className="text-xs text-muted-foreground">{t("product.payment")}</p>
                <p className="font-bold">{t("shop.invoice")}</p>
              </div>
            </div>
          </div>

          {p.description && (
            <div className="space-y-2 border-t border-border pt-6">
              <h2 className="font-bold">{t("product.description")}</h2>
              <p className="leading-relaxed text-muted-foreground">{p.description}</p>
            </div>
          )}
        </div>
      </div>

      {/* more from this shop */}
      {more.data && more.data.items.length > 1 && (
        <section className="space-y-5">
          <h2 className="text-2xl font-extrabold">{t("product.moreFrom", { shop: p.shopName })}</h2>
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {more.data.items.filter((x) => x.id !== p.id).slice(0, 4).map((x) => (
              <ProductCard key={x.id} product={x} />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}