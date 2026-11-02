import { useEffect, useState } from "react";

type Theme = "light" | "dark";

// saved choice → otherwise the computer's setting
function initialTheme(): Theme {
  const saved = localStorage.getItem("theme");
  if (saved === "light" || saved === "dark") return saved;
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

// A custom hook: any component can use dark mode with ONE line
export function useTheme() {
  const [theme, setTheme] = useState<Theme>(initialTheme);

  // whenever the theme changes: switch the class on <html> + remember it
  useEffect(() => {
    document.documentElement.classList.toggle("dark", theme === "dark");
    localStorage.setItem("theme", theme);
  }, [theme]);

  const toggleTheme = () => setTheme((t) => (t === "dark" ? "light" : "dark"));
  return { theme, toggleTheme };
}