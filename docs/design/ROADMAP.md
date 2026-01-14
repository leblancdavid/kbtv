# KBTV - Development Roadmap

## Milestone 1: Core Prototype
**Goal**: Playable live show - screen callers, manage Vern's needs, see show quality impact

### Completed
- [x] Stats system (VernStats with 7 stats, decay, show quality calculation)
  - **Note**: System being redesigned - see VERN_STATS.md for new architecture
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
  - ConversationArc, ArcRepository, ArcConversationGenerator
  - MoodCalculator, DiscernmentCalculator, DialogueSubstitution
  - 17 sample arcs (4 topics x 4 legitimacy tiers, + 1 topic-switcher)
  - 5 mood variants per arc with belief branches
- [x] Voice audio system
  - VoiceAudioService with Addressables for async loading
  - ~950 pre-generated voice lines (Piper TTS)
  - Audio Mixer with VernGroup and CallerGroup routing
- [x] Broadcast flow
  - Show opening/closing
  - Between-caller transitions
  - Dead air filler system

### In Progress
- [ ] Audio processing system
  - Phone line effects (band-pass filter, distortion, static)
  - Broadcast effects (EQ, compression, distortion)
   - Equipment-based quality progression (Levels 1-4)
   - Static noise layer for caller audio
   - See [STATION_EQUIPMENT.md](../systems/STATION_EQUIPMENT.md)

### TODO
- [ ] PreShow enhancements
  - Caller rule configuration
  - Item purchasing (pre-show supplies)
- [ ] PostShow phase UI
  - Income calculation display
  - Night summary (callers taken, show quality, listeners)
  - Equipment upgrade interface
- [ ] Economy system (Phase 1)
  - Money tracking
  - Starting money ($500) + grace period (first 5 shows no overhead)
  - Per-show expenses (power, rent)
  - Item costs
  - Income/expense breakdown screen
   - See [ECONOMY_SYSTEM.md](../systems/ECONOMY_SYSTEM.md) and [ECONOMY_PLAN.md](../systems/ECONOMY_PLAN.md)
- [ ] Basic scene structure (Control Room as primary location)
- [ ] Expand arc content (target: 80 arcs total - 4 topics x 4 legitimacy x 5 arcs each)
- [ ] Audio polish
  - Assign mixer groups to AudioManager in scene
  - Dynamic typewriter speed synced to audio duration

---

## Milestone 3: Progression
**Goal**: Multi-night play with save/load, upgrades, difficulty scaling

### In Progress
- [ ] Save/Load system
  - JSON file persistence to Application.persistentDataPath
  - Auto-save on night end and equipment purchase
   - See [SAVE_SYSTEM.md](../technical/SAVE_SYSTEM.md)
- [ ] Equipment upgrades system
  - Phone Line equipment (caller audio quality)
  - Broadcast equipment (Vern audio quality)
  - PostShow upgrade UI
   - See [STATION_EQUIPMENT.md](../systems/STATION_EQUIPMENT.md)

### TODO
- [ ] Ad system (primary income source) ✅ Phase 1 Complete
  - Schedule ad breaks during shows
  - Ad revenue based on listeners and timing
  - Sponsor contracts
   - See [AD_SYSTEM.md](../systems/AD_SYSTEM.md)
- [ ] Vern stats redesign
  - New stat categories (Dependencies, Physical, Emotional, Cognitive, Long-Term)
  - VIBE system (Vibrancy, Interest, Broadcast Entertainment) with sigmoid curves
  - 7 mood types with Spirit modifier
  - Sigmoid functions for listener growth, decay acceleration, item effectiveness
   - See [VERN_STATS.md](../systems/VERN_STATS.md)
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
