# ADR-0011: spartan-ng components owned in the repository, on Tailwind CSS

- Status: Accepted
- Date: 2026-10-10

## Decision

- UI components come from spartan-ng: unstyled, accessible "brain" primitives from npm, and "helm"
  styling that is generated into `libs/shared/ui` and owned by the repository (the shadcn model).
  They are imported as `@spartan-ng/helm/<component>`.
- Tailwind CSS 4 provides the design tokens (`:root` and `.dark` variables in `styles.css`, neutral
  theme); lucide icons come through `@ng-icons`.
- No Angular Material.
- The library is tagged `type:ui, scope:shared`, so feature code may use it but it may not depend on
  features.

## Consequences

- Components can be changed freely, but updates arrive through spartan's migration generators
  instead of a version bump.
- Tailwind emits one static stylesheet, which the BFF serves with the per-request nonce; the browser
  journeys fail on any CSP violation, so styling changes cannot weaken the policy unnoticed. A
  browser test of the production policy is still missing (threat model G5).
