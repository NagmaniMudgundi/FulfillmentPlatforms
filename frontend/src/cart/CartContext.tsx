import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import type { CatalogProductResponse } from "@/types/api";

const STORAGE_KEY = "cart";
const MAX_QUANTITY = 100;                                   // same limit as the backend [Range(1, 100)]

export interface CartItem {
  productId: number;
  name: string;
  price: number;                                            // for display only: the backend uses the current price
  categorySlug: string;
  shopName: string;
  shopSlug: string;
  shopBrandColor: string | null;
  quantity: number;
}

// the cart items of ONE shop = one future order
export interface CartGroup {
  shopSlug: string;
  shopName: string;
  shopBrandColor: string | null;
  items: CartItem[];
  subtotal: number;
}

interface CartContextValue {
  items: CartItem[];
  groups: CartGroup[];
  count: number;
  total: number;
  add: (product: CatalogProductResponse, quantity?: number) => void;
  setQuantity: (productId: number, quantity: number) => void;
  remove: (productId: number) => void;
  clear: () => void;
}

const CartContext = createContext<CartContextValue | null>(null);

function loadCart(): CartItem[] {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "[]") as CartItem[];
  } catch {
    return [];
  }
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(loadCart);

  // every change → save to the browser
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  }, [items]);

  // already in the cart → increase the quantity; otherwise add a new line
  const add = useCallback((product: CatalogProductResponse, quantity = 1) => {
    setItems((current) => {
      const existing = current.find((i) => i.productId === product.id);
      if (existing) {
        return current.map((i) =>
          i.productId === product.id ? { ...i, quantity: Math.min(i.quantity + quantity, MAX_QUANTITY) } : i
        );
      }
      return [
        ...current,
        {
          productId: product.id,
          name: product.name,
          price: product.price,
          categorySlug: product.categorySlug,
          shopName: product.shopName,
          shopSlug: product.shopSlug,
          shopBrandColor: product.shopBrandColor,
          quantity,
        },
      ];
    });
  }, []);

  // quantity 0 → remove the line
  const setQuantity = useCallback((productId: number, quantity: number) => {
    setItems((current) =>
      quantity <= 0
        ? current.filter((i) => i.productId !== productId)
        : current.map((i) => (i.productId === productId ? { ...i, quantity: Math.min(quantity, MAX_QUANTITY) } : i))
    );
  }, []);

  const remove = useCallback((productId: number) => {
    setItems((current) => current.filter((i) => i.productId !== productId));
  }, []);

  const clear = useCallback(() => setItems([]), []);

  const value = useMemo<CartContextValue>(() => {
    // group the items by shop: the same split the backend does at checkout
    const byShop = new Map<string, CartGroup>();
    for (const item of items) {
      const group = byShop.get(item.shopSlug) ?? {
        shopSlug: item.shopSlug,
        shopName: item.shopName,
        shopBrandColor: item.shopBrandColor,
        items: [],
        subtotal: 0,
      };
      group.items.push(item);
      group.subtotal += item.price * item.quantity;
      byShop.set(item.shopSlug, group);
    }

    return {
      items,
      groups: [...byShop.values()],
      count: items.reduce((sum, i) => sum + i.quantity, 0),
      total: items.reduce((sum, i) => sum + i.price * i.quantity, 0),
      add,
      setQuantity,
      remove,
      clear,
    };
  }, [items, add, setQuantity, remove, clear]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error("useCart must be used inside <CartProvider>");
  return ctx;
}