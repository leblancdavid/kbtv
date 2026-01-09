# KBTV (Beyond the Veil AM) - Game Design Document

## Overview
**Genre**: Simulation / Management / Tycoon-style
**Setting**: Paranormal/conspiracy talk radio station (inspired by Coast to Coast AM)
**Platform**: 2D Unity game

## Premise
Player acts as the managing producer of KBTV, a paranormal/conspiracy talk radio station. The goal is to grow the show's credibility and popularity by exposing the truth about paranormal topics (aliens, government conspiracies, supernatural phenomena).

## Core Characters

### Vern Tell
- **Role**: Skeptical radio host, voice of KBTV
- **Player Interaction**: Via control room during broadcasts, door drop-offs during live shows
- **Stats** (Sims-style meters):
  - **Mood** (0-100%): Affected by events, interactions, items
  - **Energy** (0-100%): Drops over time, restored by caffeine/items
  - **Susceptibility**: How much events/evidence impact Vern's beliefs
  - **Hunger/Thirst**: Basic needs
  - **Patience**: Can be patient or impatient
  - **Belief Meter**: Correlates to show quality; belief = more interesting show = more listeners

The combination of all meters affects the overall belief meter movement.

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
| **Caller Screening** | Mini-games/deduction to verify caller legitimacy; rules-based rejection affects show |
| **Belief System** | Vern's belief meter drives show quality and listener count |
| **Needs Management** | Fulfill Vern's needs to maintain mood/energy/susceptibility |
| **Show Formatting** | Balance callers, ads, guests per topic |
| **Listener Count** | Dynamic, based on show quality and belief meter |
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
