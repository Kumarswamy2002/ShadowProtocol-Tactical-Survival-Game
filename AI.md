# Shadow Protocol - AI & Perception Architecture

## 1. Sensory Perception Model

Enemies perceive their environment through three distinct channels:

### 1.1 Vision Cone
$$\text{Sight Check} = (\text{Distance} \le R_{\text{view}}) \land (\theta_{\text{target}} \le \frac{\text{FOV}}{2}) \land \text{LineOfSight}$$
Alertness grows proportionally to proximity and exposure:
$$\Delta\text{Alertness} = 60 \times \left(1 - \frac{\text{Distance}}{R_{\text{view}}}\right) \times \text{ExposureFactor} \times \Delta t$$

### 1.2 Acoustic Attenuation
$$\text{Audible Volume} = \frac{\text{SoundLoudness} \times \text{Sensitivity}}{\max(1.0, \text{Distance})}$$
If audible volume surpasses the hearing threshold ($2.0$), the AI transitions from calm to **Investigate** at the sound origin.

### 1.3 Damage Reaction
Direct kinetic impacts instantly spike alertness to $100\%$ and notify nearby squad members.

---

## 2. Squad Tactical Hierarchy

Squads operate under a distributed command structure:

```
                  Commander (Orders & Squad Strategy)
                                 |
         +-----------------------+-----------------------+
         |                       |                       |
       Scout                  Assault                  Sniper
  (Flank & Spot)        (Direct Push & Fire)     (Overwatch & Cover)
```

- **Commander**: Issues dynamic squad orders (`Attack`, `Flank`, `Search`, `Retreat`, `Hold`, `CallBackup`).
- **Casualty Fallback**: If the Commander is eliminated, the highest-ranking remaining combatant (Elite or Assault) automatically assumes command.
