import { useTranslation } from "react-i18next";

// Temporary page: we replace these one by one in 10.4 – 10.8
export function ComingSoon({ title }: { title: string }) {
  const { t } = useTranslation();

  return (
    <div className="rounded-2xl border border-dashed border-border bg-card p-16 text-center">
      <h1 className="text-2xl font-extrabold">{title}</h1>
      <p className="mt-2 text-muted-foreground">{t("common.comingSoon")} 🚧</p>
    </div>
  );
}