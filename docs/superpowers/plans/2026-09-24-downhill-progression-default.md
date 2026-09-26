# Default downhill progression driving

User asks for Initial D / downhill as the basic racing style. Existing downhill driving, coupe visual, acceleration and braking already exist; progression currently overrides that default with a locked kart/coupe choice.

- [x] Fresh progression selects numeric style 1 (Downhill) and can always use it. Keep numeric IDs 0=Kart, 1=Downhill. Existing `coupe` node becomes optional classic Kart unlock with its same ID/cost/parents, so no paid trait is left without an effect. Centralize availability in `Progression.HasCartStyle` and use it in controller/UI/save validation. Put downhill first in vehicle controls and rename generic vehicle labels.
- [x] Save V8 migrates valid old implicit kart default to downhill while retaining available explicitly selected styles. Validate old V7 rules before migration; preserve economy, purchases, phase, ingredients and clock. Keep optional-state presence handling and corruption rejection.
- [x] Run core and save regressions plus actual player checks for fresh downhill, coupe visual, classic kart unlock and switching, machine switching and save/reload. Inspect fresh business capture, build updated Release and record results.

No new physics or art is required. Prior user delegation authorizes these implementation decisions; preserve unrelated checkout changes.
