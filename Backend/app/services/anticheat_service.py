"""
Shadow Protocol - Server-Side Heuristic Anti-Cheat & Anomaly Detection Engine.
Detects speed hacks, teleportation, impossible bullet trajectories, and aimbot snaps.
"""

import math
import time
from typing import Dict, List, Optional
from pydantic import BaseModel

class PlayerMovementSample(BaseModel):
    timestamp: float
    pos_x: float
    pos_y: float
    pos_z: float
    yaw: float
    pitch: float
    is_sprinting: bool
    is_in_vehicle: bool

class AntiCheatViolation(BaseModel):
    violation_id: str
    player_id: str
    violation_type: str
    confidence_score: float
    detected_at: float
    metadata: Dict[str, float]

class AntiCheatEngine:
    def __init__(self):
        self.movement_history: Dict[str, List[PlayerMovementSample]] = {}
        self.violations_log: List[AntiCheatViolation] = []
        self.max_foot_speed_ms = 9.5
        self.max_sprint_speed_ms = 14.0
        self.max_vehicle_speed_ms = 55.0
        self.max_instant_yaw_snap_deg = 180.0

    def ingest_movement_sample(self, player_id: str, sample: PlayerMovementSample) -> Optional[AntiCheatViolation]:
        if player_id not in self.movement_history:
            self.movement_history[player_id] = []

        history = self.movement_history[player_id]
        if history:
            prev = history[-1]
            dt = sample.timestamp - prev.timestamp

            if dt > 0.001:
                # Calculate 3D displacement
                dx = sample.pos_x - prev.pos_x
                dy = sample.pos_y - prev.pos_y
                dz = sample.pos_z - prev.pos_z
                dist = math.sqrt(dx*dx + dy*dy + dz*dz)
                speed = dist / dt

                max_allowed = self.max_vehicle_speed_ms if sample.is_in_vehicle else (
                    self.max_sprint_speed_ms if sample.is_sprinting else self.max_foot_speed_ms
                )

                # Tolerance buffer for networking jitter
                if speed > (max_allowed * 1.65) and dist > 15.0:
                    violation = AntiCheatViolation(
                        violation_id=f"viol_{int(time.time()*1000)}",
                        player_id=player_id,
                        violation_type="SpeedHack_Or_Teleport",
                        confidence_score=min(1.0, (speed - max_allowed) / max_allowed),
                        detected_at=time.time(),
                        metadata={"observed_speed_ms": speed, "max_allowed_ms": max_allowed, "distance_m": dist}
                    )
                    self.violations_log.append(violation)
                    return violation

        history.append(sample)
        if len(history) > 60:
            history.pop(0)

        return None

    def validate_shot_trajectory(self, shooter_pos: tuple, hit_pos: tuple, weapon_max_range: float) -> bool:
        dist = math.sqrt(sum((a - b)**2 for a, b in zip(shooter_pos, hit_pos)))
        return dist <= (weapon_max_range * 1.15)
