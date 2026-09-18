# DESIGN.MD — QuizApp Design System

> **Agent instruction:** Read this file in full before creating, modifying, or reviewing any CSS, HTML, or visual component in the frontend. The rules here are non-negotiable; they override personal preference and framework defaults.

___

## Identity

**Style name:** Liquid Glass
**Brand palette:** Blue Gradient
**Font:** Be Vietnam Pro (already loaded; do not add a second typeface)

The Liquid Glass style is a liquid glass-morphism design language built on a fixed-position blue gradient background, semi-transparent frosted cards, and a single continuous blue gradient as the primary action colour. Every surface in the UI is a pane of frosted glass floating above the background — never an opaque white box.

___

## Background

The page background is a fixed radial gradient. It is the visual foundation; every glass card must let it show through.

```text
/* from styles.css — do not override or cover with opaque surfaces */
body {
    background:
        radial-gradient(ellipse at 10% 5%,  #bfdbfe 0, transparent 45%),
        radial-gradient(ellipse at 95% 25%, #bae6fd 0, transparent 45%),
        #f4f7fc;
    background-attachment: fixed;
}
```

___

## Brand colour — Blue Gradient

All primary actions, focus rings, badges, and active states use the blue gradient. There is no secondary accent color; do not reintroduce indigo, purple, or violet.

| Token                   | Value                                       | Use                                                    |
|-------------------------|---------------------------------------------|--------------------------------------------------------|
| `--brand`               | `#2563eb`                                   | text, rings, borders                                   |
| `--brand-gradient`      | `linear-gradient(135deg, #1d4ed8, #38bdf8)` | buttons, active states, progress bars                  |
| `--brand-soft`          | `#eff6ff`                                   | badge backgrounds, selected option fill                |
| `--on-brand`            | `#ffffff`                                   | text on top of brand-gradient surfaces                 |
| Tailwind `brand`        | `#2563eb`                                   | utility classes `text-brand`, `bg-brand`, `ring-brand` |
| Tailwind `brand-accent` | `#38bdf8`                                   | icons, accent glows                                    |
| Tailwind `brand-strong` | `#1d4ed8`                                   | dark end of gradient when used as flat colour          |

**Prohibited:** `#4f46e5`, `#6756cd`, `#4338ca`, `#6366f1`, `#7060da`, `#eeebff`, or any Tailwind `indigo-*` / `violet-*` / `purple-*` class used as a brand colour. Replace with the blue equivalent above.

___

## Glass token hierarchy

Four tokens control surface opacity. Apply by layer depth — never repeat the same token on a parent and its direct child.

| Token             | Opacity   | Use                                                                                        |
|-------------------|-----------|--------------------------------------------------------------------------------------------|
| `--glass-raised`  | 20% white | sticky nav bar, modal dialogs, toast notifications                                         |
| `--glass-surface` | 15% white | top-level page cards (overview, quiz card, CTA, auth card)                                 |
| `--glass-inset`   | 5% white  | child elements inside a surface card (metric grid, answer option, list item, keyboard tip) |
| `--glass-hover`   | 10% white | hover / pressed state for any inset or surface element                                     |

Every glass surface must carry:

```text
backdrop-filter: var(--glass-filter);          /* blur(20px) saturate(145%) */
-webkit-backdrop-filter: var(--glass-filter);
border: 1px solid var(--glass-border);         /* rgba(255,255,255,0.65) */
box-shadow: var(--glass-shadow);
border-radius: var(--glass-radius);            /* 24px for cards; smaller for child elements */
```

**Prohibited:**
- `background: #ffffff` or any opaque white.
- Opacity above 30% white on any surface token.
- Nesting `--glass-surface` inside another `--glass-surface` — use `--glass-inset` for child cards.
- Setting `opacity` on the whole component — changes only `background` opacity via the tokens.
- Adding `backdrop-filter` on a child already inside a blurred parent.

___

## Blur fallback

```text
@supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
    :root {
        --glass-surface: #f0f4fc;
        --glass-raised:  #f5f8ff;
        --glass-inset:   #e8edf8;
        --glass-hover:   #dce3f2;
    }
}
```

Do not remove this block.

___

## Component layer map

```
Page background (fixed blue gradient)
└── sticky nav                          glass-raised  + blur
└── page card / auth card               glass-surface + blur
    └── metric grid / answer option     glass-inset   (no extra blur)
    └── child list item (hover)         glass-hover
└── modal / dialog                      glass-raised  + blur
└── toast                               glass-raised  + blur
```

___

## Primary action button

```text
.primary-action {
    background: var(--brand-gradient);
    border: 1px solid var(--glass-border);
    color: var(--on-brand);
    box-shadow: var(--glass-shadow);
}
.primary-action:hover:not(:disabled) { filter: brightness(1.08); }
.primary-action:active               { transform: scale(0.98); }
```

Secondary / ghost buttons: `--glass-inset` background + `--ink` text, switch to `--glass-hover` on hover.

___

## Focus and accessibility

- Focus ring: `outline: 2px solid var(--brand); outline-offset: 4px` (global, in `styles.css`).
- Minimum contrast body text: **4.5 : 1**.
- Respect `prefers-reduced-motion` — already handled globally; do not bypass it.
- Every `<dialog>`: `max-height: calc(100dvh - 32px); overflow-y: auto`.

___

## Typography

| Role               | Value                                                |
|--------------------|------------------------------------------------------|
| Typeface           | `Be Vietnam Pro` via `font-sans`                     |
| Page heading h1    | `text-2xl font-bold tracking-tight text-ink` minimum |
| Section heading h2 | `text-xl font-semibold text-ink`                     |
| Body               | `text-sm leading-6 text-muted`                       |
| Label / meta       | `text-xs font-medium text-muted`                     |
| On-brand text      | `text-on-brand` (white)                              |

Do not add a font import; Be Vietnam Pro is already declared in `index.html`.

___

## Motion

Card transition: `transition-all duration-200`. Maximum hover lift: `translateY(-4px)`. Do not exceed `duration-300`.

___

## What agents must not change

- Semantic tokens: `--ink`, `--muted`, `--canvas`, `--on-brand`, `--success`, `--warning`, `--error`.
- Structural glass tokens: `--glass-filter`, `--glass-border`, `--glass-radius`, `--glass-shadow`.
- API contracts, DTOs, routes, or TypeScript business logic.
- English UI text, `aria-*` attributes, `role`, or tab order.
- The `Be Vietnam Pro` font declaration.
