<!-- BEGIN:nextjs-agent-rules -->
# This is NOT the Next.js you know

This app runs Next.js 16.2.7 / React 19 — newer than most training data, so APIs, conventions, and file structure may differ from what you expect. Read the relevant guide in `node_modules/next/dist/docs/` before relying on a Next.js pattern you haven't verified against this actual codebase. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

## How this app actually works (verified against the real codebase, not assumed)

- **Every page is a Client Component.** All `page.tsx` files start with `"use client"`. There are no Server Components doing data fetching, no Server Actions, and no Route Handlers under `src/app/api` — this is a client-rendered app built on the App Router purely for routing/bundling. Don't reach for RSC idioms (async page components, `fetch` cache options, `revalidatePath`, etc.) — they don't apply here.
- **The backend is a separate .NET API**, not Next.js. All data goes through `apps/api` via plain `fetch` calls in `src/lib/apiClient.ts`, using `NEXT_PUBLIC_API_URL`. Every request follows the same shape: `fetch(...) → await checkAuth(res) → if (!res.ok) throw new Error(...)`. Add new endpoints there — don't invent a Next.js API route.
- **Auth/route-guarding is client-side only.** There is no `middleware.ts`. Each page protects itself by calling a `useRequireStaff` / `useRequireAdmin` / `useRequireDentist` / `useRequireOwner` hook (`src/hooks/`) at the top of the component.
- **Imports are always relative**, never the `@/*` alias — it's declared in `tsconfig.json` but is not used anywhere in the codebase (verified: 294 relative imports, 0 alias imports). Match that; don't introduce `@/`.
- **No state/data-fetching library.** No SWR, no React Query, no Redux/Zustand. Pages manage their own `useState`/`useEffect`/`useCallback`, typically with a local `reload()` function re-run on mount and after mutations.
- **Styling is Tailwind utility classes written inline** — no CSS modules, no styled-components. Icons are inline `<svg>` elements with a raw path string, not an icon library. Money is always formatted in VND via small local helpers (e.g. `fmt`, `fmtMoneyInput`) — never assume USD/`$`.
- **Role-based folder structure**: `src/app/{admin,staff,dentist,owner}/...`, each with its own Sidebar + PageHeader component under `src/components/shared/`. New role-scoped features belong under the matching folder and should reuse that role's existing Sidebar/PageHeader/guard-hook trio rather than creating new ones.
- **Realtime updates** (e.g. notifications) go through Supabase Realtime (`src/lib/supabaseClient.ts`, `.channel(...).on("postgres_changes", ...)`) where wired up; plain polling (`setInterval`) is used as a fallback elsewhere, not a client-side cache/query library.
- All user-facing text is Vietnamese.

Read the actual current contents of a file before editing it, and don't assume a Next.js/React convention from training data applies here without checking — this codebase's own patterns above take precedence.
