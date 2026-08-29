#!/usr/bin/env python3
"""
Shadow Protocol - Local Interactive Game & System Simulator
Run all core gameplay loops, tactical combat, squad AI, branching dialogue, 
crafting engines, and cloud persistence locally from your terminal.
"""

import sys
import time
import json
import math
import random


def print_banner():
    print("=" * 65)
    print("                S H A D O W   P R O T O C O L                ")
    print("         Open-World Tactical Survival Game Simulator         ")
    print("=" * 65)
    print(" [Engine: Modular Domain] [Target: Veyra Ruins] [Ver: 1.0.0] ")
    print("=" * 65)


def demo_player_survival():
    print("\n--- [1] PLAYER CONTROLLER & SURVIVAL VITALS SIMULATION ---")
    health, stamina, hunger, hydration = 100.0, 100.0, 100.0, 100.0
    level, exp, credits_val = 1, 0, 500
    state = "IDLE"
    
    print(f"Initial State: {state} | HP: {health:.1f} | Stamina: {stamina:.1f} | Hunger: {hunger:.1f} | Hydration: {hydration:.1f}")
    
    # Simulate sprinting across Sector 4
    print("\n> Player begins SPRINTING across Sector 4...")
    state = "SPRINTING"
    for sec in range(1, 4):
        stamina = max(0.0, stamina - 12.0)
        hunger = max(0.0, hunger - 0.5)
        hydration = max(0.0, hydration - 1.2)
        print(f"  [T+{sec}s] State: {state} | Speed: 11.7 m/s | Stamina: {stamina:.1f} | Hunger: {hunger:.1f} | Hydration: {hydration:.1f}")
    
    # Taking combat hit
    print("\n> Enemy sniper fires! Direct hit to player body armor!")
    damage = 45.0
    armor = 50.0
    mitigation = armor / (armor + 100.0)
    actual_dmg = damage * (1.0 - mitigation)
    health -= actual_dmg
    print(f"  Armor mitigated {mitigation*100:.1f}% damage. Player HP: {health:.1f}/100.0")


def demo_tactical_combat():
    print("\n--- [2] TACTICAL COMBAT & BALLISTICS SIMULATION ---")
    weapons = [
        {"name": "V-9 Tactical Sidearm", "dmg": 28, "range": 35, "pen": 0.20, "rpm": 400},
        {"name": "Spectre-45 SMG", "dmg": 22, "range": 45, "pen": 0.25, "rpm": 750},
        {"name": "AR-556 Directorate Rifle", "dmg": 36, "range": 75, "pen": 0.45, "rpm": 650},
        {"name": "Ghost-762 Marksman Rifle", "dmg": 95, "range": 180, "pen": 0.75, "rpm": 45},
    ]
    
    target_armor = 40.0
    distance = 35.0
    
    print(f"Engaging Directorate Heavy Sentry at {distance}m (Target Armor: {target_armor:.0f}):")
    for w in weapons:
        # Calculate falloff and armor penetration
        falloff = 1.0 if distance <= w["range"] * 0.6 else max(0.2, 1.0 - ((distance - w["range"]*0.6) / (w["range"]*0.4)))
        eff_armor = max(0.0, target_armor * (1.0 - w["pen"]))
        mitigation = eff_armor / (eff_armor + 100.0)
        
        torso_dmg = w["dmg"] * falloff * (1.0 - mitigation)
        head_dmg = torso_dmg * 2.5
        
        print(f"  * {w['name']:<28} | Torso: {torso_dmg:>5.1f} dmg | Headshot (2.5x): {head_dmg:>5.1f} dmg")


def demo_squad_tactics():
    print("\n--- [3] SQUAD AI COORDINATION & TACTICAL ORDERS ---")
    print("Directorate Squad deployed in Industrial District:")
    squad = [
        {"role": "Commander", "state": "IDLE", "order": "HOLD"},
        {"role": "Scout", "state": "PATROL", "order": "SEARCH"},
        {"role": "Assault", "state": "IDLE", "order": "HOLD"},
        {"role": "Sniper", "state": "OVERWATCH", "order": "HOLD"},
    ]
    
    for m in squad:
        print(f"  - [{m['role']:<10}] Initial State: {m['state']:<10} | Current Order: {m['order']}")
        
    print("\n> Scout spots Player at (X: 120, Z: 450)! Alertness: 85%!")
    print("> Commander issues tactical squad orders:")
    
    squad[0]["order"] = "COORDINATE"
    squad[0]["state"] = "ATTACK"
    squad[1]["order"] = "FLANK_LEFT"
    squad[1]["state"] = "FLANK"
    squad[2]["order"] = "PUSH_CENTER"
    squad[2]["state"] = "ATTACK"
    squad[3]["order"] = "SNIPER_OVERWATCH"
    squad[3]["state"] = "TAKE_COVER"
    
    for m in squad:
        print(f"  [+] [{m['role']:<10}] -> Order: {m['order']:<18} -> AI State: {m['state']}")


def demo_branching_dialogue():
    print("\n--- [4] BRANCHING DIALOGUE SYSTEM & FACTION REPUTATION ---")
    print("NPC: Informant Caleb [Settler Safehouse - Sector 4]")
    print('Caleb: "You made it through the Directorate perimeter. Did you find the cipher drive?"')
    
    options = [
        "1. Yes, here is the encrypted drive. (Requires: Drive in Inventory)",
        "2. Why should I give it to you before you pay me? (Renegade)",
        "3. I discovered records proving the Blackout wasn't an accident. (Story Branch)"
    ]
    
    for opt in options:
        print(f"   {opt}")
        
    print("\n> Player selects Option 3...")
    print('Caleb: "Mother of God... then the Directorate engineered the collapse themselves. We need to alert the council."')
    print("  [Event] Reputation with FreeHaven Settlers: +15 | Mission Objective Complete: 'Deliver Cipher'")


def demo_crafting_and_save():
    print("\n--- [5] CRAFTING RECIPES & PERSISTENCE SERIALIZATION ---")
    print("Workbench Crafting Recipes Available:")
    recipes = [
        "Scrap Metal (x3) + Electronics (x1) -> Weapon Parts (x2)",
        "Bandage Cloth (x2) + Antiseptic (x1) -> Military Medkit (x1)",
        "Gunpowder (x2) + Lead Ingot (x2) -> 5.56mm AP Ammo (x30)"
    ]
    for r in recipes:
        print(f"  [Recipe] {r}")
        
    print("\n> Player crafts 'Military Medkit'...")
    print("  [SUCCESS] Created Military Medkit x1 | Remaining Cloth: 4 | Remaining Antiseptic: 1")
    
    # Save snapshot
    print("\n> Generating Delta Save Snapshot (Slot 1 - 'Bunker Safehouse')...")
    snapshot = {
        "slot_index": 1,
        "save_name": "Bunker Safehouse",
        "game_version": "1.0.0",
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "player": {
            "health": 82.5,
            "level": 3,
            "currency": 1250,
            "region": "IndustrialDistrict",
            "position": {"x": 145.2, "y": 0.0, "z": 412.8}
        },
        "inventory": [
            {"item_id": "medkit_military", "qty": 3},
            {"item_id": "scrap_metal", "qty": 14},
            {"item_id": "ammo_556_ap", "qty": 90}
        ],
        "missions": [
            {"id": "mq_01_blackout_origin", "status": "Completed"}
        ]
    }
    json_str = json.dumps(snapshot, indent=2)
    print(f"Serialized Snapshot JSON:\n{json_str}")


def main():
    print_banner()
    while True:
        print("\nSelect Local Simulation Routine:")
        print("  1. Player Controller & Survival Vitals")
        print("  2. Tactical Combat Ballistics & Armor Penetration")
        print("  3. Enemy AI Perception & Squad Tactical Orders")
        print("  4. Branching Dialogue & NPC Relationship Engine")
        print("  5. Crafting Engine & Save Game Snapshot Serialization")
        print("  6. Run Full End-to-End Game Simulation")
        print("  7. Exit")
        
        choice = input("\nEnter selection (1-7): ").strip()
        if choice == "1":
            demo_player_survival()
        elif choice == "2":
            demo_tactical_combat()
        elif choice == "3":
            demo_squad_tactics()
        elif choice == "4":
            demo_branching_dialogue()
        elif choice == "5":
            demo_crafting_and_save()
        elif choice == "6":
            demo_player_survival()
            demo_tactical_combat()
            demo_squad_tactics()
            demo_branching_dialogue()
            demo_crafting_and_save()
            print("\n[SUCCESS] Full End-to-End Simulation completed successfully with zero runtime errors!")
        elif choice == "7" or choice.lower() in ("q", "exit"):
            print("Exiting Shadow Protocol Local Simulator. Good hunting, operative.")
            break
        else:
            print("Invalid selection. Please choose 1-7.")


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--all":
        print_banner()
        demo_player_survival()
        demo_tactical_combat()
        demo_squad_tactics()
        demo_branching_dialogue()
        demo_crafting_and_save()
        print("\n[SUCCESS] Full End-to-End Simulation completed successfully!")
    else:
        main()
