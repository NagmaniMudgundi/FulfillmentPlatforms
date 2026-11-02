import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { api } from "@/lib/api";
import type { CategoryResponse, ProductResponse } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  product: ProductResponse | null;          // null = create a new product, otherwise edit this one
}

// ONE form for create and edit
export function ProductFormDialog({ open, onOpenChange, product }: Props) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const isEdit = product !== null;

  const categories = useQuery({
    queryKey: ["categories"],
    queryFn: () => api<CategoryResponse[]>("/api/catalog/categories"),
  });

  // start values: the product's data (edit) or empty fields (create)
  const [form, setForm] = useState({
    name: product?.name ?? "",
    sku: product?.sku ?? "",
    price: product?.price.toString() ?? "",
    stockQuantity: product?.stockQuantity.toString() ?? "0",
    categoryId: product?.categoryId.toString() ?? "",
    description: product?.description ?? "",
    isActive: product?.isActive ?? true,
  });

  const save = useMutation({
    mutationFn: () => {
      const body = {
        name: form.name.trim(),
        price: Number(form.price),
        stockQuantity: Number(form.stockQuantity),
        categoryId: Number(form.categoryId),
        description: form.description.trim() || null,
        imageUrl: null,
        isActive: form.isActive,
      };
      return isEdit
        ? api<ProductResponse>(`/api/products/${product.id}`, { method: "PUT", body: JSON.stringify(body) })
        : api<ProductResponse>("/api/products", { method: "POST", body: JSON.stringify({ ...body, sku: form.sku.trim() }) });
    },
    onSuccess: (saved) => {
      // everything that shows products is outdated now
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      queryClient.invalidateQueries({ queryKey: ["catalog"] });
      queryClient.invalidateQueries({ queryKey: ["product"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success(t(isEdit ? "productsAdmin.updated" : "productsAdmin.created", { name: saved.name }));
      onOpenChange(false);
    },
  });

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    save.mutate();
  }

  // one handler for all text fields: the "name" attribute says which field
  const update = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
    setForm({ ...form, [e.target.name]: e.target.value });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEdit ? t("productsAdmin.edit") : t("productsAdmin.new")}</DialogTitle>
          <DialogDescription>{t("productsAdmin.formHint")}</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <label htmlFor="name" className="text-sm font-medium">{t("productsAdmin.name")}</label>
            <Input id="name" name="name" required maxLength={200} value={form.name} onChange={update} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="sku" className="text-sm font-medium">SKU</label>
              <Input id="sku" name="sku" required maxLength={100} value={form.sku} onChange={update} disabled={isEdit} />
            </div>
            <div className="space-y-1.5">
              <label htmlFor="categoryId" className="text-sm font-medium">{t("productsAdmin.category")}</label>
              <select id="categoryId" name="categoryId" required value={form.categoryId} onChange={update}
                className="h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm">
                <option value="" disabled>{t("productsAdmin.choose")}</option>
                {categories.data?.map((c) => (
                  <option key={c.id} value={c.id}>{t(`categories.${c.slug}`, { defaultValue: c.name })}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="price" className="text-sm font-medium">{t("productsAdmin.price")} (€)</label>
              <Input id="price" name="price" type="number" step="0.01" min="0.01" required value={form.price} onChange={update} />
            </div>
            <div className="space-y-1.5">
              <label htmlFor="stockQuantity" className="text-sm font-medium">{t("productsAdmin.stock")}</label>
              <Input id="stockQuantity" name="stockQuantity" type="number" min="0" max="100000" required value={form.stockQuantity} onChange={update} />
            </div>
          </div>

          <div className="space-y-1.5">
            <label htmlFor="description" className="text-sm font-medium">{t("product.description")}</label>
            <textarea id="description" name="description" rows={3} maxLength={1000} value={form.description} onChange={update}
              className="w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm" />
          </div>

          <label className="flex items-center gap-3 rounded-lg border border-border p-3 text-sm">
            <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              className="h-4 w-4 accent-primary" />
            <span>
              <span className="font-semibold">{t("productsAdmin.visible")}</span>
              <span className="block text-muted-foreground">{t("productsAdmin.visibleHint")}</span>
            </span>
          </label>

          {save.error && (
            <p role="alert" className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">{save.error.message}</p>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t("productsAdmin.cancel")}</Button>
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? t("productsAdmin.saving") : t("productsAdmin.save")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}