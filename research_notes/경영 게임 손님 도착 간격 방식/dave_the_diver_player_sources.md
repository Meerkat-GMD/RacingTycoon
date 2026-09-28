# Dave the Diver (Mintrocket/Nexon, 2023): Bancho Sushi customer arrivals, from player-facing and developer-facing sources

Confidence tags used below: **[DEV]** developer statement; **[WIKI]** community wiki (Namu Wiki / Fandom). The wikis do not say their numbers come from game data, and they appear to be player-counted. **[COMMUNITY]** Steam, DCInside or Ruliweb player observation; **[LOW]** an aggregator or SEO guide of doubtful reliability, possibly AI-written; **[INFERENCE]** my own reasoning. No official Mintrocket document gives the arrival algorithm. No decompilation or mod source came up during this search.

## 1. Is the number of customers per night fixed or variable, and what determines it?

### Takeaway
Each Cooksta rank has a roughly fixed nightly quota of customers, with small random variation (about ±1 to 3). The quota does not come from menu, décor, money or stock. Night diving cuts it by roughly 1/3 to 2/5. The game never shows the number, and players have to learn it (the most-cited values are Gold 28, Platinum 36, Diamond 45). The branch restaurant has its own count, which comes from the branch manager's stats and not from Cooksta rank.

### Cited Findings
- [WIKI] Korean Namu Wiki (current): "쿡스타 랭크별 랭크 업 조건과 해금 요리, 일일 손님 수는 다음과 같다. 손님 수는 간혹 덜 오거나 더 오기도 한다." ("Daily customer count per Cooksta rank is as follows. Occasionally fewer or more customers come.") — [Namu Wiki, 데이브 더 다이버/식당 운영](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [WIKI] Namu Wiki daily customer table. The value in parentheses is the count after a night dive: Bronze 14, Silver 24, Gold 28 (17), Platinum 36 (21–25), Diamond 45 (27). "No rank" shows "-" in the Korean page. — [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [WIKI] The English Namu Wiki mirror/translation, probably an older revision, has the same table with "No Rank 10" and describes the night-dive figures as a "one-third decrease" / "a little less than 2/3". — [en.namu.wiki Dave the Diver/Restaurant](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [COMMUNITY] DCInside post "[정보/공략] 초반 손님 수": 등급X (no rank) 8, Bronze 14, Silver 20. This conflicts with Namu's Silver 24 and No-rank 10. — [DCInside #7193](https://gall.dcinside.com/mgallery/board/view/?id=davethediver&no=7193)
- [COMMUNITY] Steam user Asmodan gives fixed maxima of Beginner 8 / Bronze 15 / Silver 22 / Gold 29 / Platinum 36 / Diamond 43. Honorable_D replies "Diamond is 45." AquaX: "Diamond is 42-45. Platinum I think is 32-36." Jeff says that with staff around level 15 "diamond will be 45 customers every single time." resonance, at Diamond, gets "45 every full night, even on parties." skyshroudsylvan: "about 40 people on a full day and 28 on a day that you night dive." — [Steam: "Please show the number of customers"](https://steamcommunity.com/app/1868140/discussions/0/3806156528943742319/)
- [COMMUNITY] Steam RandomTurtle: "at gold rating you get 28 customers or 21 if you go night diving" and "Right now at platinum I get 36 customers a night". Customer count "depends on your cooksta rating, and if you've been out on a night dive or not". — [Steam discussion 3805027459316761930](https://steamcommunity.com/app/1868140/discussions/0/3805027459316761930)
- [COMMUNITY] Steam user Onyx: "The number of customers per night is fixed" and varies by Cooksta tier. Onyx refers to a Steam guide with a table. — [Steam: "What am I missing in the restaurant?"](https://steamcommunity.com/app/1868140/discussions/0/4142816719795707443/)
- [COMMUNITY] DCInside post "의외로 뉴비들이 손님 수 정해져 있는거 모르더라" ("Newbies don't know the customer count is fixed") lists Gold 28, Platinum 36, Diamond 45 and warns "손님 수 초과된 요리는 폐기처분 돼서 재료 낭비야" ("dishes beyond the customer count are thrown away, wasting ingredients"). A commenter asks: "플래티넘 야간 최대 손님 25명 아닌가?" ("Isn't Platinum's night-dive max 25?"). — [DCInside #6698](https://gall.dcinside.com/mgallery/board/view/?id=davethediver&no=6698)
- [COMMUNITY] A Ruliweb guide ("간략공략 펌") says the daily count is fixed by rank ("골드 28명, 플래티넘 36명, 다이아몬드 45명"). It recommends listing one expensive dish in the quantity of the customer count. — [Ruliweb](https://bbs.ruliweb.com/game/86358/read/2519889)
- [COMMUNITY] A DCInside post quoting an earlier Namu revision gives night-dive values of Gold 28→21, Platinum 36→22, Diamond 45→23. The poster computes losses of 25%, about 39% and about 49%, and asks whether night diving becomes unprofitable at high rank. — [DCInside #8709](https://gall.dcinside.com/mgallery/board/view/?id=davethediver&no=8709)
- [COMMUNITY] Steam, endgame: "The most customers you can have is 45 at each location (or 90 daily)." Also "an average of 45 per restaurant (sushi + branch)". — [Steam discussion 3805027864685575040](https://steamcommunity.com/app/1868140/discussions/0/3805027864685575040)
- [LOW] The Nerd Stash says the count "vary every single night" with no warning, that it is "determined solely by your Cooksta rating", and that décor, dishes and staff stats do not change visitor volume. — [The Nerd Stash](https://thenerdstash.com/dave-the-diver-how-many-customers-per-night-come-to-the-bar-answered/)
- [WIKI/LOW] Cooksta rank-up needs a follower count, a best single-dish taste rating and a number of researched recipes. Followers affect customers only through rank. — [The Nerd Stash](https://thenerdstash.com/dave-the-diver-how-many-customers-per-night-come-to-the-bar-answered/); search snippet from [Fandom Bancho Sushi](https://dave-the-diver.fandom.com/wiki/Bancho_Sushi) (the full page returned HTTP 402 and could not be read)
- [COMMUNITY] Players want the count shown. Tray asks for it because "leftover prepared sushi is wasted". Ignosius calls fixed-but-hidden counts a "legitimate design flaw" that "just encourages looking up what that number is rather than feeling organic." — [Steam: "Please show the number of customers"](https://steamcommunity.com/app/1868140/discussions/0/3806156528943742319/)
- [WIKI] Branch (2호점): "분점을 방문하는 손님 수는 쿡스타 랭크와는 무관하기에 본점이 플레티넘 이하일 경우 5랭크를 달성한 분점의 손님 수가 더 많은 경우도 발생한다." ("Branch customers are independent of Cooksta rank, so a rank-5 branch can get more customers than a main shop at Platinum or below.") — [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [WIKI, snippet only] "The Manager's total stats determine both the total number of customers that will visit the Branch that night, as well as the maximum rank of Ingredients". A manager Cooking stat of 450+ gives branch rank 9. — search snippets from [Fandom Sushi Staff](https://dave-the-diver.fandom.com/wiki/Sushi_Staff) / [Sushi Staff Strategy](https://dave-the-diver.fandom.com/wiki/Sushi_Staff_Strategy) (full pages returned HTTP 402), with a second source in [GameRant branch manager guide](https://gamerant.com/dave-the-diver-best-manager-branch-staff/) (not fetched)

### Inferences
- [INFERENCE] Best reading: each night has a quota N = base(rank), sometimes jittered by a few customers. Night diving reduces it, and fog replaces part of it (see Q6). The figure players read as "N" (45 at Diamond) is a cap that is reached only if service keeps up; see Q2 and Q4.
- [INFERENCE] The published tables disagree (Silver 20/22/24; No-rank 8/10; Diamond 43/45; night-dive Diamond 23/27). This fits randomized small variation plus a throughput cap, not one exact constant. Namu's "간혹 덜 오거나 더 오기도 한다" says the same.
- [INFERENCE, design lesson] Hiding a fixed, discoverable count created a meta-game of looking it up plus food-waste frustration. The game's partial fix is the "auto-supply" option (see Q3).

### Gaps
- No official or developer source confirms the per-rank numbers or the size of the random jitter.
- Whether "No rank" is 8 or 10 and Silver is 20, 22 or 24 is unresolved; the sources conflict.
- Exact branch customer-count formula (which manager stats and what mapping) was not read, because the Fandom pages returned HTTP 402.
- No source says marketing items, décor or Cooksta followers raise the nightly count directly. They appear to act only through rank-up.

## 2. Do customers arrive at fixed intervals, randomized intervals, in waves, or on a script? Is there a rush?

### Takeaway
No public source documents the spawn interval. The strongest community model is seat-refill: customers keep entering to fill a limited number of seats during a timed evening. So the faster seats are freed, the more of the nightly quota gets through. No source mentions a scripted rush or peak phase.

### Cited Findings
- [COMMUNITY] Steam user TheClosetSkeleton: "The night is on a timer and your popularity will determine how many seat are filled at the same time, so the faster you serve them, the more new customer come". Also "the max number of customer at the same time is what is affected by the rating. The quality and speed of service will determine how many customer you get within the time limit". — [Steam discussion 3805027459316761930](https://steamcommunity.com/app/1868140/discussions/0/3805027459316761930)
- [COMMUNITY] In the night-dive thread, Hentaika says each rank has a maximum count per night with "tiny variation". The real limit is "service speed, not … the rating directly", and staff stats only raise the "speed multiplier of actions". TheClosetSkeleton adds that actual volume "depends on your service" speed. — [Steam: "Diving at night"](https://steamcommunity.com/app/1868140/discussions/0/3805027459319213248/)
- [COMMUNITY] Jeff: with highly levelled staff (about lvl 15), Diamond gives "45 customers every single time". This implies weaker staff sometimes fall short of the cap. — [Steam: "Please show the number of customers"](https://steamcommunity.com/app/1868140/discussions/0/3806156528943742319/)
- [COMMUNITY] One new player served "no more than 2-3 customers" in a night, which shows that throughput limits the realized count. — [Steam: "What am I missing in the restaurant?"](https://steamcommunity.com/app/1868140/discussions/0/4142816719795707443/)
- [LOW] "Only a certain amount of seats will be filled at any one given time, and it's the player's job to get them out of their seats as fast as possible" (search-engine summary of Steam/Namu results; exact origin unverified). — search results for "Dave the Diver customers enter restaurant one at a time waiting line", e.g. [Steam 3806156528943742319](https://steamcommunity.com/app/1868140/discussions/0/3806156528943742319/)
- [WIKI] Flow within a visit: menu set before opening → customers sit and order → tea or drinks → food served → the table must be cleared before the next customer sits. An uncleared table when the next customer sits raises dissatisfaction. — [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81); [en.namu.wiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [DEV] Director Jaeho Hwang: separating the day (dive) and night (restaurant) stages "really made a difference" and gave the feeling of "preparing dinner for customers". He does not describe arrival pacing. — [Game Developer interview](https://www.gamedeveloper.com/design/dave-the-diver)

### Inferences
- [INFERENCE] The likely architecture is a fixed quota N per night, a seat cap S (simultaneous customers, grows with rank), and a service timer T. A new customer enters when a seat is free and there is quota left, possibly after a short delay. Realized count = min(N, what fits in T given turnover). This matches every community statement ("fixed count", "faster service → more customers", "45 every time with good staff", "2–3 customers for a new player").
- [INFERENCE] The night-dive numbers point to a time-truncated flow and not a flat multiplier. If night diving simply removed 1/3 of time with uniform arrivals, you'd expect about 19/24/30 for Gold/Plat/Diamond. Reported values of about 21/22–25/23–27 mean higher ranks lose proportionally more. That fits a throughput (seats × turnover) limit in the shortened window.
- [SPECULATION] No evidence either way on whether customers come in parties (groups at one table) or on randomized versus fixed spawn gaps.

### Gaps
- No source gives spawn intervals in seconds, whether they are random or fixed, or a peak/rush curve.
- No source says whether customers queue outside when seats are full. The community wording ("seats filled at the same time") suggests they simply don't spawn until a seat frees, but this is unconfirmed.
- No NDC or GDC talk covering restaurant flow was found. The Steam guide Onyx mentions, which has a customer table, was not identified.

## 3. How do seats/tables limit arrivals, and what happens when all seats are full?

### Takeaway
Seats cap how many customers are inside at once, and the cap grows with rank. There are community claims of rank-linked expansion, but no confirmed seat counts were found. The branch reportedly has about twice the tables. No source describes a visible waiting line. The practical limit is turnover: eating, then clearing the table before the next guest.

### Cited Findings
- [COMMUNITY] "Your popularity will determine how many seat are filled at the same time" and "the max number of customer at the same time is what is affected by the rating". — [Steam, TheClosetSkeleton](https://steamcommunity.com/app/1868140/discussions/0/3805027459316761930)
- [LOW] A search summary says "as your rank increases, the number of tables and visiting guests also increases" (origin unclear, probably Namu or a guide). A Switchblade Gaming guide mentions "Restaurant seating expansion … More tables means more covers per evening" without numbers. That guide is generic and possibly AI-written, so treat it with caution. — [Switchblade Gaming](https://www.switchbladegaming.com/cozy-games/dave-the-diver-guide/)
- [LOW] The branch provides "twice as many tables to work with" as the main restaurant; no numbers are given. — [ESTNN branch guide](https://estnn.com/dave-the-diver-branch-guide/)
- [WIKI] The kitchen can prepare at most 5 plates at once, however many customers are waiting ("최대 5접시"). — [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [WIKI] Tables with leftovers must be cleaned (25 gold per cleanup). If a table isn't cleared before the next customer sits, dissatisfaction rises. — [en.namu.wiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81); [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [COMMUNITY] The "auto-supply" menu option makes dishes only when customers order, which avoids waste from over-preparing against an unknown count. — [Steam, Knight_of_Gallifrey](https://steamcommunity.com/app/1868140/discussions/0/4142816719795707443/)
- [WIKI/COMMUNITY] Unsold prepared dishes are thrown away at closing ("팔지 못한 요리는 폐기"). — [Namu Wiki](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81); [Ruliweb](https://bbs.ruliweb.com/game/86358/read/2519889)

### Inferences
- [INFERENCE] Arrivals appear to be gated by free seats, not queued. Full seats reduce the realized count only through the timer: fewer turnovers before closing.

### Gaps
- Exact seat or table counts per rank, for the main shop and the branch, were not found.
- No source describes a door queue, customers waiting at the entrance, or customers who give up because the shop is full.

## 4. How long is a service, how many are served, and how does it scale with rank and branch?

### Takeaway
Service is a timed evening of unknown real-time length. Night diving uses about the first third of it. Realized counts run from about 8–10 (no rank) to 45 (Diamond) per location, so about 90 per day with a fully developed branch. Branch counts follow the manager's stats.

### Cited Findings
- [COMMUNITY] "If you do a night dive, it uses up 1/3 of the evening time"; "roughly a third less customers" (Bastard™); "You lose 1/3 of the available selling time" (TheClosetSkeleton); "The sushi bar isn't open for as long when you do a night dive" (smack). — [Steam: "Diving at night"](https://steamcommunity.com/app/1868140/discussions/0/3805027459319213248/)
- [WIKI] If you dive at night, "you will start operating the restaurant with a one-third decrease" in customers. The Korean page gives night-dive counts Gold 17, Platinum 21–25, Diamond 27. — [en.namu.wiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81); [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [COMMUNITY] Conflicting night-dive values elsewhere: Gold 21 (RandomTurtle); Gold 21 / Plat 22 / Diamond 23 (older Namu per DCInside #8709); "28 on a day that you night dive" versus about 40 full (skyshroudsylvan). — [Steam 3805027459316761930](https://steamcommunity.com/app/1868140/discussions/0/3805027459316761930); [DCInside #8709](https://gall.dcinside.com/mgallery/board/view/?id=davethediver&no=8709); [Steam 3806156528943742319](https://steamcommunity.com/app/1868140/discussions/0/3806156528943742319/)
- [COMMUNITY] HoneyDrake suggests highly levelled staff can sell out even in the shortened night-dive window. — [Steam: "Diving at night"](https://steamcommunity.com/app/1868140/discussions/0/3805027459319213248/)
- [COMMUNITY] Endgame cap: 45 per location, 90 per day with the branch. — [Steam 3805027864685575040](https://steamcommunity.com/app/1868140/discussions/0/3805027864685575040)
- [WIKI] A rank-5 branch can exceed a main shop at Platinum or below, because branch count ignores Cooksta rank. — [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [LOW] Branch operating policies: Price / Quality / Level. Recommended manager stats are Cooking 450+ and high Serving. — [ESTNN](https://estnn.com/dave-the-diver-branch-guide/)

### Inferences
- [INFERENCE] Growth from rank to rank is roughly linear, about +4 to +10 customers per rank (14→24→28→36→45 per Namu). Progression is expressed as a bigger nightly quota plus more seats, not as faster spawning in a fixed window.

### Gaps
- Real-time length of a night service (minutes) was not found in any source.
- No confirmed branch customer-count table per manager stat or branch rank.

## 5. Patience and leaving: how long customers wait, what reduces patience, how tea/drinks interact

### Takeaway
Each customer has a visible patience meter for their current request (tea, drink or food). If the meter fills before the request is met, the customer leaves. How full it was when served does not change the payment. Uncleared tables and slow food raise dissatisfaction. Tea and drinks are revenue and satisfaction add-ons, not patience resets, as far as the sources say.

### Cited Findings
- [COMMUNITY] Onyx: "Customers have a patience meter. If their current order … is not satisfied by the time the red rises to the top of the bubble, they'll leave." Also there is "no difference between a customer being served at 90% red vs no red". — [Steam: "What am I missing in the restaurant?"](https://steamcommunity.com/app/1868140/discussions/0/4142816719795707443/)
- [COMMUNITY/press] "Each customer has a patience meter that fills up, often too rapidly, causing the customer to leave in frustration". The game has no difficulty setting, and the reviewer hoped for an easy mode where "customers have more patience". — [Can I Play That? accessibility review](https://caniplaythat.com/2023/09/07/dave-the-diver-accessibility-review/)
- [WIKI] Customers become dissatisfied if food delivery is late and leave angry if service takes too long. Cleaning is a priority because an uncleared table raises the next customer's dissatisfaction. — [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [WIKI] Drinks: green tea before the meal; beer or cocktails after the meal (drinks priced about 1.5×, per the English mirror's summary). Tips of about 10% depend on taste and staff appeal. — [en.namu.wiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [WIKI, snippet only, unverified] Some customers ask for green tea before ordering; filling the cup near the line ("good" or better) gives a green aura and the customer pays 30% more for the meal. — search snippet attributed to [Fandom Bancho Sushi](https://dave-the-diver.fandom.com/wiki/Bancho_Sushi) (page returned HTTP 402)
- [WIKI] Running out of wasabi stops food production. Wasabi fills at the start of service, drains per dish and must be ground to refill. — search snippet from [Fandom Bancho Sushi](https://dave-the-diver.fandom.com/wiki/Bancho_Sushi)
- [DEV] Beer pouring after sushi is a mechanic unlocked by satisfying VIPs ("pouring beer after serving sushi"). — [Game Developer, Jaeho Hwang](https://www.gamedeveloper.com/design/dave-the-diver)

### Inferences
- [INFERENCE] The patience meter is per request stage, not per visit: tea → order → food each has its own wait. It is binary in outcome (served or left), so there is no graded penalty for near-timeouts.

### Gaps
- Patience durations in seconds, and whether they vary by customer type or rank, were not found.
- No source says whether the tea refill resets or extends the food-wait meter.

## 6. Special customers (VIPs, parties, critics, fog nights): do they follow the same arrival logic?

### Takeaway
VIPs are scripted, story-flagged visits. They are announced before the day's dive, need a specific researched dish, and must be served by Dave. If missed, they return a few days later. Party nights recolour part of the normal crowd as themed guests (glowing, with a ribbon dish) instead of adding customers. Fog nights swap part of the normal crowd for robed Sea-People customers. No source describes a separate arrival timer for any of them.

### Cited Findings
- [DEV] Hwang: VIP visits "make players go for certain ingredients to treat these VIPs. Once users succeed, they get new mechanics unlocked". The bar is "a place anyone can visit, and like in real life, you meet some unexpected VIPs or very harsh customers". — [Game Developer interview](https://www.gamedeveloper.com/design/dave-the-diver)
- [DEV] Art director 정기엽 (2022, pre-release): "특수 손님도 등장할 예정인데, 일본 풍의 옷을 입은 백인 중년 남성이 VIP로 등장한다…또한 주요 캐릭터의 가족도 손님으로 등장한다." ("Special customers will appear: a middle-aged white man in Japanese clothes as a VIP; main characters' family members also visit.") Hwang cites the drama 심야식당 (Midnight Diner) as the concept source. — [Kukinews interview](https://www.kukinews.com/newsView/kuk202209080094)
- [COMMUNITY, via search summary] You must research the VIP dish and add it to the menu manually before opening. Dave must serve it personally, because staff won't. Missing a VIP has no real penalty, and the VIP comes back after a few days. — [Steam: "Have I ruined my VIP customer request?"](https://steamcommunity.com/app/1868140/discussions/0/3801651941323971454/); [Steam: "What happens when I miss a VIP request dish?"](https://steamcommunity.com/app/1868140/discussions/0/7318962605504290553/); [Steam: "how do you prep meals for special guests?"](https://steamcommunity.com/app/1868140/discussions/0/3805027864677603637/) (threads not fetched in full)
- [WIKI] Parties: themes include jellyfish, tuna, marlin, shark, cucumber, curry, shrimp and lobster. Party guests wear costumes and want theme dishes; an irrelevant menu causes dissatisfaction and a lower rating. — [en.namu.wiki](https://en.namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)
- [LOW] On party nights "some customers who are glowing" are the party guests. They pay a bonus at the end of the night if served properly. — [Sirus Gaming jellyfish party guide](https://sirusgaming.com/dave-the-diver-jellyfish-party-event-guide/)
- [COMMUNITY] Diamond gives "45 every full night, even on parties". — [Steam, resonance](https://steamcommunity.com/app/1868140/discussions/0/3806156528943742319/)
- [WIKI] Fog: "안개 낀 날에는 야간 다이빙 이후 로브들이 20~22접시를 먹는 대신 일반 손님은 14~16명으로 줄어든다." ("On foggy days, after a night dive, the robed [Sea People] customers eat 20–22 plates while normal customers drop to 14–16.") The rank context isn't stated. — [Namu Wiki (KR)](https://namu.wiki/w/%EB%8D%B0%EC%9D%B4%EB%B8%8C%20%EB%8D%94%20%EB%8B%A4%EC%9D%B4%EB%B2%84/%EC%8B%9D%EB%8B%B9%20%EC%9A%B4%EC%98%81)

### Inferences
- [INFERENCE] Special customers appear to be substitutions inside the nightly quota, flagged as a type, not extra spawns. Parties don't change the Diamond count of 45, and fog trades normal customers for robed ones. VIPs are one-off scripted slots tied to story flags.

### Gaps
- Number of party guests per party night, and their share of the quota, was not found.
- No food-critic-style customer was found for the sushi bar in these sources. Namu's check found no "critic" or "influencer" mechanic.
- When in the night VIPs or themed guests enter (first, random or last) is not documented.

## 7. Developer interviews, GDC/NDC talks, patch notes about customer-flow design

### Takeaway
No developer source describes the arrival algorithm. Public developer material covers the concept (Midnight Diner, splitting day and night), VIPs as goal-setters, and narrative and humour, not tuning of customer flow. No NDC talk on Bancho Sushi's customer flow was found.

### Cited Findings
- [DEV] GDC 2024: Jaeho Hwang spoke on "캐릭터와 유머: 데이브 더 다이버를 통해 알아본 문제해결" (character and humour), which was narrative-focused. — [Gamevu GDC coverage](https://www.gamevu.co.kr/news/articleView.html?idxno=31879)
- [DEV] Game Developer interview: day/night separation, VIPs as ingredient goals that unlock mechanics, and "expanding the gameplay loop with new activities to offset a certain level of inevitable repetition". — [Game Developer](https://www.gamedeveloper.com/design/dave-the-diver)
- [DEV] Kukinews 2022: Midnight Diner (심야식당) concept; VIP and character-family customers planned. — [Kukinews](https://www.kukinews.com/newsView/kuk202209080094)
- [DEV, not fetched] Other interviews found but not read, which may cover design philosophy: Hwang on repetition and creativity ([ZDNet Korea 2025](https://zdnet.co.kr/view/?no=20250415112256)); early-access success ([Thisisgame](https://www.thisisgame.com/articles/212601)); In the Jungle DLC interview ([Inven](https://www.inven.co.kr/webzine/news/?news=317574)); healing and arcade balance ([Sisaweek](https://www.sisaweek.com/news/articleView.html?idxno=153429)).

### Inferences
- [INFERENCE] The design intent is a cosy, pre-planned "dinner service" loop. Because the nightly count is fixed and learnable, players' upstream planning (how much fish to catch, how many plates to list) matters more than reacting to arrivals, and the tension comes from patience meters and throughput within the night.

### Gaps
- No NDC 2023/2024 session on Dave the Diver restaurant design was found in searches.
- Steam patch notes were not checked for changes to customer count or patience.
- The Fandom wiki pages (Bancho Sushi, Sushi Staff, Sushi Staff Strategy) returned HTTP 402 and BreezeWiki mirrors returned a bot check, so their full tables (seats, branch formula, tea bonus) could not be verified.
