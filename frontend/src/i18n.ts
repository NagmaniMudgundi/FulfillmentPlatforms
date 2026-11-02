import i18n from "i18next";
import { initReactI18next } from "react-i18next";

// all texts in both languages
const resources = {
  en: {
    translation: {
      nav: {
        shop: "Shop", orders: "My orders", signIn: "Sign in", search: "Search all shops…", cart: "Cart",
        admin: "Admin area", hello: "Hi {{name}}", logout: "Sign out",
      },
      admin: { title: "Admin", dashboard: "Dashboard", board: "Order board", products: "Products" },
      home: {
        title: "Marketplace", loading: "Loading…", soldBy: "sold by {{shop}}", onlyLeft: "Only {{count}} left",
        badge: "One cart · many shops", visitShop: "Visit shop",
        heroTitle: "Shop from local German brands. Track every parcel live.",
        heroText: "Fashion, shoes and beauty in one checkout. Each shop ships its own part, and you follow every order in real time.",
      },
      catalog: {
        title: "Trending across all shops", resultsFor: "Results for “{{search}}”", all: "All",
        sort: "Sort", sortNewest: "Newest", sortPriceAsc: "Price: low to high", sortPriceDesc: "Price: high to low", sortName: "Name A–Z",
        prev: "Previous", next: "Next", pageOf: "Page {{page}} of {{total}}",
        empty: "No products found. Try another filter.", clearSearch: "Clear search", soldOut: "Sold out",
      },
      categories: { fashion: "Fashion", shoes: "Shoes", beauty: "Beauty", electronics: "Electronics", home: "Home", other: "Other" },
      shop: {
        allShops: "All shops", notFound: "This shop doesn't exist.", backHome: "Back to the marketplace",
        delivery: "Free delivery from €50", deliverySub: "DHL, 1–2 working days",
        returns: "30-day returns", returnsSub: "Free return label",
        invoice: "Pay on invoice", invoiceSub: "Kauf auf Rechnung",
        live: "Live order tracking", liveSub: "Updates the second they happen",
      },
      product: {
        notFound: "This product doesn't exist.", vat: "incl. 19% VAT", inStock: "In stock",
        addToCart: "Add to cart", added: "Added to cart", delivery: "Delivery by", payment: "Payment",
        description: "Description", moreFrom: "More from {{shop}}",
      },
      cart: {
        title: "Your cart", summary: "{{count}} items from {{shops}} shops — each shop ships its own parcel.",
        empty: "Your cart is empty", continue: "Continue shopping", soldBy: "Sold by {{shop}}",
        becomesOrder: "Order {{number}}", remove: "Remove", subtotal: "Subtotal {{shop}}",
        address: "Delivery address", street: "Street and number", postalCode: "Postal code",
        postalCodeHint: "5 digits, e.g. 72764", city: "City",
        overview: "Summary", items: "{{count}} items", vat: "incl. 19% VAT", total: "Total",
        placeOrders: "Place {{count}} order(s) · {{total}}", placing: "Placing orders…", loginToOrder: "Sign in to order",
        adminCantBuy: "Admin accounts can't buy. Please use a customer account.",
        nextTitle: "What happens when you order",
        next1: "Your cart is split into one order per shop, saved together in one transaction.",
        next2: "Each shop is notified live and prepares its parcel.",
        next3: "You get a confirmation email and can track every order live.",
      },
      success: {
        title: "Thank you for your order!", text: "{{count}} order(s) placed. Each shop ships its own parcel.",
        order: "Order #{{id}}", email: "A confirmation email is on its way.", track: "Track my orders",
      },
      status: { Pending: "Pending", Processing: "Processing", Shipped: "Shipped", Delivered: "Delivered", Cancelled: "Cancelled" },
      orders: {
        title: "My orders", subtitle: "Status updates appear here instantly, no need to refresh.",
        empty: "You haven't ordered anything yet.", placed: "Placed {{date}}",
        history: "History", product: "Product", qty: "Qty", unitPrice: "Unit price", lineTotal: "Total",
        cancel: "Cancel order", confirmCancel: "Do you really want to cancel this order?",
        cancelled: "Order cancelled", cancelledBanner: "This order was cancelled. The items are back in stock.",
      },
      live: {
        live: "Live tracking on", connecting: "Connecting…", reconnecting: "Reconnecting…", offline: "Offline",
        newOrder: "New order #{{id}} received", statusChanged: "Order #{{id}} is now {{status}}",
      },
      dashboard: {
        title: "Today at your shop", morning: "Good morning", day: "Good afternoon", evening: "Good evening",
        revenue: "Revenue", orders: "Orders", avgOrder: "Avg. order value", cancelRate: "Cancellation rate",
        last14: "Last 14 days", revenueChart: "Revenue, last 14 days", revenueLabel: "Revenue",
        byStatus: "Orders by status", openBoard: "Open order board", latest: "Latest orders",
        noOrders: "No orders yet.", lowStock: "Low stock", stockOk: "All products are well stocked. 👍",
        left: "{{count}} left", manageProducts: "Manage products",
      },
      board: {
        title: "Order board", subtitle: "Drag a card to change the order status. Only allowed moves are accepted.",
        moved: "Order #{{id}} → {{status}}", notAllowed: "{{from}} → {{to}} is not allowed",
        notHere: "Not allowed here", confirmCancel: "Cancel order #{{id}}? The items go back into stock.",
        autoCancel: "Auto-cancel soon",
      },
      productsAdmin: {
        title: "Products", subtitle: "{{count}} products in your shop", search: "Search name or SKU…",
        new: "New product", edit: "Edit product", formHint: "Changes are visible in the marketplace immediately.",
        name: "Product", category: "Category", choose: "Choose…", price: "Price", stock: "Stock", status: "Status",
        visible: "Visible in the shop", visibleHint: "Hidden products stay in old orders but can't be bought.",
        active: "Active", hiddenLabel: "Hidden", hide: "Hide", show: "Show",
        cancel: "Cancel", save: "Save", saving: "Saving…",
        created: "“{{name}}” created", updated: "“{{name}}” saved",
        shown: "“{{name}}” is visible again", hidden: "“{{name}}” is now hidden",
      },
      common: { comingSoon: "Coming soon", darkMode: "Toggle dark mode" },
      auth: {
        signIn: "Sign in", signingIn: "Signing in…", welcomeBack: "Welcome back. Sign in to see your orders.",
        email: "Email", password: "Password", invalid: "Invalid email or password.",
        demo: "Demo accounts", noAccount: "New here?", createAccount: "Create account", creating: "Creating account…",
        haveAccount: "Already have an account?", firstName: "First name", lastName: "Last name",
        loginTitle: "Every order, every shop, tracked live.",
        loginText: "One platform for many shops. Customers order, shops fulfil, and everyone sees each status change the moment it happens.",
        registerTitle: "One account for every shop.",
        registerAtShop: "Create your account and start shopping at {{shop}}.",
        registerText: "All your orders from every shop in one list, with live tracking.",
        ruleLength: "At least 8 characters", ruleDigit: "At least one number", ruleUpper: "At least one capital letter",
      },
    },
  },
  de: {
    translation: {
      nav: {
        shop: "Shop", orders: "Meine Bestellungen", signIn: "Anmelden", search: "In allen Shops suchen…", cart: "Warenkorb",
        admin: "Verwaltung", hello: "Hallo {{name}}", logout: "Abmelden",
      },
      admin: { title: "Verwaltung", dashboard: "Übersicht", board: "Bestell-Board", products: "Produkte" },
      home: {
        title: "Marktplatz", loading: "Wird geladen…", soldBy: "verkauft von {{shop}}", onlyLeft: "Nur noch {{count}} Stück",
        badge: "Ein Warenkorb · viele Shops", visitShop: "Zum Shop",
        heroTitle: "Kaufe bei lokalen deutschen Marken. Verfolge jedes Paket live.",
        heroText: "Mode, Schuhe und Kosmetik in einem Checkout. Jeder Shop versendet seinen Teil, und du verfolgst jede Bestellung in Echtzeit.",
      },
      catalog: {
        title: "Beliebt in allen Shops", resultsFor: "Ergebnisse für „{{search}}“", all: "Alle",
        sort: "Sortieren", sortNewest: "Neueste", sortPriceAsc: "Preis aufsteigend", sortPriceDesc: "Preis absteigend", sortName: "Name A–Z",
        prev: "Zurück", next: "Weiter", pageOf: "Seite {{page}} von {{total}}",
        empty: "Keine Produkte gefunden. Probiere einen anderen Filter.", clearSearch: "Suche löschen", soldOut: "Ausverkauft",
      },
      categories: { fashion: "Mode", shoes: "Schuhe", beauty: "Kosmetik", electronics: "Elektronik", home: "Wohnen", other: "Sonstiges" },
      shop: {
        allShops: "Alle Shops", notFound: "Diesen Shop gibt es nicht.", backHome: "Zurück zum Marktplatz",
        delivery: "Kostenloser Versand ab 50 €", deliverySub: "DHL, 1–2 Werktage",
        returns: "30 Tage Rückgabe", returnsSub: "Kostenloses Rücksendeetikett",
        invoice: "Kauf auf Rechnung", invoiceSub: "Bequem nach Erhalt zahlen",
        live: "Live-Sendungsverfolgung", liveSub: "Updates in Echtzeit",
      },
      product: {
        notFound: "Dieses Produkt gibt es nicht.", vat: "inkl. 19 % MwSt.", inStock: "Auf Lager",
        addToCart: "In den Warenkorb", added: "Zum Warenkorb hinzugefügt", delivery: "Lieferung bis", payment: "Zahlung",
        description: "Beschreibung", moreFrom: "Mehr von {{shop}}",
      },
      cart: {
        title: "Dein Warenkorb", summary: "{{count}} Artikel aus {{shops}} Shops – jeder Shop versendet sein eigenes Paket.",
        empty: "Dein Warenkorb ist leer", continue: "Weiter einkaufen", soldBy: "Verkauft von {{shop}}",
        becomesOrder: "Bestellung {{number}}", remove: "Entfernen", subtotal: "Zwischensumme {{shop}}",
        address: "Lieferadresse", street: "Straße und Hausnummer", postalCode: "PLZ",
        postalCodeHint: "5 Ziffern, z. B. 72764", city: "Ort",
        overview: "Übersicht", items: "{{count}} Artikel", vat: "inkl. 19 % MwSt.", total: "Gesamt",
        placeOrders: "{{count}} Bestellung(en) aufgeben · {{total}}", placing: "Bestellung wird aufgegeben…", loginToOrder: "Zum Bestellen anmelden",
        adminCantBuy: "Admin-Konten können nicht kaufen. Bitte ein Kundenkonto verwenden.",
        nextTitle: "Was nach der Bestellung passiert",
        next1: "Dein Warenkorb wird in eine Bestellung pro Shop aufgeteilt, gemeinsam in einer Transaktion gespeichert.",
        next2: "Jeder Shop wird live benachrichtigt und packt sein Paket.",
        next3: "Du bekommst eine Bestätigung per E-Mail und kannst jede Bestellung live verfolgen.",
      },
      success: {
        title: "Vielen Dank für deine Bestellung!", text: "{{count}} Bestellung(en) aufgegeben. Jeder Shop versendet sein eigenes Paket.",
        order: "Bestellung #{{id}}", email: "Eine Bestätigung per E-Mail ist unterwegs.", track: "Meine Bestellungen verfolgen",
      },
      status: { Pending: "Offen", Processing: "In Bearbeitung", Shipped: "Versendet", Delivered: "Zugestellt", Cancelled: "Storniert" },
      orders: {
        title: "Meine Bestellungen", subtitle: "Statusänderungen erscheinen hier sofort, ohne Neuladen.",
        empty: "Du hast noch nichts bestellt.", placed: "Bestellt am {{date}}",
        history: "Verlauf", product: "Produkt", qty: "Menge", unitPrice: "Einzelpreis", lineTotal: "Summe",
        cancel: "Bestellung stornieren", confirmCancel: "Möchtest du diese Bestellung wirklich stornieren?",
        cancelled: "Bestellung storniert", cancelledBanner: "Diese Bestellung wurde storniert. Die Artikel sind wieder auf Lager.",
      },
      live: {
        live: "Live-Tracking aktiv", connecting: "Verbinde…", reconnecting: "Verbindung wird wiederhergestellt…", offline: "Offline",
        newOrder: "Neue Bestellung #{{id}} eingegangen", statusChanged: "Bestellung #{{id}} ist jetzt {{status}}",
      },
      dashboard: {
        title: "Heute in deinem Shop", morning: "Guten Morgen", day: "Guten Tag", evening: "Guten Abend",
        revenue: "Umsatz", orders: "Bestellungen", avgOrder: "Ø Bestellwert", cancelRate: "Stornoquote",
        last14: "Letzte 14 Tage", revenueChart: "Umsatz der letzten 14 Tage", revenueLabel: "Umsatz",
        byStatus: "Bestellungen nach Status", openBoard: "Bestell-Board öffnen", latest: "Neueste Bestellungen",
        noOrders: "Noch keine Bestellungen.", lowStock: "Niedriger Lagerbestand", stockOk: "Alle Produkte sind gut auf Lager. 👍",
        left: "noch {{count}}", manageProducts: "Produkte verwalten",
      },
      board: {
        title: "Bestell-Board", subtitle: "Ziehe eine Karte, um den Status zu ändern. Nur erlaubte Schritte werden angenommen.",
        moved: "Bestellung #{{id}} → {{status}}", notAllowed: "{{from}} → {{to}} ist nicht erlaubt",
        notHere: "Hier nicht erlaubt", confirmCancel: "Bestellung #{{id}} stornieren? Die Artikel gehen zurück ins Lager.",
        autoCancel: "Wird bald automatisch storniert",
      },
      productsAdmin: {
        title: "Produkte", subtitle: "{{count}} Produkte in deinem Shop", search: "Name oder SKU suchen…",
        new: "Neues Produkt", edit: "Produkt bearbeiten", formHint: "Änderungen sind sofort im Marktplatz sichtbar.",
        name: "Produkt", category: "Kategorie", choose: "Bitte wählen…", price: "Preis", stock: "Bestand", status: "Status",
        visible: "Im Shop sichtbar", visibleHint: "Ausgeblendete Produkte bleiben in alten Bestellungen, sind aber nicht kaufbar.",
        active: "Aktiv", hiddenLabel: "Ausgeblendet", hide: "Ausblenden", show: "Einblenden",
        cancel: "Abbrechen", save: "Speichern", saving: "Wird gespeichert…",
        created: "„{{name}}“ angelegt", updated: "„{{name}}“ gespeichert",
        shown: "„{{name}}“ ist wieder sichtbar", hidden: "„{{name}}“ ist jetzt ausgeblendet",
      },
      common: { comingSoon: "Demnächst verfügbar", darkMode: "Dunkelmodus umschalten" },
      auth: {
        signIn: "Anmelden", signingIn: "Anmeldung läuft…", welcomeBack: "Willkommen zurück. Melde dich an, um deine Bestellungen zu sehen.",
        email: "E-Mail", password: "Passwort", invalid: "E-Mail oder Passwort ist falsch.",
        demo: "Demo-Zugänge", noAccount: "Neu hier?", createAccount: "Konto erstellen", creating: "Konto wird erstellt…",
        haveAccount: "Schon ein Konto?", firstName: "Vorname", lastName: "Nachname",
        loginTitle: "Jede Bestellung, jeder Shop, live verfolgt.",
        loginText: "Eine Plattform für viele Shops. Kunden bestellen, Shops liefern, und alle sehen jede Statusänderung sofort.",
        registerTitle: "Ein Konto für alle Shops.",
        registerAtShop: "Erstelle dein Konto und kaufe bei {{shop}} ein.",
        registerText: "Alle Bestellungen aus allen Shops in einer Liste, mit Live-Tracking.",
        ruleLength: "Mindestens 8 Zeichen", ruleDigit: "Mindestens eine Zahl", ruleUpper: "Mindestens ein Großbuchstabe",
      },
    },
  },
};

// saved choice → otherwise the browser language → otherwise English
const saved = localStorage.getItem("lang");
const browser = navigator.language.startsWith("de") ? "de" : "en";

i18n.use(initReactI18next).init({
  resources,
  lng: saved ?? browser,
  fallbackLng: "en",
  interpolation: { escapeValue: false },
});

// remember the choice + tell the browser
i18n.on("languageChanged", (lng) => {
  localStorage.setItem("lang", lng);
  document.documentElement.lang = lng;
});

export default i18n;