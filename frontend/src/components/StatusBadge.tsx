import { useTranslation } from "react-i18next";
import type { OrderStatus } from "@/types/api";

// the status colours of our design, with dark mode variants
const styles: Record<OrderStatus, string> = {
  Pending: "bg-amber-100 text-amber-800 dark:bg-amber-500/15 dark:text-amber-300",
  Processing: "bg-blue-100 text-blue-800 dark:bg-blue-500/15 dark:text-blue-300",
  Shipped: "bg-violet-100 text-violet-800 dark:bg-violet-500/15 dark:text-violet-300",
  Delivered: "bg-emerald-100 text-emerald-800 dark:bg-emerald-500/15 dark:text-emerald-300",
  Cancelled: "bg-rose-100 text-rose-800 dark:bg-rose-500/15 dark:text-rose-300",
};

export function StatusBadge({ status }: { status: OrderStatus }) {
  const { t } = useTranslation();
  return (
    <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-bold ${styles[status]}`}>
      {t(`status.${status}`)}
    </span>
  );
}