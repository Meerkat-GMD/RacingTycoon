# Order iteration review

Baseline: fd9688a. Reviewed current working tree on 2026-09-21 with an independent reviewer under requesting-code-review.

Scope: recipe matching, atomic manual handover, shelf capacity, partial-stock discard, order arrival/expiry and paused/results time, targeted production, same-map collection/bypass, V1/V2 save handling, and UI action guards.

Result: no material correctness issues found. Reviewer independently reran the 22 core, 17 driving, 14 order and 9 collection checks. Rendered UI and Unity playability are checked separately by the parent in the Windows player; this review is not a substitute for those checks.
