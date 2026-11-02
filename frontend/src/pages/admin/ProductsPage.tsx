import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Eye, EyeOff, Pencil, Plus, Search } from "lucide-react";
import { api } from "@/lib/api";
import { formatPrice } from "@/lib/format";
import type { CategoryResponse, ProductResponse } from "@/types/api";
import { Button } from "@/components/ui/button";
import { ProductFormDialog } from "./ProductFormDialog";

export function ProductsPage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<ProductResponse | null>(null);

  const products = useQuery({
    queryKey: ["admin-products"],
    queryFn: () => api<ProductResponse[]>("/api/products"),      // only MY shop (query filter)
  });

  const categories = useQuery({
    queryKey: ["categories"],
    queryFn: () => api<CategoryResponse[]>("/api/catalog/categories"),
  });

  // quick action: show/hide without opening the form
  const toggleActive = useMutation({
    mutationFn: (p: ProductResponse) =>
      api<ProductResponse>(`/api/products/${p.id}`, {
        method: "PUT",
        body: JSON.stringify({
          name: p.name, price: p.price, stockQuantity: p.stockQuantity, categoryId: p.categoryId,
          description: p.description, imageUrl: p.imageUrl, isActive: !p.isActive,
        }),
      }),
    onSuccess: (saved) => {
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      queryClient.invalidateQueries({ queryKey: ["catalog"] });
      toast.success(t(saved.isActive ? "productsAdmin.shown" : "productsAdmin.hidden", { name: saved.name }));
    },
    onError: (e: Error) => toast.error(e.message),
  });

  function openNew() {
    setEditing(null);
    setDialogOpen(true);
  }

  function openEdit(p: ProductResponse) {
    setEditing(p);
    setDialogOpen(true);
  }

  const categoryName = (id: number) => {
    const c = categories.data?.find((x) => x.id === id);
    return c ? t(`categories.${c.slug}`, { defaultValue: c.name }) : "—";
  };

  // client-side search: one shop has few products, no need to ask the server
  const term = search.trim().toLowerCase();
  const list = (products.data ?? []).filter(
    (p) => !term || p.name.toLowerCase().includes(term) || p.sku.toLowerCase().includes(term)
  );

  if (products.isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-muted" />;
  if (products.error) return <p className="text-destructive">❌ {products.error.message}</p>;

  return (
    <div className="space-y-6">
      {/* header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight">{t("productsAdmin.title")}</h1>
          <p className="text-muted-foreground">{t("productsAdmin.subtitle", { count: products.data?.length ?? 0 })}</p>
        </div>
        <div className="flex gap-3">
          <div className="relative">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
            <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder={t("productsAdmin.search")}
              className="h-9 w-64 rounded-lg border border-border bg-card pl-9 pr-3 text-sm outline-none focus:border-primary" />
          </div>
          <Button onClick={openNew}><Plus className="h-4 w-4" /> {t("productsAdmin.new")}</Button>
        </div>
      </div>

      {/* table */}
      <div className="overflow-x-auto rounded-2xl border border-border bg-card">
        <table className="w-full min-w-[800px] text-sm">
          <thead className="bg-muted text-left text-muted-foreground">
            <tr>
              <th className="px-5 py-3 font-semibold">{t("productsAdmin.name")}</th>
              <th className="px-5 py-3 font-semibold">{t("productsAdmin.category")}</th>
              <th className="px-5 py-3 font-semibold">{t("productsAdmin.price")}</th>
              <th className="px-5 py-3 font-semibold">{t("productsAdmin.stock")}</th>
              <th className="px-5 py-3 font-semibold">{t("productsAdmin.status")}</th>
              <th className="px-5 py-3" />
            </tr>
          </thead>
          <tbody>
            {list.map((p) => (
              <tr key={p.id} className={`border-t border-border ${p.isActive ? "" : "opacity-60"}`}>
                <td className="px-5 py-3">
                  <p className="font-semibold">{p.name}</p>
                  <p className="text-xs text-muted-foreground">SKU {p.sku}</p>
                </td>
                <td className="px-5 py-3">{categoryName(p.categoryId)}</td>
                <td className="px-5 py-3 font-semibold">{formatPrice(p.price, lang)}</td>
                <td className="px-5 py-3">
                  <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${
                    p.stockQuantity === 0
                      ? "bg-rose-100 text-rose-800 dark:bg-rose-500/15 dark:text-rose-300"
                      : p.stockQuantity <= 5
                        ? "bg-amber-100 text-amber-800 dark:bg-amber-500/15 dark:text-amber-300"
                        : "bg-emerald-100 text-emerald-800 dark:bg-emerald-500/15 dark:text-emerald-300"
                  }`}>
                    {p.stockQuantity}
                  </span>
                </td>
                <td className="px-5 py-3">
                  <span className="text-xs font-semibold">{p.isActive ? t("productsAdmin.active") : t("productsAdmin.hiddenLabel")}</span>
                </td>
                <td className="px-5 py-3">
                  <div className="flex justify-end gap-1">
                    <Button variant="ghost" size="sm" onClick={() => toggleActive.mutate(p)}
                      aria-label={p.isActive ? t("productsAdmin.hide") : t("productsAdmin.show")}>
                      {p.isActive ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => openEdit(p)} aria-label={t("productsAdmin.edit")}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
            {list.length === 0 && (
              <tr><td colSpan={6} className="px-5 py-10 text-center text-muted-foreground">{t("catalog.empty")}</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {/* the dialog: key = forces a fresh form for each product */}
      {dialogOpen && (
        <ProductFormDialog key={editing?.id ?? "new"} open={dialogOpen} onOpenChange={setDialogOpen} product={editing} />
      )}
    </div>
  );
}