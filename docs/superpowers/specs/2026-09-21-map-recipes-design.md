# Map recipe racing design
User approved on 2026-09-21 with “반영해줘”. This supersedes the short order-production run and lane-flavor design.

- Map 1 always makes small 60g cotton candy; map 2 always makes large 120g candy. Both offer strawberry/soda/vanilla before departure. The selected flavor never changes with road color or lane.
- Selecting an order sets map=size and flavor=order.Flavor; the player can edit preparation for stock. Do not lose manual order serving or existing stock/coins/upgrades.
- Two distinct full circuits: map1 approximately 750–850m (45–60sec at base kart), map2 approximately 1050–1200m (60–90sec). Long straights, broad turns, alternating bends, a shortcut. One forward lap completes production, never target weight alone. Generous timeouts180/240sec; abort/timeout yields no saleable product or completion reward.
- The driven path still supplies winding radius/samples. Each completed candy uses only configured flavor. Drift boosts and clean driving contribute quality; quality affects sale price up to30%. One-time completion/record bonus is shown at results. Existing products have quality0 and unchanged value.
- Customer patience300sec; old V2 order remaining time scales to keep its prior satisfaction ratio. SaveV3 reads V1/V2; invalid saves stay protected.
- Blender adds a candy tunnel and finish marker; map roots toggle, minimap follows selected course, machine and preview framing fit larger maps. Shop remains a separate visible area.
- UI explicitly shows map/size/flavor, lap distance, fixed flavor, quality and finish rule. Race Enter requests confirmed abort, never grants an early completed candy.
- Verify two maps ×three flavors via actual kart steering, target-full-before-finish behavior, premature abort/no reward, one-time completion reward, matching manual sale, persistence, pause, layout and Blender exports.
