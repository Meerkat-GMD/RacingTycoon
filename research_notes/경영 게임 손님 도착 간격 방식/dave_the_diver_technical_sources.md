# Dave the Diver (Bancho Sushi): implementation-level sources on customer arrival timing

Scope: the main Bancho Sushi service only. Sources are mod source code on GitHub (BepInEx 6 IL2CPP plugins), modder notes built from `dump.cs` and decompiled interop assemblies, one Cheat Engine table on GitHub, two community wikis (Fandom and an English machine translation of Korean Namuwiki), and Steam discussions. No executables, mods, cheat tables or DLLs were downloaded or run; only web pages, text source files and GitHub API listings were read. Research date: 2026-09-27. The mod code dates from July-August 2024 (game version at that time); class names may have changed in later patches.

Method note: GitHub code search (authenticated `gh search code`) was run for `SushiBarCustomer`, `SushiBar.Customer`, `SushiBarCustomerSpawner`, `CustomerSpawn SushiBar`, `CustomerEnterCurveType`, `SushiBarCustomerManager`, `enterCurveList`, `customerCurce`, `GetElaspedEveningHourSpace` and `SushiBarRankEntity`. `SushiBarCustomerSpawner` and `CustomerSpawn SushiBar` returned zero hits. Every arrival-related name (`CustomerEnterCurveType`, `SushiBarCustomerManager`, `enterCurveList`, `customerCurce`) returned only `devopsdinosaur/dave-the-diver-mods/testing/disabled.txt` and its fork `Arutsuyo/SuperDave2.0/testing/disabled.txt`. That one file is the only public source found that touches the arrival system.

## 1. Do mods or cheat tables touch customer spawn timing or count, and which class, method and field names do they reference?

### Takeaway
No public mod or cheat table found changes customer count or arrival timing. The existing sushi mods change only patience, revenue, wasabi and staff speed. One modder's test code (SuperDave author devopsdinosaur, disabled) reveals the arrival system itself: a `SushiBarCustomerManager` that holds a dictionary of Unity `AnimationCurve`s (`enterCollections.enterCurveList`, keyed by `CustomerEnterCurveType`, selected by a field named `customerCurce`). The modder read it in a hook on the evening timer (`DayManager.GetElaspedEveningHourSpace`). This is the strongest evidence that arrivals follow a designer-authored curve over the evening, not a fixed interval.

### Cited Findings

**Arrival system (only public source)**
- The disabled test code in devopsdinosaur's repo hooks `DayManager.GetElaspedEveningHourSpace` (Postfix, `ref int __result`). It then iterates `Resources.FindObjectsOfTypeAll<SushiBarCustomerManager>()`, logs `manager.customerCurce` as "curveType", reads `CustomerEnterCurveDictionary curves = manager.enterCollections.enterCurveList`, loops `foreach (CustomerEnterCurveType key in curves.Keys)`, takes `AnimationCurve curve = curves[key].enterCurve`, and logs every keyframe's `value`, `time`, `inTangent`, `inWeight`, `outTangent` and `outWeight`. The logged keyframe values are not published anywhere. — [devopsdinosaur/dave-the-diver-mods testing/disabled.txt, lines 87-105](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/testing/disabled.txt)
- The same file hooks these `DayManager` evening-timer methods (names exactly as spelled, including the game's "Elasped" typo): `StartEveningTimer`, `PauseEveningTimer`, `ResumeEveningTimer`, `SetPauseEveningTimer`, `InitEveningTimer`, `SpendEveningHours`, `SpendEveningHoursFromListMax`, `GetElaspedEveningHourSpace` (returns `int`), `GetRemainEveningHours` (returns `Il2CppSystem.TimeSpan`), `GetElapsedEveningHours` (returns `TimeSpan`) and `GetElaspedEveningHourRatio` (returns `float`). — [disabled.txt lines 26-130](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/testing/disabled.txt)
- The code reads two obfuscated `DayManager` fields as `Il2CppSystem.TimeSpan`: `IFLADCPPOKP` (logged as `evening_time_span`) and `IOGHDDCLEIM` (logged as `evening_elapsed`). It computes `remaining_seconds = (evening_time_span - evening_elapsed).TotalSeconds`. In the modder's experiment, when `remaining_seconds < 85` the elapsed field is reset to a zero TimeSpan, and `GetRemainEveningHours` is forced to return `new TimeSpan(0, 1, 30)` when under 60 s. These are the modder's experimental values, not game defaults. — [disabled.txt lines 12-18, 107-114](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/testing/disabled.txt)
- The test plugin also hooks `SushiBarManager.Update` (Prefix; empty body, used for key polling). It imports namespace `SushiBar.Customer`. — [devopsdinosaur testing/TestingPlugin.cs](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/testing/TestingPlugin.cs)
- `testing/disabled.txt` was last changed in commit `e01c7be` on 2024-07-28 ("Added staff cook/walk speed and infinite wasabi"). — [GitHub commit history for testing/disabled.txt](https://github.com/devopsdinosaur/dave-the-diver-mods/commits/HEAD/testing/disabled.txt)

**Sushi mods that exist (none change arrivals)**
- SuperDave / DaveDiverExpansion 2.0 sushi options are `Sushi - Infinite Customer Patience`, `Sushi - Money Boost`, `Sushi - Infinite Wasabi` and `Sushi - Cook Multiplier`. The Harmony targets are:
  - `SushiBarCustomer.LateUpdate` (Postfix). It sets `__instance.WaitingSpeedParameter = (__instance.IsOrderWait || __instance.IsOrderWaitDrink) ? 0f : 1f` and `__instance.RevenueBuffParameter`.
  - `SushiBarContext.WasabiGratersData.UpdateWasabiCount` (Prefix on `ref int count`).
  - `SushiBarStaffBase.CalcCookingTime` (Postfix dividing `__result`).
  
  The customer class lives in namespace `SushiBar.Customer`. — [yslailo/DaveDiverExpansion2.0 SushiBar.cs](https://github.com/yslailo/DaveDiverExpansion2.0/blob/HEAD/src/DaveDiverExpansion/Features/SuperDave/SushiBar.cs); same patches in [devopsdinosaur super_dave/SuperDavePlugin.cs](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/super_dave/SuperDavePlugin.cs), which also patches `SushiBarStaffBase.CalcMoveSpeed`
- Nexus lists SuperDave 2.0 sushi features as a money boost, a sushi-bar speed boost and a staff walk multiplier. DaveDiverExpansion lists infinite patience, infinite wasabi, a money boost and faster staff cooking. Harpoon Tip Manager has an "experimental customer patience cheat" in its config file. None of them lists a customer-count or spawn-rate option. — [SuperDave 2.0 (Nexus)](https://www.nexusmods.com/davethediver/mods/17), [DaveDiverExpansion (Nexus)](https://www.nexusmods.com/davethediver/mods/20), [Harpoon Tip Manager (Nexus)](https://www.nexusmods.com/davethediver/mods/21) (these descriptions come from search-result snippets; the pages themselves were not fetched)
- A GitHub issue on the SuperDave repo reports that with "customers never get frustrated" on, a customer at the end of the night waiting for a sold-out item soft-locked the game ("Couldn't use my phone to exit either. Had to Alt+F4"). — [devopsdinosaur/dave-the-diver-mods issue #24 "End of night glitch"](https://github.com/devopsdinosaur/dave-the-diver-mods/issues/24)

**Other class names from modder notes**
- A modder's notes list `SingletonNoMono` managers including `SushiBarAnalyticsManager`, `SushiBarDispatchListManager`, `SushiBarKitchen`, `SushiBarMenuManager`, `SpecialCustomerDataManager`, `DayManager` and `TimeManager`. — [yslailo/DaveDiverExpansion2.0 docs/game-internals.md](https://github.com/yslailo/DaveDiverExpansion2.0/blob/HEAD/docs/game-internals.md)
- The sushi scene is `DR_SushiBar` and the player class is `SushiBarPlayerHanlder` (the game's own misspelling). The doc flags this table as "from decompiled code analysis and IsilDump inference, not yet verified". — [yslailo docs/game-classes.md](https://github.com/yslailo/DaveDiverExpansion2.0/blob/HEAD/docs/game-classes.md)
- The Archipelago modder's cheat sheet (built from `dump.cs`) lists:
  - `SushiBarManager : Singleton<SushiBarManager>` with `void OnEventSushiBarOpened()` ("restaurant opens for service") and `CanProcessVIPShowdownResult(out MissionData, out MissionConditionData, out VIPCustomer)`
  - `VIPCustomer : SushiBarCustomer` with `string AssetKey` and `MissionData m_LinkMissionData`
  - `VIPCookingScenarioDataList.VIP_TID` enum (`WangPang=9100017`, `Alex=9100018`, `Pastro=9100019`)
  - `SushiBarAnalyticsReportSequenceCookStar.DoSequence()` ("fires after each service night")
  - `JungleSushiBarRankEntity`, design data for the DLC Bancho Grill rank
  
  — [AronTheunissen/dave-the-diver-archipelago docs/CLASS_NAME_CHEAT_SHEET.md](https://github.com/AronTheunissen/dave-the-diver-archipelago/blob/HEAD/docs/CLASS_NAME_CHEAT_SHEET.md)
- A player's `Player-prev.log` stack traces show `SushiBar.Customer.SushiBarTutorialCustomer:Served(SushiBarStaff)`, `SushiBarManager:OnDirectHandler(Input)` and `StaffDave:OnInteraction_Impl(SushiBarInteraction, Boolean)`. — [xu-kq/Game-Saves DAVE THE DIVER/Player-prev.log](https://github.com/xu-kq/Game-Saves/blob/HEAD/DAVE%20THE%20DIVER/Player-prev.log)

**Cheat tables**
- The one Cheat Engine table on GitHub has only Oxygen (`PlayerBreathHandler.set_HP`), Gold (`PlayerInfoSave.set_gold`, `m_Gold`) and 999 Ingredients (`IngredientsData.UpdateSaveData`) entries. There is no customer entry. — [januwA/ce-daveTheDiver DaveTheDiver.CT](https://github.com/januwA/ce-daveTheDiver/blob/main/DaveTheDiver.CT)
- FearlessRevolution has several Dave the Diver table and trainer threads: [t=21862](https://fearlessrevolution.com/viewtopic.php?t=21862), [+48 Trainer t=23345](https://fearlessrevolution.com/viewtopic.php?t=23345), [+55 Trainer t=26193](https://fearlessrevolution.com/viewtopic.php?t=26193), [table v1.0 t=30233](https://fearlessrevolution.com/viewtopic.php?t=30233), [t=34725](https://fearlessrevolution.com/viewtopic.php?t=34725), [+3 t=25132](https://fearlessrevolution.com/viewtopic.php?t=25132). Search snippets mention a "Timer pointer", ammo and ingredients, plus infinite oxygen, no depth limit, infinite weight, infinite ingredients and infinite stamina (t=30233). No snippet mentions customers. — [WebSearch results for these threads]

### Inferences
- (Inference) Customer arrival is data-driven by a per-type `AnimationCurve` (`CustomerEnterCurveType` → entry with `.enterCurve`), chosen per night by `SushiBarCustomerManager.customerCurce`. The curve is evaluated against evening progress (`DayManager.GetElaspedEveningHourRatio` 0..1 or `GetElaspedEveningHourSpace`). The modder chose to dump the curves exactly where the elapsed evening "space" is computed, so they suspected that link.
- (Inference) Two readings of the curve are plausible and the sources do not settle which one is right: (a) x = normalized evening time, y = cumulative fraction of the night's customer quota that should have entered by now; (b) y = spawn rate or density at that time. Reading (a) fits the per-night "max customers" quota best (see Q2): the manager would spawn whenever `quota * curve(t)` exceeds the number spawned so far.
- (Inference) A dictionary of several curve types implies more than one arrival profile. Candidates are normal service, party/event nights, tutorial nights (`SushiBarTutorialCustomer`) and night-dive-shortened nights. The enum member names were not published.
- (Inference) The evening timer is a real-time `TimeSpan` in `DayManager`, and it can be paused (`PauseEveningTimer`/`SetPauseEveningTimer`) and skipped (`SpendEveningHours`). Arrival scheduling is therefore tied to a pausable timer, not to wall clock time. The game pauses while the menu is open (Fandom: "The menu can be opened, pausing the game").

### Gaps
- The actual keyframe values of the enter curves, the names of the `CustomerEnterCurveType` enum members and the evening duration in seconds (`IFLADCPPOKP`) are not published in any source found. The only way to get them would be to decompile or run the game, which this task excludes.
- FearlessRevolution thread contents were not opened. It is unverified whether any table exposes a customer-count or spawn-timer entry; the visible titles and snippets do not suggest one.
- No Thunderstore listing for Dave the Diver sushi mods was found.
- No mod was found that exposes a "more customers" or "longer night" option. The "extend the night" experiment in devopsdinosaur's test code was never released.

## 2. Are there datamined or community tables of customer counts or spawn intervals per rank or per night?

### Takeaway
There is no datamined spawn-interval table. Community wikis give a per-Cooksta-rank nightly customer total (a quota, not an interval): 10 / 15 / 21 / 28 / 36 / 45 per Fandom, with "+5, +6, +7, +8, +9" rank-up rewards. Namuwiki gives slightly different Bronze and Silver figures and the reduced counts after a night dive. Both sources say the nightly count varies somewhat.

### Cited Findings
- Fandom Cooksta "Ranks" table, column "Max Customers": Coal 10, Bronze 15, Silver 21, Gold 28, Platinum 36, Diamond 45. The rank-up rewards list "Customers +5" (Bronze), "+6" (Silver), "+7" (Gold), "+8" (Platinum) and "+9" (Diamond). Rank requirements are Bronze 10 followers; Silver 20 followers + 2 researched recipes; Gold 100 followers + 125 Best Taste + 5 recipes; Platinum 200 / 250 / 19; Diamond 720 / 375 / 32. — [Dave the Diver Wiki (Fandom): Cooksta](https://dave-the-diver.fandom.com/wiki/Cooksta)
- Namuwiki "number of daily guests (night)" column: No rank 10, Bronze 14, Silver 24, Gold 28 (17), Platinum 36 (25), Diamond 45 (27).
  - The parenthesized numbers are counts after a night dive. Footnote 4: after night diving "you will start operating the restaurant with a one-third decrease. It's a little less than 2/3."
  - Footnote 5 on Platinum's (25): "In fact, it comes between 21 and 25 people."
  - The text adds "Sometimes the number of guests comes less or sometimes more."
  
  — [NamuWiki (English machine translation): Dave the Diver/Restaurant operation](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- The Bronze and Silver figures conflict: Fandom says 15 and 21, Namuwiki says 14 and 24. Gold, Platinum and Diamond agree (28 / 36 / 45). — [Fandom Cooksta](https://dave-the-diver.fandom.com/wiki/Cooksta) vs [NamuWiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- Night-dive counts also conflict. A Steam player says "at gold rating you get 28 customers or 21 if you go night diving. Right now at platinum I get 36" (RandomTurtle, 29 Jun 2023), while Namuwiki gives Gold 17 after a night dive. — [Steam discussion 3805027459316761930](https://steamcommunity.com/app/1868140/discussions/0/3805027459316761930)
- Steam players state that night diving "shortens the time that the restaurant can be open by a third", giving "roughly a third less customers" (Bastard™, 30 Jun 2023). Another: "Each rank has X amount of customers you can get per night... Doing a night dive lowers this usual maximum amount" (Hentaika, 8 Jul 2023). A third: "If you do really well, have highly leveled staff you can actually 'sell out' in that 2/3rd of the time... having as many customers as normally" (HoneyDrake, 8 Jul 2023). — [Steam discussion "Diving at night" 3805027459319213248](https://steamcommunity.com/app/1868140/discussions/0/3805027459319213248/)
- A guide site says the nightly customer count "is determined by your Cooksta rating, not by your decor, your dishes, or the stats of your staff", and that "it vary every single night" with no in-game preview. It gives no numbers. — [The Nerd Stash](https://thenerdstash.com/dave-the-diver-how-many-customers-per-night-come-to-the-bar-answered/)
- At the Branch location, "The Manager's total stats determine both the total number of customers that will visit the Branch that night" and the maximum ingredient rank. — [Fandom: Sushi Staff Strategy](https://dave-the-diver.fandom.com/wiki/Sushi_Staff_Strategy)
- The DLC Bancho Grill has its own rank design data class, `JungleSushiBarRankEntity`. — [AronTheunissen CLASS_NAME_CHEAT_SHEET.md](https://github.com/AronTheunissen/dave-the-diver-archipelago/blob/HEAD/docs/CLASS_NAME_CHEAT_SHEET.md)

### Inferences
- (Inference) Fandom's 10→15→21→28→36→45 series matches its "Customers +5/+6/+7/+8/+9" reward lines exactly, which suggests it was transcribed from the in-game rank-up reward UI. Namuwiki's 14 and 24 may be observed counts from single nights, which both wikis say vary.
- (Inference) The design data likely stores a nightly customer quota per Cooksta rank (a rank entity analogous to `JungleSushiBarRankEntity`, possibly named `SushiBarRankEntity`; not confirmed). Arrival timing is then produced by distributing that quota over the evening with the enter curve (Q1). The quota rises by a growing step (+5 → +9) per rank.
- (Inference) The night-dive reductions are about 0.6-0.75x rather than exactly 2/3 (Namuwiki 28→17, 36→21-25, 45→27; Steam 28→21). That fits a time-driven curve with a shortened evening: the quota fraction scheduled for the lost portion never spawns. It also fits randomness in the final count. The sources cannot separate the two.

### Gaps
- No datamined table (CSV, JSON or spreadsheet) of Bancho Sushi rank data, customer quotas, per-night overrides or spawn intervals was found on GitHub or the wikis.
- No source gives an explicit "seconds between customers" value, random range or minimum gap.
- Neither wiki states the exact random variance of the nightly count, whether variance is a ± range or comes from the time curve, or how Bancho Grill counts scale.

## 3. Is the arrival interval fixed, uniformly random, weighted, or scripted per night? Is there a time-of-night curve?

### Takeaway
The code-level evidence points to a scripted time-of-night curve: `AnimationCurve` assets keyed by `CustomerEnterCurveType`, read against `DayManager` evening progress. That is neither a fixed interval nor a flat uniform random interval. Players and wikis observe that the nightly count varies somewhat and that "drink orders and dirty tables appear to be random". Nobody has published the curve shape or any per-customer jitter.

### Cited Findings
- Arrival curves exist as `AnimationCurve` objects (`curves[key].enterCurve`) in `SushiBarCustomerManager.enterCollections.enterCurveList`, keyed by `CustomerEnterCurveType`. The active one is referenced by `customerCurce`. — [devopsdinosaur disabled.txt](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/testing/disabled.txt)
- The evening timer exposes an elapsed ratio (`GetElaspedEveningHourRatio` → `float`), an elapsed "space" (`GetElaspedEveningHourSpace` → `int`), and elapsed and remaining `TimeSpan`s. — [devopsdinosaur disabled.txt](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/testing/disabled.txt)
- "Sometimes the number of guests comes less or sometimes more." — [NamuWiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81); "it vary every single night" — [The Nerd Stash](https://thenerdstash.com/dave-the-diver-how-many-customers-per-night-come-to-the-bar-answered/)
- "Drink orders and dirty Tables appear to be random; ... it's sometimes possible for Dave and his Servers to get unlucky, having too many of those tasks to do at once... especially at high Cooksta Rank." — [Fandom: Sushi Staff Strategy](https://dave-the-diver.fandom.com/wiki/Sushi_Staff_Strategy)
- One player claims arrivals depend on throughput: "the max number of customer at the same time is what is affected by the rating. The quality and speed of service will determine how many customer you get", and "your popularity will determine how many seat are filled at the same time, so the faster you serve them, the more new customer come" (TheClosetSkeleton, 29-30 Jun 2023). This conflicts with the fixed per-rank totals reported by other players and both wikis. — [Steam discussion 3805027459316761930](https://steamcommunity.com/app/1868140/discussions/0/3805027459316761930)
- Special nights are scripted on top of normal arrivals. During party events "special guests who come to enjoy the party, in addition to the usual guests, visit", and party-attire guests leave if no matching menu exists. VIP customers are tied to missions (`VIPCustomer.m_LinkMissionData`, `SushiBarManager.CanProcessVIPShowdownResult`). — [NamuWiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81); [AronTheunissen CLASS_NAME_CHEAT_SHEET.md](https://github.com/AronTheunissen/dave-the-diver-archipelago/blob/HEAD/docs/CLASS_NAME_CHEAT_SHEET.md)

### Inferences
- (Inference) The method is best described as quota × authored time curve. The per-rank nightly total is spread over the pausable evening timer according to a designer-drawn `AnimationCurve`, with several curve profiles available. This allows a time-of-night shape (for example a slow start, a mid-evening rush and a tail-off), but the actual shape is unknown.
- (Inference) The reported night-to-night variance (namu's 21-25 at Platinum after a night dive) suggests some randomness: a random pick of curve type, jitter on the quota, or arrivals lost to a full or dirty house. Which one applies cannot be confirmed from public sources.
- (Inference) The throughput claim ("serve faster → more customers") would hold if a new arrival is gated by a free seat, so slow turnover delays or drops arrivals. This could coexist with a quota curve. It is unconfirmed player opinion.

### Gaps
- There is no public evidence on whether individual arrivals get random jitter, whether customers come singly or in groups, or whether the curve y-axis is cumulative or a rate.
- The enum values of `CustomerEnterCurveType` and the rule for choosing `customerCurce` each night (random, rank-based or event-based) are unknown.
- No measured arrival timestamps (for example stopwatch logs of customer entries per night) were found in Steam discussions or guides.

## 4. How are seats occupied and what happens when the restaurant is full or seats are dirty?

### Takeaway
Customers take seats even if the previous diner left dirty dishes. If the seat is not cleaned by the time they are ready to order, they leave unhappy: fewer dishes sold, a lower rating and a negative Cooksta review. No source describes a queue at the door. The seat count and the full-house rule for new arrivals were not found.

### Cited Findings
- "Sometimes, customers leave behind a pile of dirty dishes. New customers will still sit at the bar, but if the table isn't cleaned by the time they are ready to order, then the customer will get up and leave." — [Fandom: Bancho Sushi](https://dave-the-diver.fandom.com/wiki/Bancho_Sushi)
- Leftovers are "created with a certain probability by guests who have finished their meal". Cleaning gives a 25-gold tip. A customer seated at an uncleaned spot "will return with a complaint and the quantity of food sold will decrease and their star rating will be lowered". — [NamuWiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- "When the restaurant is very popular, the slightly slower customer turnaround from having only one Cook may rarely result in customers having to sit somewhere that requires Cleaning". If the table is not cleaned promptly "the customer will leave unhappy, resulting in a negative review on Cooksta and loss of profits and Artisan's Flame". Servers prioritise Cleaning over Serving. — [Fandom: Sushi Staff Strategy](https://dave-the-diver.fandom.com/wiki/Sushi_Staff_Strategy)
- Customer patience is a per-customer drain. `SushiBarCustomer.WaitingSpeedParameter` is the multiplier the patience mod sets to 0 while `IsOrderWait` or `IsOrderWaitDrink` is true and to 1 otherwise. — [yslailo SushiBar.cs](https://github.com/yslailo/DaveDiverExpansion2.0/blob/HEAD/src/DaveDiverExpansion/Features/SuperDave/SushiBar.cs)
- With no menu items available, customers order Norimaki for 1 gold, which lowers the night's score. Artisan's Flame points (1-5 per night) depend on customers served and proper dishes. — [Fandom: Bancho Sushi](https://dave-the-diver.fandom.com/wiki/Bancho_Sushi)
- At end of night the service apparently waits for seated customers to finish. With infinite patience on, a customer waiting for a sold-out item soft-locked the end of the night. — [devopsdinosaur issue #24](https://github.com/devopsdinosaur/dave-the-diver-mods/issues/24)
- Full staffing at the main restaurant is 2 kitchen + 2 serving staff. Staff slots unlock by Cooksta rank (Bronze Kitchen +1, Silver Serving +1, Gold Kitchen +1). — [NamuWiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81); [Fandom: Cooksta](https://dave-the-diver.fandom.com/wiki/Cooksta)

### Inferences
- (Inference) The dirty-seat rule means arriving customers may pick a seat whether it is clean or dirty. The penalty comes later, when they are ready to order. Arrival timing and cleaning speed therefore interact: a tight arrival curve at high rank turns random leftovers into walk-outs.
- (Inference) The end-of-night soft-lock implies the evening timer stops new arrivals when it runs out, but the night ends only when every seated customer has left. That is a "last call, then drain" model.

### Gaps
- The number of seats in Bancho Sushi, whether it changes with rank, and what happens when every seat is occupied at an arrival time (the customer is dropped, delayed or waits) are not documented in any source found.
- It is unconfirmed whether seat selection is random, nearest-first or left-to-right.

## 5. Tool-assisted or speedrun community analysis of customer spawn patterns

### Takeaway
No tool-assisted, speedrun or RNG-manipulation analysis of sushi customer arrivals was found. The only instrumented look at the system is devopsdinosaur's unpublished curve dump (Q1).

### Cited Findings
- Searches for speedrun and RNG discussion surfaced only general Steam threads ("Dave the Diver Speedrun", "Speed at the Sushi Bar???") and no arrival-timing analysis. — [Steam: Dave the Diver Speedrun](https://steamcommunity.com/app/1868140/discussions/0/3801651661449997017/), [Steam: Speed at the Sushi Bar???](https://steamcommunity.com/app/1868140/discussions/0/3805028677408545594/)
- The instrumented dump of the enter curves (`frame.value`, `frame.time` and tangent and weight fields) exists only as disabled test code. Its output was never published. — [devopsdinosaur disabled.txt](https://github.com/devopsdinosaur/dave-the-diver-mods/blob/HEAD/testing/disabled.txt)

### Inferences
- (Inference) Anyone who wants the real curve values would have to reproduce devopsdinosaur's dump: a BepInEx IL2CPP plugin logging `SushiBarCustomerManager.enterCollections.enterCurveList` keyframes, or AssetRipper extraction of the `SushiBarCustomerManager` component in the `DR_SushiBar` scene. This research did not do either, by constraint.

### Gaps
- There is no speedrun.com guide or TAS analysis of Bancho Sushi customer spawns. The Steam reference guide (sharedfiles id 2998778318) could not be read (HTTP 429), so its content is unverified.
