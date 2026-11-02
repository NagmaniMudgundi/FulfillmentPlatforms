const locale = (lang: string) => (lang === "de" ? "de-DE" : "en-GB");

// 89 → "89,00 €" (de) or "€89.00" (en)
export function formatPrice(value: number, lang: string) {
  return new Intl.NumberFormat(locale(lang), { style: "currency", currency: "EUR" }).format(value);
}

// "2026-10-30T09:00:00Z" → "30.10.2026, 10:00" (de)
export function formatDateTime(value: string, lang: string) {
  return new Intl.DateTimeFormat(locale(lang), { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

// delivery estimate: 2 working days from today (Saturday/Sunday don't count)
export function formatDeliveryDate(lang: string) {
  const date = new Date();
  let workingDays = 0;
  while (workingDays < 2) {
    date.setDate(date.getDate() + 1);
    const day = date.getDay();                    // 0 = Sunday, 6 = Saturday
    if (day !== 0 && day !== 6) workingDays++;
  }
  return date.toLocaleDateString(locale(lang), { weekday: "short", day: "numeric", month: "short" });
}