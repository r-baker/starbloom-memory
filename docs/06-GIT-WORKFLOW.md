# Git Workflow & Versioning

Repo: https://github.com/r-baker/starbloom-memory (deliberately not an
obvious Gundam name — see `04-COPYRIGHT-GUIDELINES.md` for the discretion
rationale.)

## Rule: never push directly to `main`

`main` holds only **fully tested** work. All development happens on a
branch, gets tested in-game there, and only merges to `main` once the
feature actually works.

```
git checkout -b feature/<thing>     # branch for the work
# ...build, deploy, test in-game, iterate, commit as you go...
git checkout main
git merge feature/<thing>
git tag -a vX.Y.Z -m "..."          # tag the merge, see scheme below
git push origin main --follow-tags
```

Commits on a feature branch can be pushed freely (that's the backup
safety net — the whole reason the repo exists). Only the **merge to
`main`** is gated on "this is tested and works."

## Versioning — `MAJOR.MINOR.PATCH`

| Part | Increments when |
|---|---|
| **1.0.0** | The full One Year War is complete, tested, and genuinely playable start to finish. This is the goal, not a milestone along the way. |
| **0.X.0** | A full roadmap point (`01-ROADMAP.md` Step N) is complete and tested. |
| **0.0.X** | A small successful test/upgrade *within* a roadmap point — e.g. one proven slice of a multi-slice step. |

So Step 6's slices tag as `0.0.1`, `0.0.2`, … as each is verified, and
completing Step 6 entirely bumps to `0.2.0` (Step 5 having been the
previous completed point). Tag only after in-game verification — a tag
means "this worked," not "this compiled."

## Known deviation: the initial commits

`8fe2f13` (initial import) and `67619b3` (Step 6 slice 1) were pushed
directly to `main` **before this workflow was established**, and slice 1
was untested at that point. History is deliberately not being rewritten to
fix that — force-pushing a shared repo to tidy two commits is worse than
the inconsistency. Branch discipline starts from
`feature/step6-hour-time` onward.

## Open item: full 24-hour clock

Step 6 slice 1 ships a **6-hour** tick, not a true 1-hour one. That is a
frame-rate safety constraint, not a design preference — the full reasoning
is in `03-TECHNICAL-NOTES.md` under the skip-original Prefix section
(short version: a 1-hour tick would put the fast-speed threshold below a
single 60fps frame, making campaign time frame-rate dependent).

**A genuine 24-hour clock is still wanted** and should be revisited. The
likely path is decoupling the hour advance from `Update()`'s
reset-to-zero accumulator entirely — e.g. driving hours from accumulated
`DeltaTime` with the remainder carried rather than discarded, so the tick
rate stops being bounded by frame time. Not attempted yet; needs its own
research pass before anyone tries it.
