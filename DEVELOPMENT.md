# Shadow Protocol - Development & Setup Guide

## 1. Development Environment

### Requirements:
- **.NET 8.0 SDK**
- **Python 3.12+**
- **Git**
- **Docker Desktop** (optional)

## 2. Solution Layout & Projects
- `ShadowProtocol.Core` -> Domain primitives, math, state machine, event bus.
- `ShadowProtocol.Game` -> Player, Combat, Weapons, Inventory, Missions, Economy, Vehicles, World.
- `ShadowProtocol.AI` -> Perception, Archetypes, Squad tactics.
- `ShadowProtocol.UI` -> HUD view models, menu adapters.
- `ShadowProtocol.Tools` -> CLI simulators and editors.
- `ShadowProtocol.Tests` -> xUnit testing suite.

## 3. Git Workflow
- Conventional commits format: `feat(...)`, `fix(...)`, `refactor(...)`, `test(...)`, `docs(...)`.
- Work is merged into `develop` and tagged releases on `main`.
