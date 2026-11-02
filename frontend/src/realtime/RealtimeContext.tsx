import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import i18n from "@/i18n";
import { useAuth } from "@/auth/AuthContext";
import type { OrderStatusChangedEvent } from "@/types/api";

export type LiveState = "offline" | "connecting" | "live" | "reconnecting";

const RealtimeContext = createContext<LiveState>("offline");
const HUB_URL = `${import.meta.env.VITE_API_URL}/hubs/orders`;

// ONE SignalR connection for the whole app: opened at login, closed at logout
export function RealtimeProvider({ children }: { children: ReactNode }) {
  const { user, isAdmin } = useAuth();
  const queryClient = useQueryClient();
  const [state, setState] = useState<LiveState>("connecting");
  const token = user?.token;

  useEffect(() => {
    if (!token) return;                                       // nobody logged in → no connection

    let active = true;                                        // ignore events after cleanup

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })  // token in the URL (WebSockets have no headers)
      .withAutomaticReconnect()                                // network gone? try again by itself
      .configureLogging(LogLevel.Warning)
      .build();

    // the server pushes a status change
    connection.on("OrderStatusChanged", (e: OrderStatusChangedEvent) => {
      if (!active) return;

      // the data on screen is outdated → React Query reloads it
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });

      const isNewOrder = e.oldStatus === "None";
      if (isNewOrder && isAdmin) {
        toast.info(i18n.t("live.newOrder", { id: e.orderId }));
      } else if (!isNewOrder) {
        toast.info(i18n.t("live.statusChanged", { id: e.orderId, status: i18n.t(`status.${e.newStatus}`) }));
      }
    });

    // state changes only in callbacks (the lint rule: no setState directly in the effect body)
    connection.onreconnecting(() => active && setState("reconnecting"));
    connection.onreconnected(() => active && setState("live"));
    connection.onclose(() => active && setState("offline"));

    connection
      .start()
      .then(() => active && setState("live"))
      .catch(() => active && setState("offline"));

    // cleanup: logout or another user → close this connection
    return () => {
      active = false;
      connection.stop();
    };
  }, [token, isAdmin, queryClient]);

  // no token → always "offline" (calculated, not stored)
  const value: LiveState = token ? state : "offline";

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

// any component: const live = useLiveState();  → "live" | "offline" | …
export function useLiveState() {
  return useContext(RealtimeContext);
}