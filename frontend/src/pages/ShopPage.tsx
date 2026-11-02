import { Link, useParams } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { ArrowLeft, FileText, RotateCcw, Truck, Zap } from "lucide-react";
import { api } from "@/lib/api";
import type { ShopResponse } from "@/types/api";
import { CatalogBrowser } from "@/components/CatalogBrowser";

export function ShopPage() {
  const { t } = useTranslation();
  const { slug = "" } = useParams();                // /shop/:slug → "modehaus"

  const shop = useQuery({
    queryKey: ["shop", slug],
    queryFn: () => api<ShopResponse>(`/api/shops/${slug}`),
  });

  if (shop.isLoading) return <div className="h-72 animate-pulse rounded-3xl bg-muted" />;

  if (shop.error || !shop.data) {
    return (
      <div className="rounded-2xl border border-dashed border-border p-10 text-center md:p-16">
        <h1 className="text-2xl font-extrabold">{t("shop.notFound")}</h1>
        <Link to="/" className="mt-4 inline-block font-semibold text-primary">← {t("shop.backHome")}</Link>
      </div>
    );
  }

  const s = shop.data;
  const color = s.brandColor ?? "#4338CA";

  const promises = [
    { icon: Truck, title: t("shop.delivery"), sub: t("shop.deliverySub") },
    { icon: RotateCcw, title: t("shop.returns"), sub: t("shop.returnsSub") },
    { icon: FileText, title: t("shop.invoice"), sub: t("shop.invoiceSub") },
    { icon: Zap, title: t("shop.live"), sub: t("shop.liveSub") },
  ];

  return (
    <div className="space-y-10">
      {/* branded hero in the shop's colour */}
      <section
        className="relative overflow-hidden rounded-3xl p-6 text-white md:p-10 lg:p-14"
        style={{ background: `linear-gradient(120deg, #0F172A 0%, ${color} 70%)` }}
      >
        <Link to="/" className="mb-6 inline-flex items-center gap-2 text-sm font-semibold text-white/80 hover:text-white md:mb-8">
          <ArrowLeft className="h-4 w-4" /> {t("shop.allShops")}
        </Link>
        <div className="flex items-center gap-4 md:gap-5">
          <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-white text-2xl font-extrabold md:h-16 md:w-16 md:text-3xl" style={{ color }}>
            {s.name[0]}
          </span>
          <div className="min-w-0">
            <h1 className="text-3xl font-extrabold tracking-tight md:text-4xl lg:text-5xl">{s.name}</h1>
            <p className="mt-1 text-base text-white/80 md:text-lg">{[s.description, s.city].filter(Boolean).join(" · ")}</p>
          </div>
        </div>
        <div className="pointer-events-none absolute -right-24 -top-24 h-80 w-80 rounded-full bg-white/10" />
      </section>

      {/* shop promises */}
      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {promises.map((p) => (
          <div key={p.title} className="flex items-center gap-4 rounded-2xl border border-border bg-card p-4">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl" style={{ background: `${color}1A`, color }}>
              <p.icon className="h-5 w-5" />
            </span>
            <div>
              <p className="text-sm font-bold">{p.title}</p>
              <p className="text-xs text-muted-foreground">{p.sub}</p>
            </div>
          </div>
        ))}
      </section>

      {/* the catalog: only this shop */}
      <CatalogBrowser shop={slug} />
    </div>
  );
}