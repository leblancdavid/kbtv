# KBTV - UFO Arc Expansion Catalog (50 arcs)

Plan: expand UFO caller variety. Existing UFO arcs are overwhelmingly
first-hand-experience stories. This catalog adds 50 new arcs (~10% crazy-but-
sincere stories, ~45% opinion-only callers, ~45% question-askers). Authoring
rules per [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md);
voice/filler rules per [AUDIO_GENERATION.md](../tools/AUDIO_GENERATION.md).

## Design constraints (apply to every arc here)

- `ufos` topic token on all line ids; descriptor == arcId; ids globally unique
- Turns strictly alternate, start with Vern, end with Vern (8-12 turns)
- 13 mood variants per Vern turn; caller turns 1 line each, `voiceText` with
  natural fillers ("uh", "I mean", "you know"), no asterisk notation
- No hardcoded caller names (pre-generated audio; see VOICE_AUDIO.md)
- Legitimacy: crazy/opinion lean Questionable/Fake; question arcs may be
  Credible (a coherent, documented ask is believable even when the subject
  isn't). No new Compelling arcs - that pool stays experience-heavy
- Vern's info-sharing (Blue Book, Nimitz "Tic Tac", Phoenix/Belgium waves,
  Hynek, MUFON/ASRS/ADS-B, practical checklists) is the counterweight in
  every question/opinion arc; mood variants must stay in character
- Opinion/question arcs run 9 turns (5 Vern x 13 = 65 vern lines + 4 caller
  lines = 69 audio lines per arc - pilot established this as the standard);
  crazy-story arcs also 9 unless the story earns more

## Status legend

`[ ]` drafted, `[x]` authored + schema-green. Audio generation is deferred:
run `python generate_arc_audio.py --all` before expecting
`ConversationArcTests` to pass.

## Batch 0 - PILOT

- [x] `ufo_fillings_humming` | crazy | Questionable | m | cold_factual | Fillings buzz in logged patterns; Vern: implant-signal claims date to the '70s - dentist + AM-radio control test
- [x] `ufo_it_our_military` | opinion | Fake | m | gruff_experienced | Every sighting is a black project, callers are liars; Vern: declassified Navy "Tic Tac" radar data
- [x] `ufo_stop_the_broadcast` | opinion | Questionable | f | official | Talking on air "draws them"; Vern: open-line tradition, who's called over the years
- [x] `ufo_what_do_i_do` | question | Questionable | f | emotional_wreck | It just landed in the field 20 minutes ago; Vern's checklist: don't approach, time/place, rule out drones/planes, file later
- [x] `ufo_missing_time` | question | Credible | m | nervous_hesitant | Unaccounted hours, wakes up in the yard; Vern: Hynek's criteria - and a sleep workup first

## Batch 1 - crazy stories complete + opinion wave 1

- [x] `ufo_returned_ring` | crazy | Credible | f | emotional_wreck | Ring "borrowed" in a 1979 blackout, returned last week slightly different
- [x] `ufo_wedding_lights` | crazy | Questionable | m | charismatic_storyteller | A light formation drew the ring where/when they got married
- [x] `ufo_alien_jury` | crazy | Questionable | m | nervous_hesitant | Drafted as juror in the neighbor's abduction trial in the corn; wants to countersue
- [x] `ufo_hollywood_poisoned` | opinion | Fake | m | charismatic_storyteller | Callers only see what movies taught them; Vern: shapes come in waves - foxtrots, saucers, triangles
- [x] `ufo_starling_man` | opinion | Fake | m | cold_factual | Bird researcher: 90% of sightings are starling murmurations; Vern: doesn't explain radar returns

## Batch 2 - opinion wave 2

- [x] `ufo_math_doesnt_add_up` | opinion | Questionable | m | cold_factual | Accountant: the distances are impossible; Vern: vastness is the point; Hynek changed his mind late
- [x] `ufo_demons_not_aliens` | opinion | Fake | f | official | They're demons and the show is summoning them; Vern: respectful fight, pastors call this show too
- [x] `ufo_time_travelers` | opinion | Questionable | m | enthusiastic_convert | They're not from here, they're from *later*; Vern: that hypothesis is older than the show
- [x] `ufo_town_coverup` | opinion | Questionable | f | gruff_experienced | Whole town sees them, silence over property values; Vern: a recurring small-town pattern
- [x] `ufo_call_grader` | opinion | Fake | m | charismatic_storyteller | Grades last night's callers, demands a regular segment; Vern: nobody grades the line

## Batch 3 - opinion wave 3

- [x] `ufo_haarp_ham` | opinion | Fake | m | enthusiastic_convert | Ionosphere experiments did it, all of it; Vern: what HAARP actually does
- [x] `ufo_rated_pilot_policy` | opinion | Questionable | m | professional | Licensed pilot, zero sightings, FAA coverup op-ed; Vern: pilots have ASRS - few credible reports filed
- [x] `ufo_alien_rights` | opinion | Questionable | f | emotional_wreck | Offended on the beings' behalf - "you describe them like pests"; Vern concedes, apologizes on air
- [x] `ufo_they_run_dmv` | opinion | Fake | m | charismatic_storyteller | They're already here - the DMV, the IRS; Vern: every era gets an infiltration theory
- [x] `ufo_book_deal` | opinion | Fake | m | official | Self-promoter pitching a theory book for airtime; Vern: no plugs - and a caution about publisher claims

## Batch 4 - opinion wave 4

- [x] `ufo_shoot_it_down` | opinion | Questionable | m | gruff_experienced | Shoot one down, reverse-engineer it publicly; Vern: 1950s Blue Book gunship ideas, seriously considered
- [x] `ufo_ready_for_contact` | opinion | Questionable | f | enthusiastic_convert | We're spiritually ready - broadcast a welcome; Vern: Arecibo did it in 1974, and here's why
- [x] `ufo_astronomy_snob` | opinion | Fake | f | cold_factual | Show never hosts real astronomers; Vern: open line, not a symposium - scientists do call
- [x] `ufo_foreign_drones` | opinion | Questionable | m | official | All Russian/Chinese drones, geopolitics not space; Vern: this story is 70 years older than drones
- [x] `ufo_press_coverup_local` | opinion | Questionable | f | charismatic_storyteller | Local paper buried the '79 wave out of embarrassment; Vern: small papers were the archives

## Batch 5 - opinion complete + question wave 1

- [x] `ufo_producers_compromised` | opinion | Questionable | m | cold_factual | "Your screening room is infiltrated by believers"; Vern: dry - the skeptic mailbag proves otherwise
- [x] `ufo_sleep_paralysis_purist` | opinion | Fake | m | cold_factual | It's ALL sleep paralysis; Vern: paralysis is real but doesn't explain two witnesses in one room
- [x] `ufo_tin_hat_neighbor` | opinion | Fake | m | gruff_experienced | Complains the neighbor put foil in her windows after the lights; Vern: neighbor advice, not astronomy
- [x] `ufo_ancient_astronaut_hater` | opinion | Questionable | m | official | Ancient-alien TV insults history - they were people with torches; Vern: Sagan's line, both directions
- [x] `ufo_kid_saw_lights` | question | Credible | f | professional | Child saw something at the school field - how to talk without scaring; Vern: don't interrogate, note dreams/drawings

## Batch 6 - question wave 2

- [x] `ufo_where_do_i_report` | question | Credible | m | cold_factual | Is there an official hotline?; Vern: no - MUFON, ASRS if flying, check NOTAMs
- [x] `ufo_dashcam_help` | question | Questionable | m | gruff_experienced | Has footage, wants honest analysis; Vern: keep the raw file, frame rates matter, avoid paid enhancers
- [x] `ufo_netflix_or_radio` | question | Questionable | m | charismatic_storyteller | Streaming doc offered money vs. keep it local; Vern: read the rights contract, keep your originals
- [x] `ufo_flight_tracker_says_nothing` | question | Credible | f | enthusiastic_convert | Tracker app saw nothing at the timestamp - is that evidence?; Vern: ADS-B only sees transponders - weak proof
- [x] `ufo_am_i_obsessed` | question | Credible | f | nervous_hesitant | Wife says she's in too deep - is she losing perspective?; Vern: research vs. anxiety; sleep, meals, community

## Batch 7 - question wave 3

- [x] `ufo_why_only_america` | question | Questionable | m | cold_factual | Why do sightings always seem American?; Vern: global - Petrozavodsk '83, Belgium wave
- [x] `ufo_rooftop_observation_post` | question | Questionable | m | enthusiastic_convert | How to build a cheap rooftop post?; Vern: red light, logbook, learn the sky - kills 95% of mysteries
- [x] `ufo_sue_power_company` | question | Fake | m | gruff_experienced | Transformer blew as the light passed over - can he sue?; Vern: realistically no; utility anomaly reports are a real category though
- [x] `ufo_metal_shard_safety` | question | Questionable | m | nervous_hesitant | Found a warm shard - safe to keep?; Vern: sealed bag, don't sleep with it, protocol exists
- [x] `ufo_dad_stayed_quiet` | question | Credible | m | emotional_wreck | Dad died, family found sealed envelopes - did the government pay him off?; Vern: the '50s-'60s don't-talk culture

## Batch 8 - question wave 4

- [x] `ufo_radar_vs_drone` | question | Credible | f | professional | Aviation worker: drone vs. UAP on a scope - how would you tell?; Vern: speed, plan correlation, transponder silence
- [x] `ufo_increased_sightings` | question | Questionable | f | cold_factual | Is it just her, or is it more since 2017?; Vern: NYT '17, Navy videos, AARO - the reporting curve, not the phenomenon
- [x] `ufo_call_for_my_mother` | question | Credible | f | emotional_wreck | May she tell what her late mother saw in '74, laughed out of every room?; Vern: this is where it belongs
- [x] `ufo_believe_my_wife` | question | Questionable | m | gruff_experienced | How do I know if my wife's story is real?; Vern: she's your wife - don't be the family skeptic
- [x] `ufo_support_group` | question | Credible | f | nervous_hesitant | Does a missing-time support group exist?; Vern: networks and books - and therapy isn't a confession

## Batch 9 - question complete

- [x] `ufo_what_should_i_write` | question | Questionable | m | cold_factual | What to record before the memory fades?; Vern: time, compass, sound, smell, devices - memory rewrites itself
- [x] `ufo_roadside_or_microsleep` | question | Credible | m | tired | Driver: did it happen, or did I nod off?; Vern: take both seriously - log hours, the truck's data will tell
- [x] `ufo_still_believe_eyes` | question | Credible | f | official | 81, family says stop trusting her eyes; Vern: the eye is the first instrument - witnesses don't expire
- [x] `ufo_have_you_seen_one` | question | Questionable | m | enthusiastic_convert | "Vern - have YOU seen one?"; Vern: the childhood Venus story (dead-air canon), still unresolved
- [x] `ufo_alien_ballot` | opinion | Fake | f | charismatic_storyteller | Running for local office on a disclosure platform, wants endorsement; Vern: the line is not a precinct

## Distribution after full rollout (UFO)

| Legitimacy | Existing | +New | Total |
|---|---|---|---|
| Compelling | 12 | 0 | 12 |
| Credible | 15 | 12 | 27 |
| Questionable | 18 | 26 | 44 |
| Fake | 15 | 12 | 27 |

Flavor split: 5 crazy / 23 opinion / 22 question (the last row adds one
extra opinion to keep batch sizes even).
