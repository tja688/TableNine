---
name: ai-game-scene-assembler
description: use when the user is developing an indie game with ai and wants help turning rough micro-prototypes, visual interaction slices, prefab demos, ui demos, card interaction tests, or other small gameplay feel experiments into a coherent playable scene, vertical slice, or production-ready flow. trigger when the user provides fragmented prototype code/assets plus a larger scene goal, asks for ai-assisted scene assembly, asks to infer missing interaction details, or needs safeguards around rough prototype entry/exit behavior.
---

# AI Game Scene Assembler

## Core idea

Treat the user's small prototypes as **evidence of desired feel**, not as final product specifications. The user often builds micro-slices quickly to validate a visible effect, animation, UI moment, card interaction, collision response, or game-feel detail. These slices may contain temporary shortcuts, fake entry/exit triggers, debug interactions, hardcoded data, or loose state handling.

Your job is to preserve the validated experience while assembling it into a coherent scene from the perspective of the final game flow.

## Default workflow

1. Identify the larger scene goal: what the player is supposed to experience from scene entry to scene exit.
2. Inventory the provided micro-prototypes: what each one proves visually, interactively, or technically.
3. Separate reliable intent from prototype shortcuts.
4. Define a scene-level interaction contract before implementing or editing code.
5. Add orchestration, adapters, state flow, event wiring, and lifecycle handling around the prototypes.
6. Preserve the feel of validated effects unless the user explicitly asks to redesign them.
7. Replace temporary/debug entry and exit behavior with reasonable production-context behavior.

## Interpret micro-prototypes correctly

Assume the following parts of a rough prototype are usually meaningful:

- visual timing, animation curves, movement style, layout rhythm, easing, impact, juice, and perceived feel
- the visible relationship between objects, such as card hover, drag, snap, collision, popup, floating text, or feedback timing
- component names or file organization when they reveal how the user thinks about the feature
- event moments implied by the prototype, such as "card placed", "popup shown", "damage resolved", or "effect complete"

Assume the following parts are often temporary unless the user says otherwise:

- clicking anywhere outside to close, skip, reset, or exit
- keyboard shortcuts, debug buttons, temporary mouse-only controls, forced state jumps
- hardcoded card data, enemy data, timers, mock values, fake win/loss conditions
- permissive colliders, broad hit areas, placeholder text, placeholder art, placeholder audio
- components that both display an effect and control the whole scene state
- global singletons or direct object lookups used only to make the prototype run quickly

## Entry/exit fallback rules

When the prototype's trigger or exit is informal, infer a more formal game-scene behavior. Make the assumption visible, but do not stall unless the ambiguity blocks safe implementation.

Use these defaults:

- Modal UI exits by an explicit close/confirm/cancel button. Outside-click close is optional and secondary, not the main production path.
- Result, reward, or summary UI exits through a continue/confirm button that advances the scene state.
- Tutorial or hint UI exits through next/skip/confirm, or auto-dismisses only when it is clearly non-blocking.
- Card drag starts from pointer/touch press, previews valid targets while dragging, commits on release over a valid target, and returns to origin on invalid release.
- Card placement should emit a clear event such as `OnCardPlaced`, `CardPlayed`, or equivalent instead of directly resolving the whole battle inside the visual component.
- Collision, hit, damage, juice, and floating text effects should be event-driven and expose completion callbacks when the next scene step depends on them.
- Scene transitions and round/turn advancement should be owned by the scene orchestrator or state machine, not by an isolated visual prototype.
- Temporary debug reset/exit logic should be removed, hidden behind development flags, or replaced with the final scene control.

## Assembly strategy

Prefer **adapter and orchestration code** over rewriting working prototypes.

- Keep micro-prototypes as reusable presentation components when possible.
- Add thin adapters if their public interface does not match the scene flow.
- Use a scene controller, state machine, timeline, command queue, or event bus to coordinate sequence and ownership.
- Avoid letting a visual effect component become the authority for battle rules, economy, progression, or navigation.
- Preserve local feel code; normalize lifecycle, input routing, and state ownership at the scene level.
- Where code quality is rough but the feel is good, isolate roughness behind a stable interface before deeper refactoring.
- If refactoring is necessary, first state which behavior must remain visually identical.

## Scene-level checklist

Before finalizing an implementation, check:

- What starts this interaction in the final scene?
- What ends it in the final scene?
- Who owns the state transition before and after it?
- Is the prototype showing final UX, or only a debug path to preview the effect?
- Does the player have a clear affordance: button, drag target, confirm, cancel, next, continue, or back?
- Are invalid actions handled gracefully?
- Are events unsubscribed and temporary objects cleaned up?
- Can the same component be reused in another scene without carrying debug-only assumptions?

## Response pattern

When helping the user assemble a scene from rough slices, use this compact structure unless the user asks for pure code:

1. **Understanding**: summarize the intended final scene flow.
2. **Keep as-is**: list prototype behaviors to preserve.
3. **Normalize for production flow**: list shortcuts or ambiguous entry/exit behavior to formalize.
4. **Assembly plan**: describe orchestration/state/event wiring.
5. **Implementation**: provide code, file edits, or concrete next steps.
6. **Assumptions**: call out any inferred button/exit/state behavior.

## Example interpretation

If the user says: "this popup prototype currently closes when I click outside, but in the real scene it should probably be part of reward selection," interpret it as:

- Preserve: popup animation, visual layout, reveal timing, reward item hover/selection feel.
- Do not preserve by default: outside-click as the primary close mechanism.
- Production fallback: use a confirm/select button, emit `RewardSelected`, then let the scene controller advance.

If the user only provides the prototype and forgets to specify the exit behavior, infer the same production fallback and mention it as an assumption.
