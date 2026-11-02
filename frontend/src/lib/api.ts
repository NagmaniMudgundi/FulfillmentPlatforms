// One place for every call to the backend: base URL, token, errors, JSON.
const API_URL = import.meta.env.VITE_API_URL as string;

// An error that knows the HTTP status (401, 404, 409 …), so pages can react to it
export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

// The token of the logged-in user (set after login in 10.4)
let authToken: string | null = null;

export function setAuthToken(token: string | null) {
  authToken = token;
}
// called when the API says 401 to a logged-in user (token expired) → AuthContext logs out
let onUnauthorized: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler;
}
export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);

  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (authToken) {
    headers.set("Authorization", `Bearer ${authToken}`);
  }

  const response = await fetch(`${API_URL}${path}`, { ...options, headers });

    if (!response.ok) {
    // a logged-in user got 401 → the token is no longer valid
    if (response.status === 401 && authToken && onUnauthorized) {
      onUnauthorized();
    }

    let message = `Request failed (${response.status})`;
    try {
      const problem = await response.json();
      // validation errors (400): { errors: { Password: ["…"], Email: ["…"] } }
      if (problem.errors) {
        message = Object.values(problem.errors as Record<string, string[]>).flat().join(" ");
      } else {
        message = problem.detail ?? problem.title ?? message;
      }
    } catch {
      // no JSON body: keep the default message
    }
    throw new ApiError(response.status, message);
  }

  if (response.status === 204) {
    return undefined as T;               // "No Content": nothing to read
  }
  return (await response.json()) as T;
}