# Game State reports

Produced by **`/game-state`** (see `.claude/commands/game-state.md`).

| File | Who | Notes |
|------|-----|--------|
| `LATEST.md` | You | Newest human-readable survey (overwrite each run) |
| `LATEST_Agent.md` | Agents | Newest structured survey (overwrite each run) |
| `Game_State_YYYY-MM-DD_HHmmss.md` | You | Dated archive |
| `Game_State_Agent_YYYY-MM-DD_HHmmss.md` | Agents | Dated archive |

These are observations, not a `/playtest` harness result. Agents should prefer `LATEST_Agent.md` for “what’s true right now,” then confirm in Unity before treating visual claims as done-when.
