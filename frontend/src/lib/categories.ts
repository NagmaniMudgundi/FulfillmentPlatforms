import { Footprints, House, Laptop, Package, Shirt, Sparkles, type LucideIcon } from "lucide-react";

const icons: Record<string, LucideIcon> = {
  fashion: Shirt,
  shoes: Footprints,
  beauty: Sparkles,
  electronics: Laptop,
  home: House,
};

// the icon for a category slug (fallback: a parcel)
export function categoryIcon(slug: string): LucideIcon {
  return icons[slug] ?? Package;
}