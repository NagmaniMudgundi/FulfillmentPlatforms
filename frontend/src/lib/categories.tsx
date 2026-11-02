import { Footprints, House, Laptop, Package, Shirt, Sparkles, type LucideProps } from "lucide-react";

// the icon for a category slug (fallback: a parcel)
export function CategoryIcon({ slug, ...props }: { slug: string } & LucideProps) {
  switch (slug) {
    case "fashion": return <Shirt {...props} />;
    case "shoes": return <Footprints {...props} />;
    case "beauty": return <Sparkles {...props} />;
    case "electronics": return <Laptop {...props} />;
    case "home": return <House {...props} />;
    default: return <Package {...props} />;
  }
}