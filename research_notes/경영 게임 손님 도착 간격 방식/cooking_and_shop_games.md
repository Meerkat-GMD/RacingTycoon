# Customer arrival (spawn) methods in cooking, restaurant and small-shop time-management games

Scope: PlateUp!, Papa's series (Flipline), Diner Dash, Overcooked 1/2, Good Pizza Great Pizza, Cooking Fever (mobile level-based), Supermarket Simulator, TCG Card Shop Simulator, Kebab Chefs!, Kairosoft Cafe Master Story. Dave the Diver and large tycoon sims are excluded by assignment.
Reference target: cotton-candy shop, 3 customer slots, 180 s business day, base interval about 26 s, customer wait about 90 s.
Source-quality tags used below: **[wiki]** community wiki, **[datamined]** the wiki quotes decompiled game code or formulas, **[official]** developer or publisher text, **[player]** forum or anecdote, **[snippet]** search-engine summary whose exact page wording I could not open.

## 1. What arrival method does each game use? (fixed vs randomized intervals, and how much randomness)

### Takeaway
None of the documented games uses a "random interval between customers" as its main model. The common patterns are: (a) a **daily customer count computed first**, then arrivals **spread evenly** across the day (PlateUp!, where only group size is random); (b) a **fixed per-day or per-level customer quota** with authored order (Papa's, Diner Dash, Cooking Fever); (c) **refill-to-a-concurrency-cap**, where a new customer spawns when someone leaves (TCG Card Shop Simulator, with a randomized customer type). PlateUp! is the best documented game. Its base density is **1 customer per 25 s of day length** before multipliers, which is almost exactly the 26 s interval of the target shop.

### Cited Findings

**PlateUp! (best documented; community wiki with a reverse-engineered formula)**
- The number of groups is fixed before the day starts. The Preparation Phase shows "Expected Groups" and "Group Sizes", and during the day "the exact number of Expected Groups for the day will pass through the restaurant". [wiki] — [PlateUp! Wiki: Customers](https://wiki.plateupgame.com/gameplay/Customers)
- Timing is deterministic and even: "Customers arrival time are designed to be spread out evenly during the entire day with a small buffer before the end". Once the time bar fills, no more customers appear unless a card such as Morning Rush or Closing Time changes that. [wiki] — [PlateUp! Wiki: Daily Operations](https://wiki.plateupgame.com/gameplay/DailyOperations)
- Day length starts at 100 s and grows by 25 s after every 3 completed days. The wiki's worked example for 10 expected groups gives about 9 s between groups on Day 1 (100 s), about 21 s on Day 15 (225 s) and about 34 s in Overtime Day 30 (350 s). [wiki] — [Daily Operations](https://wiki.plateupgame.com/gameplay/DailyOperations)
- Formula [wiki, reverse-engineered]: `Expected Group = Customer Count / Average Group Size`, where `Customer Count = (Day Length)/25 × (Customer Modifier Rate) × (Day Modifier) × (Multiplayer Modifier) × (Course Amount) × (Card Multiplier)`. The Day Modifier is 1, 1.25 and 1.5 for days 1–3, `1.5 + 0.15×(day−3)` for days 4–15, and `1.5 + 0.15×((day−3) + (day−15)^1.5)` in overtime. The Customer Modifier Rate is 0.85^x, where each −15% card adds 1 to x and each +15% card subtracts 1. The Multiplayer Modifier is 80%, 100%, 125% or 150% for 1 to 4 players. Courses count 1, 0.8 or 0.6667. Advertising adds 0.25 to the card multiplier. — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- Randomness is only in group size: "each group is randomizes to be any of the sizes between the min and max group size". The seed fixes it under "Seed Affects Everything". The default group size is 1–2. — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- Expected-groups table for 1 player (default settings, passive scaling only): Day 1–15 = 3, 3, 4, 5, 5, 7, 7, 8, 9, 10, 11, 13, 13, 14, 16 groups. For 4 players: 4 … 30. [wiki] — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- Practice Mode spawns "Only one group of cats … at a time", and a new group comes only when the previous one leaves. This is a separate "one at a time" model used only for rehearsal. — [Daily Operations](https://wiki.plateupgame.com/gameplay/DailyOperations)

**Papa's series (Flipline Studios)**
- Each game has a **set maximum number of customers per day**, reached at a given rank. Papa's Pizzeria, Burgeria and Burgeria HD: 10 (from Rank 10). Hot Doggeria: 9 (from Rank 20). Taco Mia!, Freezeria, Cheeseria, Bakeria, Sushiria and others: 8. Most later games (Pancakeria, Wingeria, Cupcakeria, Pastaria, Scooperia, Pizzeria Deluxe, Freezeria Deluxe and others): 7. Freezeria To Go! and Cupcakeria To Go!: 6. [wiki] — [Flipline Wiki: Order Station](https://fliplinestudios.fandom.com/wiki/Order_Station); corroborated for 10/day in [Papa's Pizzeria](https://fliplinestudios.fandom.com/wiki/Papa's_Pizzeria) and 8/day in [Papa's Taco Mia!](https://fliplinestudios.fandom.com/wiki/Papa's_Taco_Mia!)
- From Taco Mia! on, the last customer of each day is a scripted **Closer** chosen from a fixed weekly calendar with one named closer per weekday. Closers use a stricter grading scale. Pizzeria and Burgeria have no closers. [wiki] — [Flipline Wiki: Closer](https://fliplinestudios.fandom.com/wiki/Closer); [Papa Louie Series Wiki: Papa's Pizzeria](https://papalouieseries.fandom.com/wiki/Papa's_Pizzeria)
- Who visits can be steered. A **Customer Coupon** ($25) "guarantees that a customer will come into the restaurant at tomorrow or at the day after tomorrow", and only one can be delivered per day. [wiki] — [Flipline Wiki: Customer Coupons](https://fliplinestudios.fandom.com/wiki/Customer_Coupons)
- I found no documented interval (in seconds) between Papa's arrivals. See Gaps.

**Diner Dash (GameLab / PlayFirst)**
- Levels are authored. Each level lists a money goal, an expert score, a difficulty, a fixed table layout and a fixed customer-type roster. Example: Level 1-1 has 2 tables for 2 and only Rosie customers. Level 1-6 adds Clara the Food Critic. [wiki] — [Diner Dash Wiki: Flo's Diner walkthrough template](https://dinerdash.fandom.com/wiki/Template:Flo's_Diner_(Diner_Dash)) (rendered at [Walkthrough: Flo's Diner](https://dinerdash.fandom.com/wiki/Walkthrough:Flo's_Diner_(Diner_Dash)))
- The walkthrough suggests the sequence is scripted within a level. For Level 2-1 it says "Many big parties will come at the end of this shift". [wiki/guide] — [Walkthrough: Flo's Tiki Palace](https://dinerdash.fandom.com/wiki/Walkthrough:Flo's_Tiki_Palace_(Diner_Dash))
- A search summary of the wiki walkthroughs said weather events trigger "after the 4th group" or "after the 5th group" of customers. This fits a group-indexed script, but I did not see that wording on an opened page. [snippet] — [Walkthrough: Flo's Tiki Palace](https://dinerdash.fandom.com/wiki/Walkthrough:Flo's_Tiki_Palace_(Diner_Dash))

**Overcooked 1/2 (Ghost Town Games)**
- The developers replaced an earlier three-lives system with "a time limit within which to serve as many orders as possible", and they "gradually ramp up the number of orders" as difficulty rises. [official, dev article] — [Game Developer: Game Design Deep Dive, Overcooked](https://www.gamedeveloper.com/design/game-design-deep-dive-building-truly-cooperative-play-in-i-overcooked-i-)
- The game has an order minimum and maximum: an Overcooked 2 mod exposes "Max Orders" and "Min Orders" settings and lets players "remove the conventional order limit". The mod page gives no vanilla values. — [AzzaMods: Order Manager](https://www.azzamods.com/mods/overcooked-2/order-manager/)
- On some New Game+ maps, order timers stay frozen until the first delivery. [player/snippet] — [Steam: Frozen Timer thread](https://steamcommunity.com/app/728880/discussions/1/1737715419904154377/)

**Good Pizza, Great Pizza (TapBlaze)**
- Customers come one at a time in a single-file line. The shop is open "from noon until nine o' clock" of in-game time, with a day countdown. [review] — [Tech-Gaming review](https://www.tech-gaming.com/good-pizza-great-pizza/)
- Chapter events are scripted by day. For example, "Half Day" is "triggered automatically on Day 4" of Chapter 1 and changes what customers order. [wiki] — [GPGP Wiki: Half Day](https://good-pizza-great-pizza.fandom.com/wiki/Half_Day). Festivals raise the amount of pizza ordered. [wiki] — [NamuWiki: Good Pizza, Great Pizza](https://en.namu.wiki/w/%EC%A2%8B%EC%9D%80%20%ED%94%BC%EC%9E%90,%20%EC%9C%84%EB%8C%80%ED%95%9C%20%ED%94%BC%EC%9E%90)

**Cooking Fever (Nordcurrent; this is the mobile level-based model)**
- Each level has a **Level Timer** and a **Customers Count**, so levels have a fixed customer quota and a time limit. [snippet from the Cooking Fever wiki] — [Cooking Fever Wiki: Scoreboard (Sports Bar)](https://cookingfever.fandom.com/wiki/Scoreboard_(Sports_Bar))

**TCG Card Shop Simulator (refill-to-cap, datamined)**
- Max concurrent customers comes from decompiled code: `m_CustomerCountMax = Clamp(m_CustomerCountMaxBase + round(expansionTerm) + shopLevelTerm + ceil(PlayTableSitdownCustomerCount/2), 3, 28)`. Each term grows in "triangular" steps, adding 1.25 per step for expansions and 1 per step for shop level. [datamined] — [TCG Card Shop Simulator Wiki: Mechanics and Formulas](https://tcgcardshopsimulator.wiki.gg/wiki/Mechanics_and_Formulas)
- New customers "stop spawning once the [store] nears maximum capacity". "Once a customer leaves, a new one can then spawn." — [Game Rant: 8 Ways To Attract More Customers](https://gamerant.com/tcg-card-shop-simulator-attract-more-customers/)
- Customer *type* is randomized when a customer spawns: a regular has $20 up to the budget cap, a 4.7% "rich" customer has $40 up to 2× the cap, and a 0.3% "very rich" customer has $300 up to 4× the cap. [datamined] — [Mechanics and Formulas](https://tcgcardshopsimulator.wiki.gg/wiki/Mechanics_and_Formulas)

**Supermarket Simulator (Nokta Games; daily count tied to store level)**
- Player-derived rule: "Start of the game you get 20 customers, each level adds 1 extra customer … at lv50, you'll get around 70 customers per day." Another player at level 50+ with 3 licences reports "over 60 customers a day". [player] — [Steam: How do i get more customers?](https://steamcommunity.com/app/2670630/discussions/0/4518884147854988828/)
- Official patch v0.1.2.3 (2024-03-29): "Increased customer count gap to increase gradually with store level." [official] — [Supermarket Simulator Wiki: Patch v0.1.2.3](https://supermarket-simulator.fandom.com/wiki/Patch_v0.1.2.3); [Steam announcement](https://steamcommunity.com/games/2670630/announcements/detail/4187858160663536133)

**Kebab Chefs! (Biotech Gameworks)**
- The expected daily customer count is shown before opening and depends on menu breadth and dish quality. One player says: "the more different meals you offer in a day, the more guests you get". Another says: "The more meals you have + the more stars meal has = more customers. I have ~60 customers every day". [player] — [Steam: Work hours](https://steamcommunity.com/app/1001270/discussions/0/4297067919527527744/)
- Players report daily counts of 104–133 "expected" customers. [player] — [Steam: Customers come in one at a time](https://steamcommunity.com/app/1001270/discussions/0/4414172638909488369/?l=english)

**Kairosoft Cafe Master Story**
- The official manual text describes demand growth through satisfaction and popularity. It gives no spawn formula: "Once [the Satisfaction gauge is] full, they'll become regulars and visit more often. Some will even drop by specifically to use a certain facility." [official manual transcript] — [Kairosoft Wiki: Manual (Cafe Master Story)](https://kairosoft.fandom.com/wiki/Transcript:Manual_(Cafe_Master_Story))

### Inferences
- The classification below is my synthesis of the findings above.

| Game | Primary arrival model | Randomness in timing | Unit of count |
|---|---|---|---|
| PlateUp! | Count computed per day, then scheduled **evenly** over the day. Rush waves are added at fixed times | None documented in timing; group size is random (1–2 by default) | Groups per day |
| Papa's series | **Fixed quota per day** (6–10 by game), grows with rank to the cap. The last customer is a scripted Closer | Not documented | Customers per day |
| Diner Dash | **Scripted per level** (roster, table layout, big parties late in a shift) | Not documented | Groups per level |
| Overcooked | Timed round, order list kept between a min and max, ramped by level | Not documented | Orders on screen |
| Good Pizza Great Pizza | **Single-file, one at a time** within an in-game clock (noon–9 PM). Events scripted by chapter day | Not documented | Customers per day |
| Cooking Fever | **Fixed quota per level** plus a level timer. An upgrade adds 1–3 customers | Not documented | Customers per level |
| TCG Card Shop Sim | **Refill to a concurrency cap** (3–28) | Customer wealth tier is random (95/4.7/0.3%) | Concurrent customers |
| Supermarket Sim | **Daily count tied to store level** (about 20 + 1 per level, player-derived) | Small day-to-day variance (39 vs 40 reported) | Customers per day |
| Kebab Chefs! | **Daily count from menu size × dish stars** | Not documented | Customers per day |
| Kairosoft Cafe Master | **Popularity- and regular-driven** demand | Not documented | Not documented |

- For the target shop (180 s day, 26 s base interval, which gives about 6–7 arrivals per day), PlateUp!'s model maps almost directly. Its base constant is 1 customer per 25 s of day length before multipliers. A 180 s day at modifier 1.0 would give 7.2 customers. Papa's 6–10 customers per day is the same order of magnitude.
- Even spacing plus random party size (PlateUp!) keeps pacing readable while adding variety. A designer who wants jitter could add ±20–30% to the 26 s interval without changing the daily total. This is a design suggestion, not something a source documents.
- I checked PlateUp!'s solo Day 1–4 table against the formula. It matches if the result is rounded up (ceiling): Day 1 gives 100/25 × 1 × 0.8 / 1.5 = 2.13 → 3; Day 4 gives 125/25 × 1.65 × 0.8 / 1.5 = 4.4 → 5. The rounding rule is my inference.

### Gaps
- **Papa's**: I found no source for the arrival interval within a day, for whether it varies or is random, or for how the per-day count grows before the cap (for example, whether it rises by 1 per rank). The Order Station page only gives the maximum and the rank at which it is reached.
- **Overcooked**: I found no source for the base order spawn interval or for the vanilla min/max order counts. A search summary claimed each level lasts "four minutes", but I could not tie it to a specific page. Common folk knowledge (a new order when the list drops below a minimum, and a cap of about 5 visible orders) is unverified and was left out.
- **Diner Dash**: the per-level arrival schedule (seconds between parties, total parties per level) is not documented on the pages I opened. I found no postmortem. The Fortugno GDC 2007 "four casual design lessons" talk is only on the GDC Vault video, and its transcript was not accessible.
- **Good Pizza Great Pizza**: I found no numbers for customers per day or for the real-time length of a day. A search summary mentioned scheduled delivery orders at 3:30, 5:15 and 7:15 PM in Chapter 3+, but I could not verify the source page.
- **Cooking Fever, Cooking Diary, Cooking Madness**: I found no numbers for per-level customer counts or arrival intervals. The Cooking Diary and Cooking Madness wikis had no arrival mechanics pages.
- **Bakery Simulator and similar shop sims, Coffee Talk, Venba, Pocket Academy**: not covered; I found no usable sources within the call budget. The narrative games are probably scripted per story beat, but this is unverified.

## 2. Does the arrival rate vary within a day (morning/lunch/dinner peaks, rush phases)?

### Takeaway
Only PlateUp! documents explicit intra-day peaks. They are opt-in **Rush cards** that add waves at the start, middle and shortly before closing of the day, plus a Herd Mentality card that packs *all* customers into those three windows. Rush size is **+15% of the daily group count per rush, minimum 1 group**. A "Closing Time?" card adds late arrivals after the time bar ends. Other games mostly keep a flat or authored flow and use a scripted special customer at the end of the day (Papa's Closers) or level-authored late big parties (Diner Dash).

### Cited Findings
- PlateUp! Rush cards: Morning Rush means "More customers at the start of each day", Lunch Rush means "in the middle of the day" and Dinner Rush means "shortly before closing". "Each Rush introduces +15% Expected Groups, which appear in a wave at the specified time of day, indicated on the timeline." — [PlateUp! Wiki: Cards](https://wiki.plateupgame.com/Cards)
- Rush size is "+15% of Expected Group Count (as of v1.3.0; prior to this version, it was based on Customer Count, meaning larger group sizes than the standard would see huge influxes …)". Rushes stack additively, with "a minimum of 1 group being added". — [Customers](https://wiki.plateupgame.com/gameplay/Customers); [Restaurant](https://wiki.plateupgame.com/gameplay/Restaurant)
- Herd Mentality: "Customers are split between three waves – Morning, Lunch, and Dinner. Unlike Rush Cards, does not introduce additional customers." It "will not add the icon to the time bar, but all customer groups set to appear will only appear within the three rush times." — [Cards](https://wiki.plateupgame.com/Cards); [Daily Operations](https://wiki.plateupgame.com/gameplay/DailyOperations)
- Closing Time?: "Customers can arrive after closing time". It "spawns groups based off of 20% of the Customer Count after the 'end of the day' is reached", only "if players have failed to clear all remaining customers" within a grace window. — [Customers](https://wiki.plateupgame.com/gameplay/Customers); [Cards](https://wiki.plateupgame.com/Cards)
- Turbo setting: starts with Morning, Lunch and Dinner Rush active, and expected groups rise by "roughly: 1.5 * (1 + 0.1* day)". — [Restaurant](https://wiki.plateupgame.com/gameplay/Restaurant)
- PlateUp! late-day pressure comes through patience, not arrivals: "Night occurs once the time bar reaches or past 75%", and "during Night, customer's queue patience will decrease 100% faster". Rain (50% daily chance on some maps) makes it decay 50% faster and Snow 100% faster. The factors multiply, so Snow at Night leaves 25% of normal patience. — [Restaurant](https://wiki.plateupgame.com/gameplay/Restaurant)
- PlateUp! Booking Desk: the day "can be sped up by using the Booking Desk, advancing to day to when the next customer group is set to arrive". The day ends when all scheduled customers have been served, not when the bar fills. — [Daily Operations](https://wiki.plateupgame.com/gameplay/DailyOperations)
- Papa's: "Closers are the last Customer of the day. Every day you play, there will be a Closer to serve as the last Customer." [player guide] — [Steam guide: Papa's Freezeria Deluxe](https://steamcommunity.com/sharedfiles/filedetails/?id=2956785457). Taco Mia! was "the first Gameria where the windows outside of the restaurant reveal the time of day". This is only cosmetic as far as sources say. — [Papa's Taco Mia!](https://fliplinestudios.fandom.com/wiki/Papa's_Taco_Mia!)
- Diner Dash Level 2-1: "Many big parties will come at the end of this shift". This is an authored end-of-level spike. — [Walkthrough: Flo's Tiki Palace](https://dinerdash.fandom.com/wiki/Walkthrough:Flo's_Tiki_Palace_(Diner_Dash))
- Supermarket Simulator: "No new one spawn after 9pm, but the one already on their way keep coming." [player] — [Steam: Daily customer limit?](https://steamcommunity.com/app/2670630/discussions/0/4289188948011605689/)
- Good Pizza Great Pizza: there is a noon-to-9 PM clock. Festival and convention days produce visible lines. [review/player] — [Tech-Gaming](https://www.tech-gaming.com/good-pizza-great-pizza/); [Steam: Customers Bug](https://steamcommunity.com/app/770810/discussions/0/594022118134401039/)

### Inferences
- PlateUp!'s rush design is a clean template for a 180 s day. Treat rushes as **additive waves on top of an even baseline** (+15% of the daily count, minimum 1), placed at fixed timeline positions (start, middle, near close) and shown on the day's time bar so players can prepare. A 7-customer day with one rush would add 1 customer, because the 15% rounds up to the minimum.
- "Herd Mentality" shows a second option: keep the daily total the same but *compress* it into windows. This raises difficulty without raising the count.
- Several games raise late-day difficulty through patience modifiers (PlateUp! night) or tougher special customers (Papa's Closers) rather than through more arrivals.

### Gaps
- No source documents real morning, lunch or dinner rate curves in Supermarket Simulator, TCG Card Shop Simulator or Kebab Chefs. Only the Supermarket 9 PM cutoff is reported. I found no datamined time-of-day spawn curve for the shop simulators.

## 3. How do games handle full capacity (queues outside, waiting, spawn pauses)?

### Takeaway
There are three documented approaches. (1) **Queue outside with shared, escalating impatience** (PlateUp!: a shared queue patience bar that decays 10% faster per extra queued group; Kebab Chefs: people wait outside). (2) **Pause spawning at a concurrency cap** (TCG Card Shop Simulator: spawns resume when someone leaves). (3) **Customers never leave but the wait is scored** (Papa's loses Order Station points; Good Pizza Great Pizza gives a happiness penalty when a line forms; Diner Dash has a line with hearts that the podium refills).

### Cited Findings
- PlateUp! seating priority on spawn: a free Dining Table, then a Hosting Stand, then a Coffee Table, and otherwise "they must Queue outside". "Customers in smaller groups may skip ahead in the queue if a table becomes available which cannot seat the larger groups in front of them." — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- PlateUp! queue patience: "All queuing customer groups share a single patience bar." Decay rate = `TimeOfDay(1|2) × Weather(1|1.5|2) × Players(0.75|1|1.1|1.15) × Queuers(1.1^groupsInQueue, cap 5) × Exclusive(0.75|1)` per second, starting from 100. When a group gets in, patience rises by 10 s' worth of decay. When the queue empties, it resets to 100. — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- PlateUp! base patience per phase (1p / 2p): Queuing 120 s / 90 s by day and 60 s / 45 s by night. Waiting for Table 200 s / 150 s. Service 200 s / 150 s. Waiting for Food 120 s / 90 s. Delivery 20 s / 15 s, plus a per-dish bonus. Thinking 2.5 s and Eating 3 s are fixed. Running out of any patience bar immediately ends the run unless an Extra Life is active. — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- PlateUp! tables are checked at spawn. Customers "will start walking towards a Dining Table if it is clear of items at any moment after they spawn". The Preparation Phase cannot end until at least one table fits the largest group size. — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- TCG Card Shop Simulator pauses spawns: "new customers will stop spawning once the [store] nears maximum capacity … Once a customer leaves, a new one can then spawn." Customers who sit at play tables use up part of that limit. — [Game Rant](https://gamerant.com/tcg-card-shop-simulator-attract-more-customers/). The cap is clamped to 3–28 in code. — [TCG Wiki: Mechanics and Formulas](https://tcgcardshopsimulator.wiki.gg/wiki/Mechanics_and_Formulas)
- TCG checkout queue choice follows code logic: an empty player-manned register first, then an empty NPC register, then the shortest queue, then a random register. Customers swap lines if another queue is shorter. [datamined] — [Mechanics and Formulas](https://tcgcardshopsimulator.wiki.gg/wiki/Mechanics_and_Formulas)
- Kebab Chefs: players report "Only 4 customers at a time" and "20 People standing outside" when a day expects 133. The same thread treats arriving "one at a time" as a bug. [player] — [Steam: Customers come in one at a time](https://steamcommunity.com/app/1001270/discussions/0/4414172638909488369/?l=english)
- Papa's: waiting is penalized, not fatal: "if a customer waits in line too long before the player clicks the 'Take Order' Button, or if it takes too long for the player to prepare their order, the player will lose Order Station points". Lobby decorations raise patience. — [Order Station](https://fliplinestudios.fandom.com/wiki/Order_Station); [Steam guide: Freezeria Deluxe](https://steamcommunity.com/sharedfiles/filedetails/?id=2956785457). The Doorbell upgrade lets you "hear when new customers enter, no matter where you are". — [Papa's Pizzeria Deluxe](https://fliplinestudios.fandom.com/wiki/Papa's_Pizzeria_Deluxe)
- Good Pizza Great Pizza: "When there is a visible line of customers, then they have the 80% happiness penalty for having to stand there before having their order taken" (player sleepygeckos; the thread reports this as a possible bug). [player] — [Steam: Customers Bug](https://steamcommunity.com/app/770810/discussions/0/594022118134401039/)
- Diner Dash: customers wait in a line at the entrance with heart meters. Level 2-4 says "if we stand at the podium until the heart fills up, it will make everyone in line happier". — [Walkthrough: Flo's Tiki Palace](https://dinerdash.fandom.com/wiki/Walkthrough:Flo's_Tiki_Palace_(Diner_Dash)); [Diner Dash Wiki](https://dinerdash.fandom.com/wiki/Diner_Dash)

### Inferences
- For a 3-slot shop, the two practical templates are: (a) **TCG-style pause** (never spawn when 3 slots are full, and restart the 26 s timer when a slot frees), or (b) **PlateUp-style outside queue with one shared patience bar** that decays faster per extra queued customer (×1.1 each) and resets when the queue clears. Option (a) makes the day easier and the daily count variable. Option (b) keeps the daily count fixed and turns overflow into pressure.
- PlateUp!'s queue patience (90 s for 2 players, 120 s solo, by day) is close to the target's 90 s wait, and so is its Waiting for Food patience (90 s / 120 s). This is evidence that ~90 s is a genre-typical patience window for a 25–26 s arrival density.

### Gaps
- I found no data on whether Supermarket Simulator or Kebab Chefs pause spawns at capacity or only queue customers. There is also no data on Papa's lobby limits: whether arrivals pause when the order line is long.

## 4. How do upgrades or progression change the arrival rate (reputation, ads, decoration, stars)?

### Takeaway
Progression most often raises the **daily count** or the **concurrency cap**, not the interval directly. The drivers are: day number (PlateUp!), rank up to a hard cap (Papa's), store level (Supermarket +1 per level, TCG with diminishing returns), menu breadth × dish stars (Kebab Chefs), a paid "extra customers" upgrade (Cooking Fever +1–3 per level), advertising cards (+25% in PlateUp!) and popularity or regulars (Kairosoft). PlateUp! notably also *reduces* customers for harder menus, which keeps the workload constant.

### Cited Findings
- PlateUp! modifiers to expected groups: starting recipe +30% (Burgers, Coffee), +15% (Salad, Tacos, Hot Dogs), −15% (Turkey, Pies, Fish, Spaghetti, Stir Fry), −30% (Dumplings). Food cards −15% (−30% for Apple and Potato Salad). Two courses −20% and three courses −33.3%. Advertising +25%. Double Helpings and Leisurely Eating −15%. All You Can Eat −30%. Franchise cards +30%. — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- PlateUp! group-size cards (Medium Groups 2–4, Large Groups 4–6, Individual Dining 1–1, Flexible Dining 1–5) "Does not affect total number of customers". Only the number of groups changes. — [Cards](https://wiki.plateupgame.com/Cards)
- PlateUp! also scales customers with the number of players (80/100/125/150%). — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- Papa's per-day count reaches its cap at a specific rank (for example 10 customers from Rank 10 in Pizzeria, 7 from Rank 16 in most later games). Star Customers (Bronze, Silver, Gold) give orders and responses faster, so throughput rises without more arrivals. — [Order Station](https://fliplinestudios.fandom.com/wiki/Order_Station). Papa's Pizzeria Deluxe sells a Special Sign ($250) for "More customers will order Today's Special!" This changes the order mix, not the count. — [Papa's Pizzeria Deluxe](https://fliplinestudios.fandom.com/wiki/Papa's_Pizzeria_Deluxe)
- Supermarket Simulator: 20 customers + 1 per store level [player] — [Steam](https://steamcommunity.com/app/2670630/discussions/0/4518884147854988828/). Players describe a soft cap around 60–65 near level 40–50 ("the scaling really falls off after lvl 40"). One player at level 70+ sees "always 58 max customers no matter how big the store is", while another claims expansions raised it to 74 at level 52. These player reports conflict. — [Steam: About customer amount](https://steamcommunity.com/app/2670630/discussions/0/4289188517338864417/); [Steam: How do I get more customers per day?](https://steamcommunity.com/app/2670630/discussions/0/4289187252715721945/). Players also report that shelf quantity or stock level does not raise the count: "Putting shelves etc and loads and loads of stock does not work". — [same thread](https://steamcommunity.com/app/2670630/discussions/0/4289187252715721945/)
- TCG Card Shop Simulator: in the decompiled loops, each additional +1 from shop level needs one more level than the last (a "triangular" pattern), and expansions add 1.25 per step in the same pattern. Seated play-table customers add ceil(n/2). The total is clamped to 3–28. [datamined] — [Mechanics and Formulas](https://tcgcardshopsimulator.wiki.gg/wiki/Mechanics_and_Formulas). "Expanding the store is the best way to increase customer traffic." — [Game Rant](https://gamerant.com/tcg-card-shop-simulator-attract-more-customers/)
- Kebab Chefs: "The more meals you have + the more stars meal has = more customers." One player reported ~60 per day. — [Steam: Work hours](https://steamcommunity.com/app/1001270/discussions/0/4297067919527527744/)
- Cooking Fever: interior upgrades "increase a customer's waiting time, tip time, and tip amount. It also provides an option for extra customers which is the most expensive upgrade." — [Cooking Fever Wiki: Sunset Café](https://cookingfever.fandom.com/wiki/Sunset_Caf%C3%A9). The extra-customers upgrade adds 1 to 3 customers per level [snippet] — [Cooking Fever Wiki: Scoreboard (Sports Bar)](https://cookingfever.fandom.com/wiki/Scoreboard_(Sports_Bar)). Official account: "you can get extra clients to help you achieve 3 stars, by purchasing the scoreboard for your interior". [official] — [Cooking Fever on X](https://x.com/cookingfever/status/752890214914326528)
- Kairosoft Cafe Master Story: regulars "visit more often". Regularity "will also make your cafe more popular". The Specials Board boosts popularity. Employee "Affinity: Helps Boost the Popularity of your cafe". Cafe rank increases "help increase your Popularity". [official manual] — [Kairosoft Wiki: Manual (Cafe Master Story)](https://kairosoft.fandom.com/wiki/Transcript:Manual_(Cafe_Master_Story))
- Papa's Customer Coupons force a particular customer to appear within 2 days, one per day. — [Customer Coupons](https://fliplinestudios.fandom.com/wiki/Customer_Coupons)

### Inferences
- A pattern for a small shop: grow the *daily count* (or shorten the interval) with a slow, capped curve, as with Papa's hard cap, Supermarket's soft cap near level 40–50 and TCG's triangular steps. Also offer one expensive "+1 customer" upgrade (Cooking Fever) or a "+25% advertising" option (PlateUp!).
- PlateUp!'s negative modifiers for complex menus are a useful balancing idea. If cotton-candy recipes get more complex (more steps per order), reduce arrivals proportionally so the workload stays flat.
- Throughput upgrades (Papa's faster-ordering star customers, Cooking Fever's longer waiting and tipping time) are an alternative to raising arrivals. They make the same arrival rate easier instead of adding customers.

### Gaps
- I found no source that says **price** reduces customer arrivals in these games. In TCG, price affects whether a customer buys, not whether they spawn, and I found no spawn-side price effect. The Supermarket Simulator threads say licences raise spending per visit but disagree on whether they raise visits.
- I found no numbers for how Kairosoft popularity maps to visitors per unit time.

## 5. Developer statements or postmortems explaining the chosen approach

### Takeaway
Direct developer rationale for *arrival timing* is scarce. The best primary material is Ghost Town Games' Overcooked deep dive (a time-limited round with ramped order counts and deliberate delays), plus official patch notes (Supermarket Simulator) and manuals (Kairosoft). The PlateUp! numbers are community reverse-engineering, not developer statements.

### Cited Findings
- Overcooked: the switch from three lives to a time limit made the game "much more fun and allowed players more breathing room to think". Designers kept "generally always more actions to perform than players available", added "delays: when ingredients were added to a pot, when plates were sent out" to force secondary tasks, and would "gradually ramp up the number of orders". [official, dev article] — [Game Developer: Game Design Deep Dive, Overcooked](https://www.gamedeveloper.com/design/game-design-deep-dive-building-truly-cooperative-play-in-i-overcooked-i-)
- Supermarket Simulator (Nokta Games) changed customer scaling on purpose in patch v0.1.2.3: "Increased customer count gap to increase gradually with store level." [official] — [Steam announcement](https://steamcommunity.com/games/2670630/announcements/detail/4187858160663536133)
- PlateUp! changed rush sizing in v1.3.0 from Customer Count to Expected Group Count, because larger group sizes had caused "huge influxes of customers per Rush". The source is the wiki's record of the change, not a developer quote. — [Customers](https://wiki.plateupgame.com/gameplay/Customers)
- PlateUp! wiki rationale: "A longer day length allows customer groups arrival time to be more spread out, which gives the player more time to serve customers before a potential patience bar depletes." — [Daily Operations](https://wiki.plateupgame.com/gameplay/DailyOperations)
- Diner Dash designer Nick Fortugno describes the game as simple "one-click flow organizing" whose complexity comes from level design and tougher customers. [snippet from podcast page] — [Open Culture: Pretty Much Pop #46](https://www.openculture.com/2020/06/what-is-a-casual-game-pretty-much-pop-a-culture-podcast-46-talks-to-nick-fortugno-creator-of-diner-dash.html). His GDC 2007 casual-design lecture is referenced here: [Game Developer: Four casual design lessons from Diner Dash's creator](https://www.gamedeveloper.com/design/video-four-casual-design-lessons-from-i-diner-dash-i-s-creator). The page itself carries no transcript.
- Good Pizza Great Pizza reviewers frame the single-file queue as a deliberate difference from multitasking cooking games: it "mercifully dispenses with the multitasking". [review] — [Tech-Gaming](https://www.tech-gaming.com/good-pizza-great-pizza/)

### Inferences
- The shared design logic across sources is to hold *workload per unit time* roughly constant while adding variety. PlateUp! does this by scaling day length with count and cutting customers for complex menus, Overcooked by ramping order count per level, and Papa's by capping daily customers at 6–10 while order complexity grows. For a cotton-candy shop, this argues for tying the arrival interval to expected service time rather than tuning it on its own.

### Gaps
- I found no PlateUp! developer (It's Happening) statement explaining the even-spread scheduling. It may exist in Discord or dev streams, which I did not access.
- I found no Flipline blog post explaining the 6–10 customers-per-day cap or the arrival pacing. Blog archives such as flipline.com/blog were not searched in depth.
- I found no postmortem for Diner Dash level scripting, Cooking Fever level tuning, Kebab Chefs or TCG Card Shop Simulator spawn design.
