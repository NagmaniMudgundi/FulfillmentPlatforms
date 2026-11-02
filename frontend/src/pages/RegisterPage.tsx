import { useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Check } from "lucide-react";
import { useAuth } from "@/auth/AuthContext";
import { api } from "@/lib/api";
import type { ShopResponse } from "@/types/api";
import { AuthBrandPanel } from "@/components/AuthBrandPanel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export function RegisterPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const shopSlug = searchParams.get("shop");               // /register?shop=modehaus

  // load the shop only if the link came from a shop
  const shop = useQuery({
    queryKey: ["shop", shopSlug],
    queryFn: () => api<ShopResponse>(`/api/shops/${shopSlug}`),
    enabled: !!shopSlug,
  });

  const [form, setForm] = useState({ firstName: "", lastName: "", email: "", password: "" });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // one change handler for all fields: the input's "name" says which field
  const update = (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm({ ...form, [e.target.name]: e.target.value });

  // the same password rules as the backend (Program.cs)
  const rules = [
    { ok: form.password.length >= 8, label: t("auth.ruleLength") },
    { ok: /\d/.test(form.password), label: t("auth.ruleDigit") },
    { ok: /[A-Z]/.test(form.password), label: t("auth.ruleUpper") },
  ];

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await api("/api/auth/register", { method: "POST", body: JSON.stringify(form) });
      await login(form.email, form.password);                       // log in right away
      navigate(shopSlug ? `/shop/${shopSlug}` : "/", { replace: true });
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  const shopName = shop.data?.name;

  return (
    <div className="grid min-h-screen bg-background lg:grid-cols-2">
      <AuthBrandPanel
        color={shop.data?.brandColor ?? "#1E1B4B"}
        badge={shopName}
        title={shopName ? t("auth.registerAtShop", { shop: shopName }) : t("auth.registerTitle")}
        text={t("auth.registerText")}
      />

      <section className="flex items-center justify-center p-8">
        <form onSubmit={handleSubmit} className="w-full max-w-md space-y-5">
          <div className="space-y-2">
            <h2 className="text-3xl font-extrabold">{t("auth.createAccount")}</h2>
            <p className="text-muted-foreground">
              {t("auth.haveAccount")} <Link to="/login" className="font-semibold text-primary">{t("auth.signIn")}</Link>
            </p>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <label htmlFor="firstName" className="text-sm font-medium">{t("auth.firstName")}</label>
              <Input id="firstName" name="firstName" required value={form.firstName} onChange={update} />
            </div>
            <div className="space-y-2">
              <label htmlFor="lastName" className="text-sm font-medium">{t("auth.lastName")}</label>
              <Input id="lastName" name="lastName" required value={form.lastName} onChange={update} />
            </div>
          </div>

          <div className="space-y-2">
            <label htmlFor="email" className="text-sm font-medium">{t("auth.email")}</label>
            <Input id="email" name="email" type="email" required value={form.email} onChange={update} />
          </div>

          <div className="space-y-2">
            <label htmlFor="password" className="text-sm font-medium">{t("auth.password")}</label>
            <Input id="password" name="password" type="password" required value={form.password} onChange={update} />
            {/* live password rules */}
            <ul className="space-y-1 pt-1 text-xs">
              {rules.map((r) => (
                <li key={r.label} className={`flex items-center gap-2 ${r.ok ? "text-emerald-600" : "text-muted-foreground"}`}>
                  <Check className="h-3.5 w-3.5" /> {r.label}
                </li>
              ))}
            </ul>
          </div>

          {error && (
            <p role="alert" className="rounded-lg bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</p>
          )}

          <Button type="submit" className="h-11 w-full" disabled={busy || rules.some((r) => !r.ok)}>
            {busy ? t("auth.creating") : t("auth.createAccount")}
          </Button>
        </form>
      </section>
    </div>
  );
}