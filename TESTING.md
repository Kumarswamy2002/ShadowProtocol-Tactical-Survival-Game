# Shadow Protocol - Testing Strategy & Quality Assurance

## 1. Testing Strategy

Shadow Protocol enforces strict automated testing across all gameplay layers and backend microservices:

1. **Domain & Application Unit Tests (xUnit)**
   - `PlayerSystemTests`: Validates FSM transitions, stamina burn, starvation, level progression.
   - `CombatAndWeaponTests`: Validates ballistic curves, headshot multipliers, armor penetration.
   - `EnemyAITests`: Validates perception sight cones, hearing attenuation, squad orders.
   - `ProgressionAndEconomyTests`: Validates inventory capacity, crafting recipes, 3-branch skill trees.
   - `SaveAndWorldTests`: Validates JSON snapshot roundtrips and vehicle physics.

2. **Backend API & Database Tests (Pytest + Async SQLite/Postgres)**
   - `test_api.py`: Validates user registration, JWT auth, player profile mutation, inventory sync, cloud saves, and leaderboards.

## 2. Test Execution

```bash
# Run C# game engine test suite
dotnet test ShadowProtocol/Tests/ShadowProtocol.Tests.csproj

# Run Python backend test suite
cd Backend
pytest tests/ -v
```
