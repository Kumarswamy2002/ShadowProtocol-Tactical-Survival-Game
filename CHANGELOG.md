# Changelog

All notable changes to the **Shadow Protocol** project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-08-29

### Added
- **Core Engine**: Hexagonal architecture, Event Bus, Dependency Injection container, Math3D primitives, Object pooling, State Machine FSM, and Behavior Tree engine.
- **Player & Survival**: 10-state movement FSM, vitals & survival tick engine (Hunger, Hydration, Stamina, Armor), level progression.
- **Combat & Weapons**: Ballistics simulator, recoil curves, armor penetration vs mitigation, weapon attachments, durability, firearm inventory.
- **Enemy & Squad AI**: Sensory perception (Sight cone, Sound propagation), 6 enemy archetypes, 12 AI states, Squad Commander tactical orders (Flanking, Push, Cover, Retreat).
- **NPC & Dialogue**: 24-hour daily schedules, relationship levels, interactive branching dialogue graph with conditional choices.
- **Missions & World**: Main story, side quests, dynamic timed events, objective trackers, 10 interconnected Veyra regions, 24-hour Day/Night cycle, dynamic weather system.
- **Progression & Economy**: Grid/weight inventory, crafting engine with stations and blueprint unlocks, 3-branch skill trees (Combat, Survival, Stealth), merchant shops with reputation discounts.
- **Vehicles & Persistence**: Vehicle physics controller, fuel, damage model, trunk storage, multi-slot game state serialization snapshots.
- **UI Adapters**: Decoupled HUD view models and menu adapters.
- **Backend Microservices**: Python FastAPI service with PostgreSQL async ORM, Redis caching, JWT auth, player profiles, inventory sync, cloud save, and leaderboards.
- **Developer Tools**: CLI simulators for missions, combat ballistics, squad tactics, branching dialogue, and save inspection.
- **Automated Testing**: Comprehensive xUnit tests for C# game subsystems and Pytest suite for backend REST endpoints.
- **DevOps**: Docker Compose multi-container stack and GitHub Actions CI workflow.
- **Documentation**: Full suite of architectural, API, gameplay, database, testing, and security manuals.
