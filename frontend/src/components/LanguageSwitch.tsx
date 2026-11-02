import { useTranslation } from "react-i18next";

const languages = ["de", "en"];

// DE | EN switch: the active language is dark
export function LanguageSwitch() {
  const { i18n } = useTranslation();

  return (
    <div className="flex h-9 overflow-hidden rounded-lg border border-border text-xs font-bold">
      {languages.map((lng) => (
        <button
          key={lng}
          type="button"
          onClick={() => i18n.changeLanguage(lng)}
          className={`w-10 ${i18n.language === lng ? "bg-foreground text-background" : "text-muted-foreground hover:text-foreground"}`}
        >
          {lng.toUpperCase()}
        </button>
      ))}
    </div>
  );
}