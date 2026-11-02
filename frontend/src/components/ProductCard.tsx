import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { formatPrice } from "@/lib/format";
import { CategoryIcon } from "@/lib/categories";
import type { CatalogProductResponse } from "@/types/api";

export function ProductCard({ product }: { product: CatalogProductResponse }) {
  const { t, i18n } = useTranslation();
  const color = product.shopBrandColor ?? "#4338CA";

  return (
    <Link to={`/product/${product.id}`} className="group flex flex-col gap-3">
      {/* image area: a light tint of the shop colour */}
      <div
        className="relative flex aspect-[4/3] items-center justify-center overflow-hidden rounded-2xl"
        style={{ background: `${color}1A`, color }}
      >
        {product.imageUrl ? (
          <img src={product.imageUrl} alt={product.name} className="h-full w-full object-cover transition group-hover:scale-105" />
        ) : (
          <CategoryIcon slug={product.categorySlug} className="h-16 w-16 transition group-hover:scale-110" strokeWidth={1.25} />
        )}

        {/* "sold by" chip */}
        <span className="absolute bottom-3 left-3 flex items-center gap-1.5 rounded-full bg-card px-2 py-1 text-xs font-bold text-foreground shadow-sm">
          <span className="flex h-5 w-5 items-center justify-center rounded-md text-[10px] text-white" style={{ background: color }}>
            {product.shopName[0]}
          </span>
          {product.shopName}
        </span>

        {!product.inStock && (
          <span className="absolute right-3 top-3 rounded-full bg-foreground px-2 py-1 text-xs font-bold text-background">
            {t("catalog.soldOut")}
          </span>
        )}
      </div>

      <div className="space-y-1 px-1">
        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {t(`categories.${product.categorySlug}`, { defaultValue: product.categoryName })}
        </p>
        <div className="flex items-start justify-between gap-2">
          <p className="font-semibold leading-snug group-hover:text-primary">{product.name}</p>
          <p className="shrink-0 font-bold">{formatPrice(product.price, i18n.language)}</p>
        </div>
        {product.onlyLeft !== null && (
          <p className="text-xs font-semibold text-amber-600">{t("home.onlyLeft", { count: product.onlyLeft })}</p>
        )}
      </div>
    </Link>
  );
}