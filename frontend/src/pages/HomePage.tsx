import { Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { api } from "@/lib/api";
import type { ShopResponse } from "@/types/api";
import { CatalogBrowser } from "@/components/CatalogBrowser";

export function HomePage() {
  const { t } = useTranslation();

  const shops = useQuery({
    queryKey: ["shops"],
    queryFn: () => api<ShopResponse[]>("/api/shops"),
  });

  // show the shops that have a full profile (brand colour) in the hero
  const featured = shops.data?.filter((s) => s.brandColor).slice(0, 3) ?? [];

  return (
    <div className="space-y-10 md:space-y-12">
      {/* hero banner */}
      <section
        className="relative overflow-hidden rounded-3xl p-6 text-white md:p-10 lg:p-14"
        style={{ background: "linear-gradient(115deg, #1E1B4B 0%, #312E81 50%, #4338CA 100%)" }}
      >
        <div className="relative z-10 grid grid-cols-1 gap-8 lg:grid-cols-2 lg:items-center lg:gap-10">
          <div className="space-y-5">
            <span className="inline-block rounded-full bg-white/15 px-3 py-1 text-xs font-bold uppercase tracking-wider">
              {t("home.badge")}
            </span>
            <h1 className="text-3xl font-extrabold leading-tight tracking-tight md:text-4xl lg:text-5xl">{t("home.heroTitle")}</h1>
            <p className="max-w-xl text-base text-indigo-100 md:text-lg">{t("home.heroText")}</p>
          </div>

          {/* featured shops */}
          <div className="space-y-3">
            {featured.map((shop) => (
              <Link
                key={shop.id}
                to={`/shop/${shop.slug}`}
                className="flex items-center gap-4 rounded-2xl border border-white/15 bg-white/10 p-4 transition hover:bg-white/20"
              >
                <span
                  className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl text-lg font-extrabold"
                  style={{ background: shop.brandColor ?? "#4338CA" }}
                >
                  {shop.name[0]}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="font-bold">{shop.name}</p>
                  <p className="text-sm text-indigo-200">{[shop.description, shop.city].filter(Boolean).join(" · ")}</p>
                </div>
                <span className="hidden shrink-0 text-sm font-semibold text-indigo-100 sm:inline">{t("home.visitShop")} →</span>
              </Link>
            ))}
          </div>
        </div>
        <div className="pointer-events-none absolute -bottom-40 -left-20 h-96 w-96 rounded-full bg-white/5" />
      </section>

      {/* the catalog: all shops */}
      <CatalogBrowser />
    </div>
  );
}