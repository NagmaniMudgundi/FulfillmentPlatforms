import { Package } from "lucide-react";

interface Props {
  color: string;          // background: indigo for login, the shop's colour for register
  title: string;
  text: string;
  badge?: string;         // optional small label at the top
}

// left half of the login/register pages (hidden on small screens)
export function AuthBrandPanel({ color, title, text, badge }: Props) {
  return (
    <section
      className="relative hidden flex-col justify-between overflow-hidden p-14 text-white lg:flex"
      style={{ background: color }}
    >
      <div className="flex items-center gap-3">
        <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-white/15">
          <Package className="h-5 w-5" />
        </span>
        <span className="text-xl font-extrabold">FulfillmentPlatform</span>
      </div>

      <div className="max-w-lg space-y-5">
        {badge && (
          <span className="inline-block rounded-full bg-white/15 px-3 py-1 text-xs font-bold uppercase tracking-wider">
            {badge}
          </span>
        )}
        <h1 className="text-5xl font-extrabold leading-tight tracking-tight">{title}</h1>
        <p className="text-lg text-white/80">{text}</p>
      </div>

      {/* mini live-tracking card, like in the mockup */}
      <div className="max-w-lg space-y-3 rounded-2xl bg-white/10 p-5">
        <div className="flex justify-between text-sm font-semibold">
          <span>Order #1042</span>
          <span className="flex items-center gap-2 text-emerald-300">
            <span className="h-2 w-2 rounded-full bg-emerald-400" /> Live
          </span>
        </div>
        <div className="grid grid-cols-4 gap-2 text-xs">
          {["Pending", "Processing", "Shipped", "Delivered"].map((step, i) => (
            <div key={step} className="space-y-2">
              <div className={`h-1.5 rounded-full ${i < 3 ? "bg-emerald-400" : "bg-white/20"}`} />
              <span className="text-white/80">{step}</span>
            </div>
          ))}
        </div>
      </div>

      <p className="text-sm text-white/60">ASP.NET Core 8 · EF Core · SignalR · RabbitMQ · React</p>

      <div className="pointer-events-none absolute -bottom-32 -right-32 h-96 w-96 rounded-full bg-white/5" />
    </section>
  );
}