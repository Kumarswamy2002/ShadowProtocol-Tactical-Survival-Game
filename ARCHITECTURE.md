# Shadow Protocol - Architecture Specification

## 1. Architectural Philosophy

Shadow Protocol is built on **Hexagonal / Clean Architecture** principles to ensure that all core gameplay logic is testable, decoupled, and completely independent of any specific rendering engine or game engine editor runtime.

```
       +-------------------------------------------------------------+
       |                     Presentation Layer                      |
       |  Unity Engine Views, UI Adapters, Audio / Particle Bridges  |
       +------------------------------+------------------------------+
                                      |
       +------------------------------v------------------------------+
       |                      Application Layer                      |
       |    Controllers, State Machines, Behavior Trees, Managers    |
       +------------------------------+------------------------------+
                                      |
       +------------------------------v------------------------------+
       |                        Domain Layer                         |
       |       Pure Entities, Value Objects, Domain Events, Math     |
       +------------------------------+------------------------------+
                                      |
       +------------------------------v------------------------------+
       |                    Infrastructure Layer                     |
       |    JSON Serializer, File System IO, FastAPI Client API      |
       +-------------------------------------------------------------+
```

## 2. Design Patterns Applied

| Pattern | Implementation Location | Purpose |
| :--- | :--- | :--- |
| **State Pattern / FSM** | `ShadowProtocol.Core.FSM` | Controls Player movement states (Walk, Sprint, Climb, Swim) and Enemy AI behaviors. |
| **Behavior Trees** | `ShadowProtocol.Core.BehaviorTree` | Hierarchical decision making for AI investigation, combat positioning, and patrol routines. |
| **Event Bus / Observer** | `ShadowProtocol.Core.Events` | Decouples subsystem interactions (e.g. VitalsChanged, WeaponFired, MissionCompleted). |
| **Object Pooling** | `ShadowProtocol.Core.Pooling` | High-frequency object reuse (bullets, audio triggers, impact particles) with zero GC allocation. |
| **Dependency Injection** | `ShadowProtocol.Core.DI` | Service registration and resolution enabling mock injection during automated testing. |
| **Repository Pattern** | `Backend.app.api.v1.endpoints` | Abstracted persistence operations for user profiles, inventory items, and cloud saves. |

## 3. Communication Flows

Subsystems communicate asynchronously via domain events:
```
Player Action (Shoot)
       |
       v
Firearm.Shoot()  --> [EventBus.Publish(WeaponFiredEvent)]
                           |
            +--------------+--------------+
            |                             |
            v                             v
   AIPerception (Hearing)          HUDViewModel (Ammo Counter)
```

This guarantees that shooting a weapon does not tightly couple the Player controller to the AI subsystem or the HUD canvas.
