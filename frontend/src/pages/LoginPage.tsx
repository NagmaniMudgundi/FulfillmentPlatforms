import { useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { useAuth } from "@/auth/AuthContext";
import { ApiError } from "@/lib/api";
import { AuthBrandPanel } from "@/components/AuthBrandPanel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

// demo accounts from the seeder: one click fills the form (great for recruiters)
const demoAccounts = [
  { label: "Customer · Anna", email: "anna@test.de" },
  { label: "Admin · ModeHaus", email: "max@modehaus.de" },
];

export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from;   // where RequireRole sent us from

  // controlled inputs: each field is linked to state
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();                          // stop the browser from reloading the page
    setError(null);
    setBusy(true);
    try {
      const user = await login(email, password);
      const isAdmin = user.roles.includes("TenantAdmin");
      navigate(isAdmin ? "/admin" : from ?? "/", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401 ? t("auth.invalid") : (err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="grid min-h-screen bg-background lg:grid-cols-2">
      <AuthBrandPanel color="#1E1B4B" title={t("auth.loginTitle")} text={t("auth.loginText")} />

      <section className="flex items-center justify-center p-8">
        <form onSubmit={handleSubmit} className="w-full max-w-md space-y-6">
          <div className="space-y-2">
            <h2 className="text-3xl font-extrabold">{t("auth.signIn")}</h2>
            <p className="text-muted-foreground">{t("auth.welcomeBack")}</p>
          </div>

          <div className="space-y-2">
            <label htmlFor="email" className="text-sm font-medium">{t("auth.email")}</label>
            <Input id="email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>

          <div className="space-y-2">
            <label htmlFor="password" className="text-sm font-medium">{t("auth.password")}</label>
            <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>

          {error && (
            <p role="alert" className="rounded-lg bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</p>
          )}

          <Button type="submit" className="h-11 w-full" disabled={busy}>
            {busy ? t("auth.signingIn") : t("auth.signIn")}
          </Button>

          {/* demo logins */}
          <div className="rounded-xl border border-dashed border-border p-4 text-sm">
            <p className="mb-2 font-semibold">{t("auth.demo")} <span className="font-normal text-muted-foreground">(Passwort1)</span></p>
            <div className="flex flex-wrap gap-2">
              {demoAccounts.map((d) => (
                <Button key={d.email} type="button" variant="outline" size="sm"
                  onClick={() => { setEmail(d.email); setPassword("Passwort1"); }}>
                  {d.label}
                </Button>
              ))}
            </div>
          </div>

          <p className="text-center text-sm text-muted-foreground">
            {t("auth.noAccount")} <Link to="/register" className="font-semibold text-primary">{t("auth.createAccount")}</Link>
          </p>
        </form>
      </section>
    </div>
  );
}