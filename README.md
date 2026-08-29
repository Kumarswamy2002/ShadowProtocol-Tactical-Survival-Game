# SHADOW PROTOCOL
### *Open-World Tactical Survival Game & Enterprise Architecture Showcase*

[![CI Pipeline](https://github.com/shadow-protocol/shadow-protocol/actions/workflows/ci.yml/badge.svg)](https://github.com/shadow-protocol/shadow-protocol/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Python](https://img.shields.io/badge/Python-3.12-yellow.svg)](https://www.python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.110+-009688.svg)](https://fastapi.tiangolo.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED.svg)](https://www.docker.com/)

---

## 1. Project Overview

**Shadow Protocol** is a third-person open-world tactical survival game set in the ruined, post-collapse city of **Veyra**. Following a catastrophic global blackout that dismantled modern civilizations and military supply chains, isolated survivor enclaves fight for survival against **The Directorate**—a ruthless paramilitary faction monopolizing critical resource zones.

As an operative seeking the truth behind the Blackout, you navigate a 10-zone interconnected world featuring dynamic weather, tactical ballistic combat, intelligent squad AI, deep crafting mechanics, and branching NPC narratives.

Built not merely as a game but as an **enterprise-grade software engineering portfolio project**, Shadow Protocol implements clean hexagonal architecture, decoupled domain logic, automated testing suites, a Python FastAPI microservice layer, and full Docker containerization.

---

## 2. Architecture & Design

```
+-------------------------------------------------------------------------+
|                           PRESENTATION LAYER                            |
|             Unity Engine / MonoBehaviours / View Models / HUD           |
+------------------------------------+------------------------------------+
                                     |
+------------------------------------v------------------------------------+
|                         APPLICATION / GAME LAYER                        |
|   Player FSM  *  Combat Engine  *  Squad AI  *  Missions  *  Inventory  |
+------------------------------------+------------------------------------+
                                     |
+------------------------------------v------------------------------------+
|                               DOMAIN LAYER                              |
|   Math3D  *  Event Bus  *  State Machine  *  Behavior Tree  *  Pooling  |
+------------------------------------+------------------------------------+
                                     |
+------------------------------------v------------------------------------+
|                       INFRASTRUCTURE / BACKEND                          |
|         FastAPI REST API  *  PostgreSQL Async  *  Redis Cache           |
+-------------------------------------------------------------------------+
```

### Decoupled Domain Design
The game engine's core subsystems (`ShadowProtocol.Core`, `ShadowProtocol.Game`, `ShadowProtocol.AI`) are pure, high-performance C# libraries. They do not depend on the Unity Editor runtime, making them 100% testable in automated CI pipelines using standard `dotnet test` runners.

---

## 3. Key Subsystems & Feature Matrix

| Subsystem | Key Capabilities |
| :--- | :--- |
| **Player Controller** | 10-state FSM (Walk, Run, Sprint, Crouch, Jump, Climb, Vault, Dodge, Swim, Stealth), dynamic stamina drain, survival degradation (hunger/hydration). |
| **Tactical Combat** | Ballistic simulation, headshots ($2.5\times$), critical hit modifiers, distance falloff curves, armor mitigation vs armor penetration, recoil & sway. |
| **Weapons Engine** | Modular firearms (Pistol, SMG, Shotgun, Assault Rifle, Sniper Rifle, Melee), attachment slots (Optics, Muzzles, Mags), durability & ammo reserves. |
| **Enemy & Squad AI** | Sensory perception (Sight FOV cone, sound attenuation), FSM behavior trees (12 states), Squad Commander dynamic orders (Attack, Flank, Search, Retreat, Call Backup). |
| **NPCs & Schedules** | 24-hour daily schedules (Wake, Work, Break, Social, Sleep), faction reputation $(-100\dots+100)$, interactive branching dialogue graphs with condition gates. |
| **Missions & Quests** | Main story, side missions, timed dynamic world events (Convoys, Distress signals), objective trackers, multi-tier reward engines. |
| **Inventory & Crafting** | Slot & weight limits, item stacking, durability degradation, crafting recipes (Workbench, Chemistry, Munitions) with blueprint unlocking. |
| **Skill Progression** | 3 Skill Trees (Combat, Survival, Stealth) with multi-rank nodes and attribute recalculation. |
| **Dynamic World** | 10 Interconnected Veyra regions, 24-hour Day/Night cycle, dynamic weather (Clear, Rain, Fog, Storm) altering visibility and vehicle physics. |
| **Vehicles** | Physics-based driving (Car, Truck, Motorcycle, Armored Vehicle), fuel consumption, vehicle damage model, and trunk storage inventory. |
| **Persistence** | Multi-slot game state serialization, checkpoints, JSON snapshot migrations, cloud save synchronization. |
| **Backend Services** | FastAPI REST service, PostgreSQL async ORM, Redis session/leaderboard caching, JWT Bearer authentication. |

---

## 4. Repository Structure

```
ShadowProtocol/
|-- Core/                     # Event bus, DI, Math3D, State machines, Behavior trees, Pooling
|-- Game/
|   |-- Player/               # Vitals, Attributes, Movement FSM
|   |-- Combat/               # Ballistics, Damage calculator, Hitboxes
|   |-- Weapons/              # Firearm implementations, Attachments, Ammo
|   |-- NPC/                  # NPC Controllers, 24h Daily schedules, Factions
|   |-- Dialogue/             # Node graphs, Branching choices, Context flags
|   |-- Missions/             # Quest engine, Objectives, Dynamic events
|   |-- Inventory/            # Grid inventory, Stacking, Durability
|   |-- Crafting/             # Recipes, Workbenches, Material costs
|   |-- Skills/               # 3-Branch skill trees (Combat, Survival, Stealth)
|   |-- Economy/              # Merchant shops, Dynamic pricing, Trade engine
|   |-- Vehicles/             # Vehicle controllers, Physics, Fuel, Trunk
|   |-- World/                # Day/Night cycle, Weather, 10 Veyra zones
|   |-- SaveSystem/           # Snapshot serializer, Multi-slot save manager
|-- AI/
|   |-- Perception/           # Sight cone, Sound attenuation, Alertness decay
|   |-- CombatAI/             # Archetypes (Scout, Heavy, Sniper, Elite)
|   |-- SquadAI/              # Commander tactics, Dynamic role assignments
|-- UI/                       # HUD ViewModels, Menu adapters
|-- Tools/                    # CLI developer tools & game simulators
|-- Tests/                    # Comprehensive xUnit automated test suite
Backend/                      # FastAPI, PostgreSQL models, JWT Auth, Pytest
Docker/                       # Dockerfile & Docker Compose multi-container stack
Documentation/                # In-depth architectural & API manuals
.github/                      # CI/CD workflows, PR and Issue templates
```

---

## 5. Quick Start & Setup

### Prerequisites
- **.NET 8.0 SDK** (for C# game libraries and test suites)
- **Python 3.12+** (for backend services)
- **Docker & Docker Compose** (optional for local multi-container development)

### Running the .NET Core Test Suite
```bash
dotnet restore ShadowProtocol.sln
dotnet test ShadowProtocol/Tests/ShadowProtocol.Tests.csproj
```

### Running Developer Tools & Simulators
```bash
dotnet run --project ShadowProtocol/Tools/ShadowProtocol.Tools.csproj mission-demo
dotnet run --project ShadowProtocol/Tools/ShadowProtocol.Tools.csproj combat-sim
dotnet run --project ShadowProtocol/Tools/ShadowProtocol.Tools.csproj squad-sim
```

### Running the Backend Service Locally
```bash
cd Backend
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```
Interactive Swagger API documentation available at: `http://localhost:8000/docs`

### Launching with Docker Compose
```bash
cd Docker
docker compose up --build -d
```

---

## 6. Documentation Suite

- [ARCHITECTURE.md](ARCHITECTURE.md) - Deep architectural patterns, clean layers, and event bus design.
- [GAMEPLAY.md](GAMEPLAY.md) - Core gameplay loops, survival systems, combat mechanics, and world regions.
- [AI.md](AI.md) - Mathematical perception models, behavior trees, and squad tactical algorithms.
- [API.md](API.md) - Complete OpenAPI / REST endpoint specifications and authentication guide.
- [DATABASE.md](DATABASE.md) - PostgreSQL schema diagrams, indexing strategies, and Redis caching.
- [DEVELOPMENT.md](DEVELOPMENT.md) - Code style, development workflow, and contribution guide.
- [TESTING.md](TESTING.md) - Testing philosophy, coverage targets, and test runner instructions.
- [SECURITY.md](SECURITY.md) - Threat analysis, JWT secret management, and validation standards.

---

## 7. License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
