# Shadow Protocol - Gameplay Systems Manual

## 1. Core Gameplay Loop

```
    Explore Interconnected Veyra World
                   |
                   v
   Discover Hostile / Neutral Outposts
                   |
                   v
    Tactical Engagement / Stealth Infiltration
                   |
                   v
     Scavenge Resources & Rare Components
                   |
                   v
  Complete Main / Side Missions & Dynamic Events
                   |
                   v
    Upgrade Character Skills & Craft Advanced Gear
                   |
                   v
    Unlock High-Danger Regions & Uncover Truth
```

## 2. Survival & Vitals Systems

- **Health ($0\dots100$)**: Direct physical condition. Starvation or extreme dehydration causes steady degradation until death.
- **Stamina ($0\dots100$)**: Expended during sprinting, dodging, climbing, swimming, and melee attacks. Regenerates faster when fed and hydrated.
- **Armor ($0\dots100$)**: Absorbs up to 70% of incoming kinetic damage before direct health depletion.
- **Hunger & Hydration ($0\dots100$)**: Natural depletion over time. Exertion (sprinting, swimming) increases the rate of loss.

## 3. Tactical Ballistics & Combat

Damage calculation utilizes full physical metrics:
$$\text{Damage} = \text{BaseDamage} \times \text{Falloff}(d) \times \text{HitZoneMultiplier} \times \left(1 - \frac{\text{Armor} \times (1 - \text{Pen})}{\text{Armor} \times (1 - \text{Pen}) + 100}\right)$$

- **Headshot Multiplier**: $2.5\times$
- **Torso Multiplier**: $1.0\times$
- **Limbs Multiplier**: $0.75\times$
- **Critical Strike**: $1.5\times$ base damage

## 4. The 10 Interconnected Regions of Veyra

1. **Old City (Danger: 2)**: Ruined metropolitan core with salvageable electronics and scattered raiders.
2. **Industrial District (Danger: 4)**: Toxic refineries, chemical stocks, and armed Directorate scavengers.
3. **Abandoned Highway (Danger: 3)**: Vehicle salvage and highway sniper ambushes.
4. **Survivor Settlement (Danger: 1)**: Safe haven with merchant shops, crafting workbenches, and quest givers.
5. **Forest Region (Danger: 3)**: Dense wilderness, stealth camouflage opportunities, and wildlife encounters.
6. **Military Base (Danger: 7)**: Heavily guarded Directorate forward outpost with high-tier weapon blueprints.
7. **Underground Facility (Danger: 6)**: Subterranean research bunker requiring low-light night-vision gear.
8. **Research Laboratory (Danger: 8)**: Directorate black-site holding Blackout prototype records.
9. **Enemy Fortress (Danger: 9)**: Heavily fortified citadel commanded by Directorate Elites.
10. **Restricted Zone (Danger: 10)**: Ground zero anomalies with extreme hazards and legendary loot.
