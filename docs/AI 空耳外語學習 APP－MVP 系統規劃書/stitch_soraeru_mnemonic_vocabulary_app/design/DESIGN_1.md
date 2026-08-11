---
name: Soraeru
colors:
  surface: '#f7f9fc'
  surface-dim: '#D8E3E9'
  surface-bright: '#F5FAFD'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f4f6'
  surface-container: '#eceef1'
  surface-container-high: '#e6e8eb'
  surface-container-highest: '#e0e3e5'
  on-surface: '#181c1e'
  on-surface-variant: '#3f484d'
  inverse-surface: '#2d3133'
  inverse-on-surface: '#eff1f3'
  outline: '#70787e'
  outline-variant: '#bfc8cd'
  surface-tint: '#016684'
  primary: '#004d64'
  on-primary: '#ffffff'
  primary-container: '#006684'
  on-primary-container: '#a2e1ff'
  inverse-primary: '#87d0f2'
  secondary: '#4d616c'
  on-secondary: '#ffffff'
  secondary-container: '#d0e6f3'
  on-secondary-container: '#536772'
  tertiary: '#6b3a00'
  on-tertiary: '#ffffff'
  tertiary-container: '#885116'
  on-tertiary-container: '#ffcfa6'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#bee9ff'
  primary-fixed-dim: '#87d0f2'
  on-primary-fixed: '#001f2a'
  on-primary-fixed-variant: '#004d64'
  secondary-fixed: '#d0e6f3'
  secondary-fixed-dim: '#b4cad6'
  on-secondary-fixed: '#081e27'
  on-secondary-fixed-variant: '#364954'
  tertiary-fixed: '#ffdcc0'
  tertiary-fixed-dim: '#ffb876'
  on-tertiary-fixed: '#2d1600'
  on-tertiary-fixed-variant: '#6b3b00'
  background: '#f7f9fc'
  on-background: '#181c1e'
  surface-variant: '#e0e3e5'
  status-warning: '#8B5000'
  status-info: '#0061A4'
  status-danger: '#BA1A1A'
typography:
  display-hero:
    fontFamily: Hanken Grotesk
    fontSize: 40px
    fontWeight: '700'
    lineHeight: 48px
  headline-lg:
    fontFamily: Hanken Grotesk
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
  headline-sm:
    fontFamily: Hanken Grotesk
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
  body-source-lg:
    fontFamily: Noto Sans
    fontSize: 22px
    fontWeight: '400'
    lineHeight: 30px
  body-target-md:
    fontFamily: Noto Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-mono:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
  caption:
    fontFamily: Noto Sans
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  appbar-height: 64px
  bottom-bar-height: 80px
  gutter: 16px
  stack-gap: 12px
  card-padding: 16px
---

## Brand & Style

The design system embodies a **Pragmatic and Supportive** personality, positioning itself as a focused academic utility rather than a gamified or social experience. It prioritizes clarity, user trust, and task-oriented efficiency.

The chosen style is **Corporate / Modern** with a focus on **Functional Minimalism**. It avoids trendy AI-clichés like vibrant purple gradients or memetic imagery in favor of a structured, reliable interface. The visual language uses layered surfaces and subtle depth to create a focused learning atmosphere, ensuring that complex multilingual data (Traditional Chinese, Thai, Japanese, etc.) remains legible and prioritized.

### Design Principles
- **Single-Task Focus:** Layouts are disciplined, typically featuring a single Primary CTA per screen to guide users through the "Entry → Analysis → Result" flow.
- **Safety-First Utility:** Disclaimers regarding AI accuracy and data privacy are treated as first-class UI components, using high-visibility styling to build transparency.
- **Atmospheric Depth:** Instead of flat white backgrounds, the system uses subtle tonal layering to differentiate interactive containers from the application shell.

## Colors

The palette is anchored by a **Deep Teal** primary color, chosen for its association with professional tools and academic reliability. This is contrasted by a **Cool Slate** secondary tone used for auxiliary actions.

- **Primary:** Used for the main CTA on each page (e.g., "Start Analysis," "Save Card").
- **Secondary:** Used for outline buttons and secondary actions like "Google Login" or "Reselect."
- **Surface Strategy:** The app uses `surface-bright` as the base background, with `surface-dim` or layered cards to group content.
- **Functional Tones:** 
    - **Warning (Amber):** High-contrast treatment for mandatory mnemonic disclaimers.
    - **Info (Blue):** Dedicated to privacy notices regarding on-device OCR.
    - **Danger (Red):** Reserved for destructive actions like "Delete Vocabulary."

## Typography

The typography system is designed for **Bilingual Clarity**, supporting complex character sets (Traditional Chinese) alongside various source languages (Thai, Japanese, Roman scripts).

- **Hanken Grotesk:** Used for branding and structural headings. Its sharp, contemporary feel provides a professional "SaaS" aesthetic.
- **Noto Sans:** The primary workhorse for content. It is chosen for its universal character support and neutral readability across different languages.
- **JetBrains Mono:** Used specifically for phonetic strings (Bopomofo/Zhuyin, Romanization) and technical metadata, providing a distinct visual "slot" for pronunciation guides.

**Scaling Note:** On the Analysis Result page, the "Foreign Word" (Source Language) uses `body-source-lg` to ensure it is the most prominent element in the content area.

## Layout & Spacing

The system follows a **Fixed Grid** philosophy optimized for portrait mobile devices (390x844 base). 

- **Structural Zones:** Layouts are divided into a fixed Top AppBar, a fluid scrollable content area, and a Sticky Bottom Bar.
- **Sticky Bottom Bar:** All critical path actions (Primary CTAs) must be placed in this 80px zone to ensure they are always reachable and prominent.
- **Rhythm:** A 4px/8px baseline grid is used. Page margins are set to 16px (`gutter`) to maximize horizontal space for definition text and mnemonic candidates.
- **Reflow:** For tablet views, the content area should be constrained to a max-width of 600px and centered to maintain readability.

## Elevation & Depth

Hierarchy is established through **Tonal Layers** rather than heavy shadows, maintaining a clean, modern aesthetic.

- **Level 0 (Base):** The `surface-bright` background provides a clean foundation.
- **Level 1 (Containers):** Vocabulary cards and input fields use a subtle 1px outline or a slightly darker surface to group information.
- **Level 2 (Interaction):** Floating elements or the Sticky Bottom Bar use a soft, low-opacity ambient shadow (Blur 8px, Y-2px, 10% opacity) to indicate they sit above the scrollable content.
- **Translucency:** The Top AppBar may use a backdrop blur effect when content scrolls beneath it to maintain a sense of space.

## Shapes

The design system uses a **Rounded** shape language (8px / 0.5rem base) to balance professional structure with approachability.

- **Standard (8px):** Primary buttons, input fields, and standard cards.
- **Large (16px):** Large info banners and the bottom navigation container.
- **Pill (Full):** Used exclusively for language filter chips and status badges (e.g., Daily Quota).
- **Interactive States:** Buttons should not change shape on press, but should use a subtle color overlay to indicate interaction.

## Components

### Buttons
- **Primary:** Filled with `primary_color_hex`. High-contrast white text. One per screen maximum.
- **Secondary:** Outlined with `secondary_color_hex`. No fill. Used for "Cancel" or alternative login methods.

### Input Fields
- **Container:** Outlined boxes with 8px roundedness.
- **Character Counter:** Always visible for word inputs (max 50 chars), placed in the bottom right of the field using `label-mono`.

### Banners (Warning/Info)
- **Status Banners:** Full-width or card-style with a left-accent border.
- **Warning Styling:** Uses `status-warning` background with a specific "Caution" icon. These are non-dismissible if they contain legal or AI-accuracy disclaimers.

### Radio Lists (Mnemonic Candidates)
- **Selection:** Use standard Material 3 radio circles.
- **List Items:** Separated by subtle horizontal dividers. Each item contains the mnemonic approximation and the "meaning" in Traditional Chinese.

### AppBar
- **Title:** Centered `headline-sm`.
- **Actions:** Icon buttons (Settings, Back) placed at the extreme edges.

### Cards
- **Vocabulary Card:** Contains the source word, phonetic guide, and a Play (TTS) icon. Uses Level 1 elevation.