# KBTV - Development Roadmap

## Milestone 1: Core Prototype
**Goal**: Playable live show - screen callers, manage Vern's needs, see show quality impact

### Completed
- [x] Stats system (VernStats redesigned with new categories: Dependencies, Physical, Emotional, Cognitive, Long-Term)
  - VIBE system (Vibrancy, Interest, Broadcast Entertainment) with sigmoid curves
  - 7 mood types with Spirit modifier
  - Sigmoid functions for listener growth, decay acceleration, item effectiveness
  - See [VERN_STATS.md](../systems/VERN_STATS.md)
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

### Completed
- [x] PreShow phase UI
  - Topic selection grid with descriptions
  - Start Show button (disabled until topic selected)
  - Night counter display
- [x] Arc-based dialogue system
  - ConversationArc, ArcRepository, ArcJsonParser
  - MoodCalculator, DiscernmentCalculator, DialogueSubstitution
  - JSON conversation arcs with mood variants and belief branches
- [x] Voice audio system
  - VoiceAudioService for async loading of pre-generated audio
  - Audio dialogue player with event-driven playback
  - Broadcast audio service for show elements
- [x] Broadcast flow with AsyncBroadcastLoop
  - Event-driven broadcast coordination
  - Show opening/closing, between-caller transitions
  - Dead air filler system
  - Executable-based architecture (Music, Dialogue, Ad, Transition executables)
- [x] PostShow phase UI
  - Income calculation and display
  - Night summary (callers taken, show quality, listeners)
  - Equipment upgrade interface
- [x] Economy system (Phase 1)
  - Money tracking and transactions
  - Starting money and per-show expenses
  - Item purchasing and costs
  - Income/expense breakdown
- [x] Save/Load system
  - JSON persistence with version migration
  - Auto-save triggers
  - SaveData structure with ISaveable interface
- [x] Ad system (primary income source)
  - Ad break scheduling and revenue
  - Sponsor contracts and ad breaks
  - AdManager with break logic and timing
- [x] Equipment upgrades system
  - Phone Line and Broadcast equipment upgrades
  - Audio quality progression through upgrade tiers
  - PostShow upgrade UI integration

### In Progress
- [x] Audio processing system (completed)
  - Phone line effects (band-pass filter, distortion, static)
  - Broadcast effects (EQ, compression, distortion)
  - Equipment-based quality progression (Levels 1-4)
  - Static noise layer for caller audio

_Milestone 2 complete! Core game loop fully functional. Proceed to Milestone 3._

---

## Milestone 3: Progression
**Goal**: Multi-night play with save/load, upgrades, difficulty scaling

### In Progress
- [x] Save/Load system (completed)
  - JSON file persistence with version migration
  - Auto-save on night end and equipment purchase
- [x] Equipment upgrades system (completed)
  - Phone Line equipment (caller audio quality)
  - Broadcast equipment (Vern audio quality)
  - PostShow upgrade UI

### TODO
- [x] Ad system (primary income source) ✅ Phase 1 Complete
- [x] Vern stats redesign ✅ Complete - see VERN_STATS.md
- [ ] Economy system (Phase 2-3)
  - Equipment maintenance/degradation system
  - Broadcast delay system (dump button for FCC compliance)
  - Sponsorship system (fixed payments, relationship meters)
  - Affiliate system (passive income from listener milestones)
   - License renewal (every 30 shows)
- [ ] Economy system (Phase 2-3)
  - Equipment maintenance/degradation system
  - Broadcast delay system (dump button for FCC compliance)
  - Sponsorship system (fixed payments, relationship meters)
  - Affiliate system (passive income from listener milestones)
   - License renewal (every 30 shows)
   - See [ECONOMY_PLAN.md](../systems/ECONOMY_PLAN.md)
- [ ] Additional upgrade tracks
  - Transmitter (listener range)
  - Screening tools (more caller info)
  - Station upgrades (queue size, show duration)
- [ ] Difficulty scaling (more callers, trickier legitimacy, faster stat decay)
- [ ] Topic experience system
  - XP gain per show (base + quality + screening bonuses)
  - 7 levels per topic (Novice → Master)
  - Freshness system (encourages topic variety)
  - Level bonuses (discernment, screening info, caller quality, mood)
  - See [TOPIC_EXPERIENCE.md](TOPIC_EXPERIENCE.md)
- [ ] Special events system
  - Events trigger during broadcasts (urgency)
  - Topic-based events unlock at levels 3/5/7
  - Evidence rewards on success, penalties on failure
  - Cross-topic events at 2+ max level topics
  - See [SPECIAL_EVENTS.md](SPECIAL_EVENTS.md)
- [ ] Evidence collection system
  - 5 evidence types (Photo, Audio, Sample, Document, Video)
  - 5 tiers (Common → Legendary), replaces lower tiers
  - Cabinet storage with upgradeable slots
  - Set bonuses for collecting all types
   - See [EVIDENCE_SYSTEM.md](../systems/EVIDENCE_SYSTEM.md)
- [ ] Investigation tools
  - 6 tool types (Camera, Flashlight, EMF Reader, Audio Recorder, Night Vision, Motion Sensor)
  - 4 upgrade tiers per tool
  - Better tools = better evidence quality/drops
  - Synergy bonuses for multiple high-level tools
   - See [TOOLS_EQUIPMENT.md](../systems/TOOLS_EQUIPMENT.md)

---

## Milestone 4: Content & Polish
**Goal**: Full game experience with depth, art, and audio

### TODO
- [ ] Additional locations (Studio view, Kitchen, Equipment Room)
- [ ] Staff hiring system (assistants to help with tasks)
  - Fixed pool unlocking at show milestones (15, 25, 35, 40, 50)
  - Intern, Assistant Screener, Tech, Security, Producer
  - Autonomous behavior (screener, producer) vs passive bonuses (tech, security)
  - Staff salaries as recurring expense
   - See [ECONOMY_PLAN.md](../systems/ECONOMY_PLAN.md)
- [ ] Maintenance mechanics (equipment degradation)
  - Equipment condition meter (100% → 0%)
  - Degraded performance at low condition
  - Equipment failure risk when critical
  - Full replacement cost at 0%
- [ ] Penalty system
  - FCC violations (missed dump button)
  - Equipment failure from neglect
  - Vern walkout at mood 0
  - Dead air fines
  - Noise complaints, caller lawsuits (rare events)
- [ ] Facility upgrades
  - Break room, security system, backup generator
  - Larger studio
  - Buy station ($15,000) to eliminate rent
- [ ] Technology upgrades
  - Website, podcast feed (online income streams)
  - Digital board
- [ ] Late-game income sources
  - Merchandise system
  - Donations/pledge drives
  - Publications (book deals)
  - Premium hotline
  - Syndication goals
- [ ] Art pass (proper sprites, UI art, backgrounds)
- [ ] Audio pass (music, full SFX, caller voice cues)
- [ ] More topics, caller types, random events
- [ ] Topic experience unlocks
  - Recurring callers (level 3+)
  - Topic-specific sponsors (level 3+)
  - Expert guest booking system (level 4+)
  - Breaking news events (level 5+)
  - See [TOPIC_EXPERIENCE.md](TOPIC_EXPERIENCE.md)
- [ ] Balancing and playtesting

---

## Backlog
Features to consider for future development:

- Topic expansion (new topics, unlock system)
- Guest interviews (tied to topic experience level 4+)
- Investigation mini-game
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
- [ ] CI/CD setup for automated builds (see docs/technical/CI_CD_SETUP.md)
- [ ] Steam deployment pipeline (requires Steamworks account)
