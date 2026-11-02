import { useState } from "react";
import {
  DndContext, DragOverlay, KeyboardSensor, PointerSensor,
  useDraggable, useDroppable, useSensor, useSensors,
  type DragEndEvent, type DragStartEvent,
} from "@dnd-kit/core";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Clock } from "lucide-react";
import { api } from "@/lib/api";
import { formatDateTime, formatPrice } from "@/lib/format";
import type { OrderResponse, OrderStatus, PagedResult } from "@/types/api";

const COLUMNS: OrderStatus[] = ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"];
const COLORS: Record<OrderStatus, string> = {
  Pending: "#F59E0B", Processing: "#3B82F6", Shipped: "#8B5CF6", Delivered: "#10B981", Cancelled: "#F43F5E",
};

// the SAME rules as OrderStatusTransitions in the backend (the backend still has the final say)
const ALLOWED: Record<OrderStatus, OrderStatus[]> = {
  Pending: ["Processing", "Cancelled"],
  Processing: ["Shipped", "Cancelled"],
  Shipped: ["Delivered"],
  Delivered: [],
  Cancelled: [],
};

const BOARD_KEY = ["orders", { board: true }];
const AUTO_CANCEL_WARNING_MS = 2 * 24 * 60 * 60 * 1000;      // Pending for 2+ days → the Hangfire job cancels after 3

type Board = PagedResult<OrderResponse>;

export function BoardPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [activeOrder, setActiveOrder] = useState<OrderResponse | null>(null);

  // mouse: start dragging after 6 px (so a click is not a drag) · keyboard: Space + arrow keys
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor)
  );

  const board = useQuery({
    queryKey: BOARD_KEY,
    queryFn: () => api<Board>("/api/orders?pageSize=100"),
  });

  // change the status, with an OPTIMISTIC update + rollback
  const move = useMutation({
    mutationFn: ({ order, to }: { order: OrderResponse; to: OrderStatus }) =>
      to === "Cancelled"
        ? api<OrderResponse>(`/api/orders/${order.id}/cancel`, { method: "POST", body: JSON.stringify({ reason: "Cancelled by shop" }) })
        : api<OrderResponse>(`/api/orders/${order.id}/status`, { method: "PATCH", body: JSON.stringify({ newStatus: to }) }),

    // ① BEFORE the server answers: move the card on screen
    onMutate: async ({ order, to }) => {
      await queryClient.cancelQueries({ queryKey: BOARD_KEY });
      const previous = queryClient.getQueryData<Board>(BOARD_KEY);           // remember the old state
      queryClient.setQueryData<Board>(BOARD_KEY, (old) =>
        old && { ...old, items: old.items.map((o) => (o.id === order.id ? { ...o, status: to } : o)) }
      );
      return { previous };
    },

    // ② the server said no → put the card back
    onError: (error: Error, _variables, context) => {
      if (context?.previous) queryClient.setQueryData(BOARD_KEY, context.previous);
      toast.error(error.message);
    },

    onSuccess: (_data, { order, to }) =>
      toast.success(t("board.moved", { id: order.id, status: t(`status.${to}`) })),

    // ③ either way: reload the real data (board, customer lists, dashboard)
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });

  const orders = board.data?.items ?? [];

  function handleDragStart(event: DragStartEvent) {
    setActiveOrder(orders.find((o) => o.id === event.active.id) ?? null);
  }

  function handleDragEnd(event: DragEndEvent) {
    const order = activeOrder;
    setActiveOrder(null);
    const to = event.over?.id as OrderStatus | undefined;          // the column it was dropped on

    if (!order || !to || to === order.status) return;

    if (!ALLOWED[order.status].includes(to)) {
      toast.error(t("board.notAllowed", { from: t(`status.${order.status}`), to: t(`status.${to}`) }));
      return;
    }
    if (to === "Cancelled" && !confirm(t("board.confirmCancel", { id: order.id }))) return;

    move.mutate({ order, to });
  }

  if (board.isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-muted" />;
  if (board.error) return <p className="text-destructive">❌ {board.error.message}</p>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-extrabold tracking-tight">{t("board.title")}</h1>
        <p className="text-muted-foreground">{t("board.subtitle")}</p>
      </div>

      <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd} onDragCancel={() => setActiveOrder(null)}>
        <div className="grid gap-4 overflow-x-auto pb-4" style={{ gridTemplateColumns: "repeat(5, minmax(240px, 1fr))" }}>
          {COLUMNS.map((status) => (
            <Column key={status} status={status} orders={orders.filter((o) => o.status === status)} dragging={activeOrder} />
          ))}
        </div>

        {/* the card that follows the mouse while dragging */}
        <DragOverlay>{activeOrder && <OrderCard order={activeOrder} overlay />}</DragOverlay>
      </DndContext>
    </div>
  );
}

// one column = one status = one drop zone
function Column({ status, orders, dragging }: { status: OrderStatus; orders: OrderResponse[]; dragging: OrderResponse | null }) {
  const { t } = useTranslation();
  const { setNodeRef, isOver } = useDroppable({ id: status });

  // while dragging: green = allowed target, red/faded = not allowed
  const isTarget = !!dragging && dragging.status !== status;
  const allowed = !!dragging && ALLOWED[dragging.status].includes(status);
  const zone = !isTarget
    ? "border-transparent"
    : allowed
      ? isOver ? "border-emerald-500 bg-emerald-500/10" : "border-dashed border-emerald-500/60"
      : isOver ? "border-rose-500 bg-rose-500/10" : "border-transparent opacity-50";

  return (
    <section ref={setNodeRef} className={`flex min-h-[65vh] flex-col gap-3 rounded-2xl border-2 bg-muted/50 p-3 transition ${zone}`}>
      <div className="flex items-center gap-2 px-1 py-1">
        <span className="h-2.5 w-2.5 rounded-full" style={{ background: COLORS[status] }} />
        <h2 className="text-sm font-bold">{t(`status.${status}`)}</h2>
        <span className="ml-auto rounded-full bg-card px-2 py-0.5 text-xs font-bold text-muted-foreground">{orders.length}</span>
      </div>

      {orders.map((o) => <DraggableCard key={o.id} order={o} />)}

      {isTarget && isOver && !allowed && (
        <p className="rounded-xl border-2 border-dashed border-rose-400 p-4 text-center text-sm font-semibold text-rose-600">
          {t("board.notHere")}
        </p>
      )}
    </section>
  );
}

// makes a card draggable
function DraggableCard({ order }: { order: OrderResponse }) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: order.id });
  return (
    <div ref={setNodeRef} {...listeners} {...attributes} className={isDragging ? "opacity-30" : ""}>
      <OrderCard order={order} />
    </div>
  );
}

// the card itself (also used as the overlay while dragging)
function OrderCard({ order, overlay = false }: { order: OrderResponse; overlay?: boolean }) {
  const { t, i18n } = useTranslation();
  const lang = i18n.language;
  const [now] = useState(() => Date.now());          // read the clock ONCE (not on every render)
  const itemCount = order.items.reduce((sum, i) => sum + i.quantity, 0);
  const autoCancelSoon = order.status === "Pending" && now - new Date(order.createdAt).getTime() > AUTO_CANCEL_WARNING_MS;

  return (
    <article
      className={`cursor-grab space-y-2 rounded-xl border border-border bg-card p-3 shadow-sm active:cursor-grabbing ${
        overlay ? "rotate-2 shadow-2xl" : ""
      }`}
      style={{ borderLeft: `4px solid ${COLORS[order.status]}` }}
    >
      <div className="flex justify-between text-sm">
        <span className="font-bold">#{order.id}</span>
        <span className="font-bold">{formatPrice(order.totalAmount, lang)}</span>
      </div>
      <p className="text-xs text-muted-foreground">
        {t("cart.items", { count: itemCount })} · {formatDateTime(order.createdAt, lang)}
      </p>
      {autoCancelSoon && (
        <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-bold text-amber-800 dark:bg-amber-500/15 dark:text-amber-300">
          <Clock className="h-3 w-3" /> {t("board.autoCancel")}
        </span>
      )}
    </article>
  );
}