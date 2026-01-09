# KBTV - Development Roadmap

## Milestone 1: Core Prototype
**Goal**: Playable live show - screen callers, manage Vern's needs, see show quality impact

### Completed
- [x] Stats system (VernStats with 7 stats, decay, show quality calculation)
- [x] Phase/Time system (PreShow → LiveShow → PostShow cycle)
- [x] Caller screening system (generation, queues, legitimacy, patience)
- [x] Topics with screening rules
- [x] StatModifier assets (Items, Events)
- [x] Debug UI for testing
- [x] Live Show UI (runtime-generated uGUI)
  - Header bar with night, phase, clock, LIVE indicator
  - Vern's stats panel with 7 stat bars + show quality
  - Caller screening panel with approve/reject
  - On-air panel with active caller display
  - Caller queue panel (incoming + on-hold)
- [x] Listener count tracking
  - ListenerManager tracks audience size during live shows
  - Show quality affects listener growth/decay over time
  - Caller quality impacts listeners (great callers attract, bad callers repel)
  - Listener count displayed in header bar with change indicator
- [x] Item usage system
  - ItemManager tracks inventory and handles item usage
  - ItemPanel UI with use buttons and keyboard shortcuts (1-5)
  - Items consume on use, apply stat modifications to Vern
  - Optional cooldowns and per-item settings
  - Starts with 3 of each item (Coffee, Water, Sandwich, Whiskey, Cigarette)
- [x] Basic audio system
  - AudioManager singleton with SFX/Music/Ambience sources
  - 15 SFX types for phase transitions, caller events, UI feedback, alerts
  - Auto-plays sounds by subscribing to game events
  - UI panels trigger button click and item feedback sounds

### TODO
_Milestone 1 complete! Proceed to Milestone 2._

---

## Milestone 2: Full Loop
**Goal**: All 3 phases functional - PreShow setup, LiveShow gameplay, PostShow income

### TODO
- [ ] PreShow phase UI
  - Topic selection
  - Caller rule configuration
  - Item purchasing (pre-show supplies)
- [ ] PostShow phase UI
  - Income calculation display
  - Night summary (callers taken, show quality, listeners)
- [ ] Economy system
  - Money tracking
  - Income based on listeners/show quality
  - Item costs
- [ ] Basic scene structure (Control Room as primary location)

---

## Milestone 3: Progression
**Goal**: Multi-night play with save/load, upgrades, difficulty scaling

### TODO
- [ ] Save/Load system (persist night count, money, unlocks)
- [ ] Upgrades system
  - Equipment upgrades (better caller screening info, queue size)
  - Station upgrades (longer show duration, more ad slots)
- [ ] Difficulty scaling (more callers, trickier legitimacy, faster stat decay)
- [ ] Ad break mechanics (schedule ads, impact on listeners)

---

## Milestone 4: Content & Polish
**Goal**: Full game experience with depth, art, and audio

### TODO
- [ ] Additional locations (Studio view, Kitchen, Equipment Room)
- [ ] Staff hiring system (assistants to help with tasks)
- [ ] Maintenance mechanics (equipment degradation)
- [ ] Art pass (proper sprites, UI art, backgrounds)
- [ ] Audio pass (music, full SFX, caller voice cues)
- [ ] More topics, caller types, random events
- [ ] Balancing and playtesting

---

## Backlog
Features to consider for future development:

- Guest interviews
- Evidence collection/analysis mini-game
- Special broadcast events (breaking news, celebrity callers)
- Affiliate network growth
- Multiple endings based on Vern's belief trajectory
- Steam achievements
- Accessibility options

---

## Infrastructure

### Completed
- [x] CI/CD with GitHub Actions (GameCI)
  - Automated builds on version tags
  - Windows and WebGL platforms
  - GitHub Releases with downloadable zips
- [x] Unit tests for core data classes (Stat, VernStats, Caller)

### TODO
- [ ] Unity license activation (follow docs/CI_CD_SETUP.md)
- [ ] Steam deployment pipeline (requires Steamworks account)
