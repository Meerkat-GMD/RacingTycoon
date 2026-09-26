# Continuous race and shop

The requested prototype keeps racing on the left and sales on the right at the same time. The default is automatic driving so the player can focus on sales; a visible toggle retains manual driving. Existing cars, courses, candy production, orders, upgrades, and save format are reused.

The centered 1600 × 900 composition uses a 960-pixel racing area and a 640-pixel shop area. The shop contains a live counter preview, two customer orders, the inventory, and tabs for next-production settings and upgrades. Race and shop remain visible together across supported window sizes.

A completed lap deposits one candy and its existing completion reward, then immediately begins the next production cycle. There is no results-screen interruption. Same-course cycles preserve vehicle motion. A full shelf suspends production while the vehicle keeps driving; freeing a slot resumes production without credit for driving during the full-shelf interval. Sales, inventory selection, discarding, and upgrades work during a lap. Map, flavor and vehicle selections apply to the next product. Pause freezes driving and customer timers.

Verification covers continuous laps, sales while moving, duplicate-sale rejection, full inventory and resumption, queued settings, pause, save reload, both driving styles, and viewport/UI bounds at common aspect ratios. Existing legacy tests remain available through an explicitly noncontinuous initialization path.
