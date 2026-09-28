# Customer/guest arrival models in tycoon games, and design theory on randomizing spawn timing

Scope: (A) how larger tycoon/management sims generate customers, from open-source code where possible; (B) what design literature says about fixed intervals, jitter, Poisson arrivals, pseudo-random/shuffle-bag methods, rush curves, and seeded determinism; and (C) what this means for a 180-second cotton-candy shop day with 3 slots, a base interval of about 26 s (about 7 customers), about 90 s of customer patience, "ads" upgrades that shorten the interval, and saves that must replay deterministically.

Numbers labelled **(computed)** are my own calculations, not sources. They come from closed-form Poisson/exponential formulas plus a Monte Carlo script: 200,000 simulated 180-s days, Python `random.seed(1)`. For renewal processes (fixed/jittered gaps) the first arrival comes one gap after t=0, so their mean counts are about 6.4–6.9. A fixed 26 s timer that starts with a customer at t=0 gives exactly 7 (0, 26, …, 156 s). Poisson counts are exact.

---

## Q1. Which tycoon games use probabilistic per-tick spawning vs interval timers, and what drives the rate?

### Takeaway
The two big open-source references use two different models. RollerCoaster Tycoon (OpenRCT2) runs a memoryless per-tick Bernoulli roll, which approximates a Poisson process. Its probability comes from park rating and is cut by soft caps: guest count over a "suggested maximum", overpriced entry, and awards. Marketing campaigns add independent extra rolls. Theme Hospital (CorsixTH) plans each month's arrivals in advance: a monthly quota (level-scripted rate × reputation multiplier) is spread over the month with Poisson-distributed day gaps, then each patient gets a random hour. Neither game uses a plain fixed-interval timer. Both link "attractiveness" (rating or reputation) to the arrival rate multiplicatively, and both add negative feedback so the rate cannot run away.

### Cited Findings

**RollerCoaster Tycoon 2 / OpenRCT2 (primary source: C++ code)**
- The logic runs at a fixed 40 ticks/s: `constexpr uint32_t kGameUpdateFPS = 40;`, and a comment gives the update interval as "(1000 / 40fps) = 25ms". — [OpenRCT2 Game.h](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/Game.h)
- `generateGuests(park, gameState);` runs every tick with no condition. Park rating, suggested guest maximum and guest generation probability are recalculated only `if (currentTicks % 512 == 0)`, which the code comments as "Every ~13 seconds". — [OpenRCT2 Park.cpp](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/world/Park.cpp)
- Per-tick spawn test: `if (static_cast<int32_t>(ScenarioRand() & 0xFFFF) < park.guestGenerationProbability)`, which is probability/65536 per tick. With the "difficult guest generation" flag, a guest is created only if `park.suggestedGuestMaximum + 150 >= park.numGuestsInPark`. — [OpenRCT2 Park.cpp](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/world/Park.cpp)
- Probability formula: `uint32_t probability = 50 + std::clamp(park.rating - 200, 0, 650);` (comment: "Begin with 50 + park rating"). So the value runs from 50 at rating ≤200 to 700 at rating ≥850. The modifiers are:
  - probability /4 if guests in park plus guests heading there exceed `suggestedGuestMaximum`, and another /4 under difficult generation;
  - /4 above 52,000 guests;
  - /4 if the entrance fee exceeds `totalRideValueForMoney`, and another /4 if half the fee still exceeds it;
  - ±probability/4 for each positive or negative award. The code comment says "+/- 0.25%", but the code does ±25%. — [OpenRCT2 Park.cpp](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/world/Park.cpp)
- Suggested max guests: the sum of each open, non-broken ride type's `BonusValue`. Under difficult generation it is capped at 1000, plus a bonus of 2×BonusValue for each tested tracked ride with length ≥600 and excitement ≥6.00. The hard cap is 65535. — [OpenRCT2 Park.cpp](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/world/Park.cpp)
- Marketing: every active campaign gets its own independent per-tick roll, `ScenarioRandMax(uint16 max) < probability`, which creates an extra guest tagged with that campaign. Base probabilities are `{ 400, 300, 200, 200, 250, 200 }` for the six campaign types. Free-entry and half-price-entry campaigns are divided by 8 if the entrance fee is below £4.00 or £6.00; the free-ride campaign is divided by 8 if the ride price is below £0.30. Weekly costs are `{ 50, 50, 50, 50, 350, 200 }` GBP. — [OpenRCT2 Marketing.cpp](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/management/Marketing.cpp); generation loop in [Park.cpp](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/world/Park.cpp)
- All of this uses `ScenarioRand()`, the shared simulation RNG. OpenRCT2 multiplayer detects desync partly by checking that each client's RNG state matches the server's. PRs fixed desyncs where code used the non-simulation `UtilRand` or advanced `ScenarioRand` only on one client during a track-design preview. — [OpenRCT2 PR #26605](https://github.com/OpenRCT2/OpenRCT2/pull/26605); [OpenRCT2 PR #27134](https://github.com/OpenRCT2/OpenRCT2/pull/27134); [DeepWiki: Game Synchronization](https://deepwiki.com/OpenRCT2/OpenRCT2/6.2-game-synchronization). These are search-result summaries of the PRs; the PR pages were not fetched.

**Theme Hospital / CorsixTH (primary source: Lua code and level files)**
- Monthly quota: `local no_of_spawns = self.spawn_rate * local_hospital.population`. `spawn_rate` starts at `level_config.popn[0].Change`. Each month `self.spawn_rate = self.spawn_rate + self.monthly_spawn_increase`, where the increase is the `Change` value of the most recent `popn[i]` entry for that month. — [CorsixTH world.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/world.lua)
- Level data example (St. Peter's): `#popn[0].Month.Change 0 2`, `#popn[1] 6 0`, `#popn[2] 12 2`, `#popn[4] 15 0`, `#popn[5] 23 1`, `#popn[6] 28 0`. The rate grows by 2 per month in months 0–5, pauses from month 6, grows again from month 12, and so on. Finisham also uses negative changes, for example `#popn[3].Month.Change 21 -58` and `#popn[12] 57 -90`. — [st.peter's.level](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Levels/st.peter's.level); [finisham.level](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Levels/finisham.level)
- `hospital.population = 0.25`. A code comment calls it the share "of the total population that goes to the player's hospital", with a TODO to change it "when competitors are there". Each month end resets it to 0.25; from month `gbv.AllocDelay` onward it is multiplied by `getReputationImpact`. — [CorsixTH hospital.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/hospital.lua); [world.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/world.lua)
- Reputation multiplier: `1 + ((hospital.reputation - 500) / 250)`, floored at 0.01. That gives ×1 at reputation 500 and ×3 at 1000, falling to the 0.01 floor at 250 and below. — [CorsixTH world.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/world.lua)
- Scheduling: `updateSpawnDates()` sets `interval = last_day / no_of_spawns` and loops `day = day + math.p_random(interval)`, adding one spawn on each resulting day ≤ `last_day`. If no spawn landed, `force_arrival` puts one patient on a random day. The next day's patients are each given `math.random(1, Date.hoursPerDay())` as their hour, and the tick loop spawns `spawn_hours[hour]` patients. — [CorsixTH world.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/world.lua)
- `math.p_random(mean)` draws a Poisson-distributed integer. It is Knuth's multiplicative algorithm with a `STEP = 500` extension for large means, and its comment links Wikipedia's Poisson distribution. — [CorsixTH app.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/app.lua)

**Parkitect**
- A community guide citing `Park.calculateMaxGuestCount` says each ride attracts `excitement × 0.8 × (0.8 + 0.2 × satisfaction)` guests and each shop up to 5 more. Park rating acts "roughly as a multiplier (~half the guests at 50% rating), but can't drop below 15%". Duplicate rides are penalised: −10% at 4 copies, −19% at 5, −25% at 6, −30% at 7, −33% at 8 or more. The guide does not document spawn-interval timing. — [YAL-Game-Things Parkitect guide, Metrics](https://github.com/YAL-Game-Things/Parkitect-guide/blob/main/0300-Metrics.md)
- The fan wiki (search snippet) says guests start at spawn points outside the park, path randomly to the gate, and then decide whether to enter based at least on entry cost and total excitement. — [Parkitect Wiki: Guests](https://parkitect.fandom.com/wiki/Guests)

**Two Point Hospital**
- The wiki (search snippet) says overall reputation affects "the rate at which patients arrive". — [Two Point Hospital Wiki: Overview](https://two-point-hospital.fandom.com/wiki/Overview)
- A player (Freiya, not a developer) says the patient "cap" depends on reputation, hospital level and a hidden per-mission modifier. That modifier is exposed in sandbox mode as "Patient Arrival Rate" (for example ×2). About 250 concurrent patients is the practical campaign maximum at hospital level 30 with max reputation. — [Steam discussion](https://steamcommunity.com/app/535930/discussions/0/1735469327937533890/)

**Design-level precedent from outside tycoons (arrival-like spawns)**
- Left 4 Dead mobs spawn "at randomized intervals (90-180 seconds on Normal difficulty)". Boomer vomit "forces Mob spawn, resets random interval". Mob size grows from a minimum just after one spawn to a maximum over time "to balance difficulty of successive, frequent Mobs". — [Booth, "The AI Systems of Left 4 Dead", Valve 2009](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf)

### Inferences
- **RCT arrival rates (computed from the code):** per-tick p = probability/65536 at 40 ticks/s.
  - Rating ≥850 (700): about 0.427 guests/s, 25.6/min, mean gap about 2.3 s.
  - Rating about 500 (350): about 0.214/s, 12.8/min.
  - Rating ≤200 (50): about 0.031/s, 1.8/min.
  - A free-entry campaign (400) adds about 0.244/s, 14.6/min.

  A Bernoulli trial with very small p every 25 ms is effectively a Poisson process, with exponential gaps and memoryless clumping. Clumping does not matter at RCT's scale of hundreds of guests, but it would at 7 customers.
- **Rating feedback in RCT:** rating feeds the rate through a clamped linear term, and overcrowding or overpricing divide it by powers of 4. The designers controlled the count with multiplicative rate modifiers rather than scheduled times. Marketing is additive: separate independent rolls, not a shorter base interval.
- **CorsixTH plans a month ahead.** Using a Poisson-distributed day gap (variance = mean) instead of an exponential gap (SD = mean) gives less clumping than a true Poisson process when the mean gap is several days. Example (computed): 6 spawns in a 30-day month gives a mean gap of 5 days, SD √5 ≈ 2.24 days (CV about 45%, against 100% for exponential). A gap draw of 0 puts two patients on one day, and the random hour then spreads them within it. Precomputing a schedule is the model closest to a "deterministic day schedule" for the cotton-candy game.
- The CorsixTH schedule (`spawn_dates`) is stored on the World object, which probably survives save/load. I did not verify the serialisation code.

### Gaps
- **Planet Coaster:** I found no primary or official documentation of its guest spawn algorithm.
- **Two Point Hospital/Campus:** no developer-published arrival formula; only wiki and player statements.
- **Recettear:** the wiki pages that might describe customer arrival order and timing ("Order of Events", "Customer Reputation") returned HTTP 402 and could not be read. Search snippets only confirm a day split into fixed periods and customer budgets scaled by relationship. — [Wikipedia: Recettear](https://en.wikipedia.org/wiki/Recettear:_An_Item_Shop's_Tale); [Recettear Wiki: Customer Reputation](https://recettear.fandom.com/wiki/Customer_Reputation)
- **Moonlighter:** search found only pricing/reaction mechanics ([Moonlighter Wiki: Selling and Reactions](https://moonlighter.fandom.com/wiki/Selling_and_Reactions)), nothing on arrival rate or interval.
- **SimTower / Mad Games Tycoon:** not researched, as allowed by the brief.
- **Parkitect:** spawn timing (per-tick vs timer) remains undocumented.

---

## Q2. What do designers and design literature say about fixed vs random timing for feel, fairness and predictability?

### Takeaway
The consensus from primary talks and articles is "structured unpredictability". Pure uniform timing is monotonous, but pure memoryless randomness produces streaks and droughts that players read as unfair or broken, because players judge probability poorly. The standard fixes are:
- bounded jitter (L4D: 90–180 s);
- rising-hazard pseudo-random distribution (Dota PRD, WoW quest drops);
- sampling without replacement (shuffle bag, Tetris 7-bag);
- an intensity curve with explicit rest periods (L4D Director, Lopez's peaks and troughs).

Determinism needs one dedicated seeded simulation RNG that only simulation code advances (OpenRCT2 `ScenarioRand`).

### Cited Findings

**Structured unpredictability and pacing**
- Booth (Valve, 2009) describes population design as "Not purely random, nor deterministically uniform", with "designer-defined amount of randomization". His example is mob spawns at a "randomized interval between 90 and 180 seconds", and he calls the superposition of several such functions "Structured Unpredictability". — [Booth 2009](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf)
- Booth's pacing rationale, drawn from Counter-Strike: "Constant, unchanging combat is fatiguing"; "Long periods of inactivity are boring"; "Unpredictable peaks and valleys of intensity create a powerfully compelling and replayable experience". — [Booth 2009](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf)
- The Adaptive Dramatic Pacing loop:
  - Survivor Intensity rises with damage taken, incapacitation, and nearby Infected deaths (inversely with distance). It decays toward zero over time, but not while Infected are engaging.
  - Phases: **Build Up** (full threat population until intensity peaks), **Sustain Peak** ("3-5 seconds" after the peak), **Peak Fade** (minimal population until intensity decays), **Relax** ("30-45 seconds", or until Survivors have "traveled far enough toward the next safe room"), then Build Up again.
  - "Algorithm adjusts pacing, not difficulty … frequency (pacing) is [changed]". "Survivor Intensity estimation is crude, yet the resulting pacing works."
  - Boss encounters are excluded from adaptive pacing.
  — [Booth 2009](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf)
- Mike Lopez (ex-EA/THQ design director, Nov 12, 2008) says pacing should alternate peaks and troughs, not escalate monotonically: "the contrast between the peaceful calm and the intense action will punctuate and maximize the impact". Peaks should grow in intensity and ideally come at shorter intervals later on. Matching peak magnitude and timing captures most of the intended experience, while troughs need less precision. — [Game Developer: Harnessed Pacing & Intensity](https://www.gamedeveloper.com/design/gameplay-fundamentals-revisited-harnessed-pacing-intensity)

**Player perception of probability and fairness**
- Sid Meier (GDC 2010) said Civilization Revolution testers facing 3-to-1 odds "expect to win this battle every time despite there being a 25% chance of losing each time". He changed the math to account for losing streaks instead of teaching statistics. — [Shacknews, Mar 15 2010](https://www.shacknews.com/article/62807/sid-meier-and-rob-pardo); talk: [GDC Vault, The Psychology of Game Design](https://gdcvault.com/play/1012186/The-Psychology-of-Game-Design)
- Rob Pardo (Blizzard, GDC 2010): WoW quest drops "increased the drop rate after each kill until it hits 100% and then reset it", so players would not conclude the RNG was broken. This is a rising-hazard scheme. — [Shacknews, Mar 15 2010](https://www.shacknews.com/article/62807/sid-meier-and-rob-pardo)

**Pseudo-random distribution (PRD)**
- Dota 2 PRD: the chance on the N-th test since the last proc is P(N) = C·N. C is lower than the nominal chance, and the event becomes certain once C·N ≥ 1. The constants come from original WC3 DotA data. — [Dota 2 Wiki: Random Distribution](https://dota2.fandom.com/wiki/Random_Distribution) (search snippet; the page returned HTTP 402)
- For a 25% nominal proc, c ≈ 0.0847: about 8.5% on the first hit, 17% on the second, 25.5% on the third. PRD reduces repeat procs, guarantees a proc by trial 1/c, and stops players "priming" the effect. — [Diplograph: Dota 2 PRD](https://diplograph.net/notes/games/randomness/dota-2-prd)

**Shuffle bag / 7-bag**
- The Tetris Random Generator deals all seven tetrominoes in a random order (7! = 5,040 permutations per bag), then refills. It allows "no more than 12 tetrominoes between one I piece and the next" and S/Z runs of at most 4. The Guideline gives the rationale as limiting "droughts". It started in The New Tetris with a 63-piece bag (9 of each piece). — [TetrisWiki: Random Generator](https://tetris.wiki/Random_Generator)
- Jon Davis (Envato Tuts+, Oct 24 2012) describes a shuffle bag as: fill it with values in the desired proportions, shuffle, draw each one, refill. His rationale: "Random.Next() doesn't guarantee a nice even distribution … in many gameplay situations, Random() isn't fun." — [Tuts+: Shuffle Bags](https://code.tutsplus.com/shuffle-bags-making-random-feel-more-random--gamedev-1249t)

**Determinism**
- OpenRCT2 keeps a simulation RNG (`ScenarioRand`) separate from a utility RNG (`UtilRand`). Desync bugs arose when gameplay-affecting code used `UtilRand`, or when UI-only code (track-design preview) advanced `ScenarioRand` on one client only. — [OpenRCT2 PR #26605](https://github.com/OpenRCT2/OpenRCT2/pull/26605); [PR #27134](https://github.com/OpenRCT2/OpenRCT2/pull/27134) (search summaries)

### Inferences
- **PRD constants (computed; my 25% value matches Diplograph's 0.0847):**

  | Nominal chance | C | Guaranteed by trial |
  |---|---|---|
  | 10% | 0.01475 | 68 |
  | 15% | 0.03222 | 32 |
  | 20% | 0.05570 | 18 |
  | 25% | 0.08474 | 12 |
  | 30% | 0.11895 | 9 |

  Applied to time (hazard per second = C·t), a 26 s mean gap needs C ≈ 0.00227/s. The gap can then never exceed about 442 s, and the day-level spread is much tighter than Poisson (see Q3).
- **Three families of alternatives to pure random**, and what each bounds:
  - Hazard-based PRD and Pardo's drop ramp bound a gap from above but leave the lower end fairly free.
  - Bags/stratification guarantee the total count per bag or day.
  - Bounded jitter (L4D) bounds both ends.

  For "a customer should never take forever, and the day should not swing from 4 to 11 customers", bounded jitter or stratification fits better than PRD.
- L4D's 90–180 s mob interval is a uniform ±33% jitter around 135 s. That is direct industry precedent for "base ± x%" rather than exponential gaps for events that set the player's workload.
- Lopez and Booth both argue that rest periods matter as much as peaks. For a time-management shop, some "lull" after a cluster fills all 3 slots is desirable, but it should be designed, not left to exponential droughts.

### Gaps
- I did not find a Red Blob Games or Game Programming Patterns article specifically on spawn timing or Poisson arrivals. Red Blob's probability article is about damage rolls, and I did not fetch it.
- I found no GDC talk or paper specifically on customer arrival distributions in tycoon or time-management games. Evidence is indirect: L4D pacing, PRD, bags, and Meier/Pardo on perception.
- The academic "queueing in games" literature was not reached within the tool budget. Only a standard queueing result (Erlang B, computed below) is used.

---

## Q3. What concrete recommendations fit a 180-second day with about 7 customers (3 slots, about 90 s patience, ads shorten the interval, deterministic replay)?

### Takeaway
At about 7 events per day, a pure Poisson or exponential process is too swingy:
- 18% of days bring ≤4 customers and 16% bring ≥10;
- about half of days contain a 60-s-or-longer gap;
- about half contain 3 arrivals within 20 s.

A bounded-jitter timer (base ±30%, uniform or triangular), or a stratified or bag schedule with a minimum-gap clamp, keeps the count at about 6–8. It still varies moment-to-moment feel, and it matches the "structured unpredictability" precedent. Generate the whole day's arrival schedule at day start from a dedicated seeded RNG (as CorsixTH precomputes a month), so saves and replays are exact. Add rush feel with a designed intensity multiplier, not with variance.

### Cited Findings
- CorsixTH precomputes the whole month's `spawn_dates` at month start, then assigns hours day by day, rather than rolling each tick. — [CorsixTH world.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/world.lua)
- CorsixTH forces at least one arrival per month if the random schedule produced none (`force_arrival`). This is a guard against a zero-arrival period. — [CorsixTH world.lua](https://github.com/CorsixTH/CorsixTH/blob/master/CorsixTH/Lua/world.lua)
- L4D uses bounded uniform intervals (90–180 s) for workload-setting spawns and explicit 30–45 s Relax windows. — [Booth 2009](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf)
- RCT runs spawn rolls on fixed-rate logic ticks (40/s) with a shared simulation RNG, not per rendered frame. — [OpenRCT2 Game.h](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/Game.h); [Park.cpp](https://github.com/OpenRCT2/OpenRCT2/blob/develop/src/openrct2/world/Park.cpp)

### Inferences

**Pure Poisson at a 26 s mean over 180 s (computed, closed form)**
- Expected count λ = 6.92, SD = 2.63 (CV 38%).
- Count probabilities: P(N≤3) = 8.6%; P(N≤4) = 18.0%; P(N≤5) = 31.1%; P(N≥10) = 16.2%; P(N≥11) = 9.3%. Only 65.8% of days land in 5–9.
- Gaps: the median gap is only 18.0 s (26·ln 2), because most gaps are short and a few are long.
  - P(gap < 5 s) = 17.5%; P(gap < 10 s) = 31.9%.
  - P(gap > 52 s) = 13.5%; P(gap > 60 s) = 9.9%; P(gap > 78 s) = 5.0%; P(gap > 90 s) = 3.1%.
- The chance of no customer in the first 30 s is 31.5%.
- "Ads" at a 20 s mean give 9.0 ± 3.0 customers: P(≤5) = 11.6%, P(≥13) = 12.4%. Poisson variance grows with the rate, so upgrades make days more erratic in absolute terms.

**Monte Carlo comparison (computed, 200k days).** Columns: mean ± SD of daily count, observed range, P(≤4 customers), P(≥10), P(day contains a ≥60 s gap, including open or close edges), P(3 arrivals within 20 s), median of the day's shortest gap, median of the day's longest gap.

| Model | Count | Range | ≤4 | ≥10 | ≥60 s gap | 3 in 20 s | Shortest gap (median) | Longest gap (median) |
|---|---|---|---|---|---|---|---|---|
| Exponential (Poisson) | 6.93±2.63 | 0–22 | 18.0% | 16.3% | 50.9% | 54.0% | 3.1 s | 60.4 s |
| Exponential + 8 s min gap (mean kept 26) | 6.66±1.80 | 0–14 | 11.3% | 5.6% | 28.4% | 10.6% | 10.2 s | 49.6 s |
| Gamma k=4 (Erlang-4), mean 26 | 6.55±1.34 | 2–14 | 5.3% | 1.6% | 9.3% | 7.0% | 12.0 s | 43.3 s |
| Time PRD (hazard 0.00227·t per s) | 6.54±1.40 | 2–15 | 5.7% | 2.3% | 7.7% | 14.7% | 10.0 s | 44.0 s |
| Uniform ±50% (13–39 s) | 6.47±0.82 | 4–10 | ~0% | ~0% | 0% | 0% | 16.0 s | 36.3 s |
| Normal SD 6, clamped 12–40 | 6.45±0.66 | 4–10 | ~0% | ~0% | 0% | 0% | 18.9 s | 33.6 s |
| Triangular 13/26/39 | 6.44±0.61 | 4–9 | ~0% | ~0% | 0% | 0% | 19.3 s | 33.1 s |
| Uniform ±30% (18.2–33.8 s) | 6.44±0.55 | 5–8 | 0% | 0% | 0% | 0% | 20.0 s | 32.2 s |
| Fixed 26 s | 6 (7 if first at t=0) | – | 0% | 0% | 0% | 0% | 26 s | 26 s |

- **Stratified (one arrival uniformly placed in each 1/7 of the day, about 25.7 s windows):** always exactly 7 customers. The longest gap is never more than 51.4 s (median 40 s), and 3 arrivals never fall within 20 s. However, the median shortest gap is 11.4 s and can approach 0, so add a minimum-gap clamp.

**Slot blocking with 3 slots (computed, Erlang B)**
- If arrivals are Poisson and a 4th customer is lost or turned away when all 3 slots are busy, the blocked fraction depends on the service/occupancy time S. The Erlang B result does not depend on the service-time distribution.

  | S | Blocked at 26 s interval | Blocked at 20 s interval (ads) |
  |---|---|---|
  | 15 s | 1.8% | 3.3% |
  | 20 s | 3.5% | 6.2% |
  | 30 s | 8.3% | 13.4% |
  | 45 s | 17.0% | 24.7% |
  | 60 s | 25.5% | 34.6% |

- With fixed 26 s arrivals and fixed service, nothing is blocked until S > 78 s. Arrival randomness alone creates overflow and idle time at the same average load.
- 90 s patience combined with a 26 s mean interval means an ignored shop fills all 3 slots in about 52 s under a fixed timer. The steady-state number waiting if the player serves no one is λ·90 ≈ 3.5, which is more than 3 slots. Patience and interval are already tuned tightly, so extra clumping from Poisson would be felt strongly.

**Recommended approach (my synthesis from the sources and numbers above)**
1. **Base model: a bounded jitter renewal timer**, uniform or triangular, about ±25–35% of the current interval (26 s gives about 18–34 s). It follows the L4D precedent, keeps the daily count within ±1–2 of the mean, and keeps a visible "someone will come soon" rhythm. Use a triangular distribution if the extreme gaps should be rarer than under a uniform.
2. **If the count per day must be exact** (for example, an economy tuned to exactly 7): use stratified slots or a shuffle bag of gap multipliers, for example {0.7, 0.85, 1.0, 1.0, 1.15, 1.3} × interval, drawn without replacement. Combine either with a minimum gap of about 8–10 s, so two customers never pop in on the same frame unless designed to.
3. **Avoid pure Poisson or per-frame `Random.value < dt/26`.** At n≈7 its CV is 38%, and about half of days have a 60-s drought. Per-frame rolls in Unity also depend on frame rate unless run on a fixed tick. RCT gets away with it because it runs a fixed 40 Hz tick with hundreds of guests.
4. **Guarantee an opening:** schedule the first customer early and fixed or tightly jittered (for example 2–6 s). This follows CorsixTH's `force_arrival` guard; pure exponential gives a 31.5% chance of a >30 s empty start in a 180 s day.
5. **Rush feel belongs in the mean, not the variance.** Multiply the interval by a designed day-curve, for example calm open, one or two "rushes" where 2–3 customers arrive about 8–12 s apart around mid or late day, then a short lull before close. This follows Booth's Build Up / Relax and Lopez's escalating peaks with troughs. Keep the total area under the curve so the expected count stays about 7 (or the ads-boosted count).
6. **Ads and upgrades:** apply them as a multiplier on the mean interval (26 → e.g. 22 → 20) and keep jitter as a percentage, so the feel stays the same and only the density changes. RCT models marketing as additional independent arrival streams; for a 3-slot shop, a shorter interval is simpler and bounded. Decide whether a mid-day purchase re-plans the rest of the day or takes effect next day. Next day keeps a precomputed schedule simple.
7. **Determinism:**
   - At day start, create a dedicated PRNG seeded from (save seed, day index) and generate the full arrival schedule, times plus customer types. Store either the seed or the schedule and next index in the save.
   - Never share that PRNG with cosmetic or UI randomness. OpenRCT2's `ScenarioRand`/`UtilRand` desync bugs are the cautionary example.
   - Compare scheduled times against accumulated simulation time, not frame counts.
   - This mirrors CorsixTH's precomputed `spawn_dates`. It makes a reloaded mid-day save replay identical arrivals as long as the player-independent schedule is not re-rolled on load.
   - The day's count and times can also be shown or tested ahead of time, for example unit tests asserting count ranges per seed.

### Gaps
- Service time per cotton candy was not given, so the blocking table is parametric in S. The real overflow rate depends on the game's actual serve time and on whether turned-away customers are lost or queue outside.
- None of the sources run A/B player tests of jittered vs Poisson arrivals in shop games. The fit to a 180-s day rests on the math above plus the L4D, Meier/Pardo, Tetris and Dota precedents, not on direct playtest evidence.
- I did not verify that CorsixTH saves `spawn_dates` and its RNG state across save/load, or how `math.random` is seeded there.
