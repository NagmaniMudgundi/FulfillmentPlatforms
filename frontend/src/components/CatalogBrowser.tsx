import type { ReactNode } from "react";
import { useSearchParams } from "react-router";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";
import { api } from "@/lib/api";
import type { CatalogProductResponse, CategoryResponse, PagedResult } from "@/types/api";
import { ProductCard } from "@/components/ProductCard";
import { Button } from "@/components/ui/button";

const PAGE_SIZE = 12;
const GRID = "grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4 2xl:grid-cols-5";   // ONE place for the grid

// a round filter button
function Chip({ active, onClick, children }: { active: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`h-9 rounded-full border px-4 text-sm font-semibold transition ${
        active ? "border-foreground bg-foreground text-background" : "border-border bg-card text-muted-foreground hover:text-foreground"
      }`}
    >
      {children}
    </button>
  );
}

// The product catalog with filters. "shop" = optional: only this shop's products.
export function CatalogBrowser({ shop }: { shop?: string }) {
  const { t } = useTranslation();
  const [params, setParams] = useSearchParams();

  // the filters live in the URL: ?search=boot&category=shoes&sort=price-asc&page=2
  const search = params.get("search") ?? "";
  const category = params.get("category") ?? "";
  const sort = params.get("sort") ?? "newest";
  const page = Number(params.get("page") ?? "1");

  // change one filter; any filter change goes back to page 1
  function setParam(key: string, value: string) {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    setParams(next);
  }

  const categories = useQuery({
    queryKey: ["categories"],
    queryFn: () => api<CategoryResponse[]>("/api/catalog/categories"),
  });

  // build the API query string from the filters
  const query = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE), sort });
  if (search) query.set("search", search);
  if (category) query.set("category", category);
  if (shop) query.set("shop", shop);

  const products = useQuery({
    queryKey: ["catalog", query.toString()],
    queryFn: () => api<PagedResult<CatalogProductResponse>>(`/api/catalog/products?${query}`),
    placeholderData: keepPreviousData,
  });

  const data = products.data;

  return (
    <section className="space-y-6">
      {/* title + sorting */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h2 className="text-2xl font-extrabold tracking-tight">
          {search ? t("catalog.resultsFor", { search }) : t("catalog.title")}
        </h2>
        <select
          value={sort}
          onChange={(e) => setParam("sort", e.target.value)}
          aria-label={t("catalog.sort")}
          className="h-10 rounded-lg border border-border bg-card px-3 text-sm"
        >
          <option value="newest">{t("catalog.sortNewest")}</option>
          <option value="price-asc">{t("catalog.sortPriceAsc")}</option>
          <option value="price-desc">{t("catalog.sortPriceDesc")}</option>
          <option value="name">{t("catalog.sortName")}</option>
        </select>
      </div>

      {/* category chips */}
      <div className="flex flex-wrap items-center gap-2">
        <Chip active={!category} onClick={() => setParam("category", "")}>{t("catalog.all")}</Chip>
        {categories.data
          ?.filter((c) => shop || c.productCount > 0)
          .map((c) => (
            <Chip key={c.id} active={category === c.slug} onClick={() => setParam("category", c.slug)}>
              {t(`categories.${c.slug}`, { defaultValue: c.name })}
              {!shop && <span className="ml-1.5 opacity-60">{c.productCount}</span>}
            </Chip>
          ))}
        {search && (
          <Button variant="ghost" size="sm" onClick={() => setParam("search", "")}>
            <X className="h-4 w-4" /> {t("catalog.clearSearch")}
          </Button>
        )}
      </div>

      {/* grid: loading → skeletons · error · empty · products */}
      {products.isLoading ? (
        <div className={GRID}>
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="space-y-3">
              <div className="aspect-[4/3] animate-pulse rounded-2xl bg-muted" />
              <div className="h-4 w-2/3 animate-pulse rounded bg-muted" />
            </div>
          ))}
        </div>
      ) : products.error ? (
        <p className="text-destructive">❌ {products.error.message}</p>
      ) : data && data.items.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-border p-12 text-center text-muted-foreground">
          {t("catalog.empty")}
        </div>
      ) : (
        <div className={`${GRID} ${products.isFetching ? "opacity-60 transition" : ""}`}>
          {data?.items.map((p) => <ProductCard key={p.id} product={p} />)}
        </div>
      )}

      {/* page buttons */}
      {data && data.totalPages > 1 && (
        <div className="flex flex-wrap items-center justify-center gap-3 pt-4">
          <Button variant="outline" disabled={page <= 1} onClick={() => setParam("page", String(page - 1))}>
            {t("catalog.prev")}
          </Button>
          <span className="text-sm text-muted-foreground">
            {t("catalog.pageOf", { page, total: data.totalPages })}
          </span>
          <Button variant="outline" disabled={page >= data.totalPages} onClick={() => setParam("page", String(page + 1))}>
            {t("catalog.next")}
          </Button>
        </div>
      )}
    </section>
  );
}