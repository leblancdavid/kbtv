# KBTV (Beyond the Veil AM) - Game Design Document

## Overview
**Genre**: Simulation / Management / Tycoon-style
**Setting**: Paranormal/conspiracy talk radio station (inspired by Coast to Coast AM)
**Platform**: 2D Godot game

## Premise
Player acts as the managing producer of KBTV, a paranormal/conspiracy talk radio station. The goal is to grow the show's credibility and popularity by exposing the truth about paranormal topics (aliens, government conspiracies, supernatural phenomena).

## Core Characters

### Vern Tell
- **Role**: Skeptical radio host, voice of KBTV
- **Player Interaction**: Via control room during broadcasts, door drop-offs during live shows
- **Stats** (see [VERN_STATS.md](VERN_STATS.md) for complete system):
  - **Dependencies**: Caffeine, Nicotine (0-100) - must be maintained, cause withdrawal
  - **Physical**: Energy, Satiety (0-100) - capacity to perform
  - **Spirit**: (-50 to +50) - universal mood modifier, affects all mood expressions
  - **Cognitive**: Alertness, Discernment, Focus (0-100) - performance quality
  - **Long-Term**: Skepticism (0-100), Topic Affinity (-50 to +50 per topic)

The combination of all stats affects VIBE (Vibrancy, Interest, Broadcast Entertainment), which drives listener growth.

## Game Loop: Nightly Shows

### Pre-Show
- Choose topic to cover
- Set rules for callers/guests based on topic
- Format show (callers, ad breaks, guests)
- Purchase supplies
- Perform maintenance

### Live Show
- Screen callers via mini-games/deduction
- Queue callers for airtime
- Manage ad breaks (fade-in/out)
- Fulfill Vern's needs (coffee, food, etc.) via door drops
- Handle events and evidence
- Monitor listener count (changes based on show quality)
- Special events occur

### Post-Show
- Calculate income (viewers, ads, affiliates)
- Purchase upgrades
- Hire staff
- "Go to bed" → next night

## Environment / Locations

### Studio (Broadcast Room)
- Where Vern broadcasts (locked during live shows)
- Player interacts via door drop-offs
- Player cannot enter during broadcast

### Control/Monitor Room
- Primary player location
- Tasks: Choose topics, screen callers, handle audio, ad breaks, analyze evidence
- Caller screening: Mini-games with deduction gameplay (Papers, Please style)
- Caller info: Name, phone number, location, topic
- Rules-based: Callers not matching topic criteria hurt show quality and Vern's belief

### Dining/Kitchen/Bathroom Area
- Fetch items for Vern: Coffee, food, alcohol, cigarettes
- High-traffic area (Vern is caffeine-addicted, chain smoker)

### Main Office
- Hire staff to assist with station management
- Unlocks as station grows

### Transmitter/Equipment Room
- Holds transmitter and radio equipment
- Requires maintenance

### Recording Studio
- TBD

### Parking Lot / Entrance
- Front of station
- Potential events occur here

### Back Area
- Antennas and external equipment
- Requires maintenance

## Mechanics Summary

| Feature | Description |
|---------|-------------|
| **Caller Generation** | 90% on-topic callers match show topic, 10% off-topic callers provide variety; each caller has `IsOffTopic` flag |
| **Caller Screening** | Mini-games/deduction to verify caller legitimacy; rules-based rejection affects show |
| **Caller Phone Quality** | Each caller has varying audio quality (Terrible to Good) affecting how they sound on air |
| **VIBE System** | VIBE (Vibrancy, Interest, Broadcast Entertainment) drives show quality and listener count |
| **Needs Management** | Fulfill Vern's needs (caffeine, nicotine, food) to maintain VIBE |
| **Mood Types** | 7 mood types (Tired, Energetized, Irritated, Amused, Gruff, Focused, Neutral) affect dialog |
| **Spirit Modifier** | Spirit (-50 to +50) modulates all mood expressions using sigmoid curves |
| **Show Formatting** | Balance callers, ads, guests per topic |
| **Listener Count** | Dynamic, based on VIBE using sigmoid growth curves |
| **Economy** | Earn income → buy upgrades → hire staff → grow station |
| **Maintenance** | Equipment, antennas require upkeep |

## Potential Features (TBD)
- Guest interviews
- Evidence collection/analysis
- Special broadcast events
- Affiliate network growth
- Staff management system

## Design Goals
1. Create tension between screening callers quickly vs. correctly
2. Balance Vern's needs while managing the broadcast
3. Build show credibility through evidence and compelling content
4. Progressive complexity as station grows (staff, upgrades, larger audience)
