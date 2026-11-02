/// <reference types="vite/client" />

// our own .env variables, typed (so VITE_API_URL gets autocomplete and can't be misspelled)
interface ImportMetaEnv {
  readonly VITE_API_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}