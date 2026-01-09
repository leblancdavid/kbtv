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

### TODO
- [ ] Basic game UI for Live Show (replace debug IMGUI)
  - Caller queue display
  - Screening interface
  - Vern's stats display
  - Show clock/progress
- [ ] Item usage system (use Coffee, Sandwich, etc. to modify Vern's stats)
- [ ] Listener count tracking (based on show quality, caller impacts)
- [ ] Basic audio (placeholder SFX for caller events, phase transitions)

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
