"""
Shadow Protocol - Real-Time Gameplay Analytics & Telemetry Aggregator.
"""

from typing import Dict, List
import time
from pydantic import BaseModel

class GameEventPayload(BaseModel):
    event_name: str
    player_id: str
    session_id: str
    timestamp: float
    properties: Dict[str, float]

class AnalyticsService:
    def __init__(self):
        self.event_stream: List[GameEventPayload] = []
        self.kill_counts_by_weapon: Dict[str, int] = {}
        self.deaths_by_zone: Dict[str, int] = {}

    def track_event(self, event: GameEventPayload):
        self.event_stream.append(event)
        if event.event_name == "player_kill":
            wpn = str(event.properties.get("weapon_id", "unknown"))
            self.kill_counts_by_weapon[wpn] = self.kill_counts_by_weapon.get(wpn, 0) + 1
        elif event.event_name == "player_death":
            zone = str(event.properties.get("zone_id", "unknown"))
            self.deaths_by_zone[zone] = self.deaths_by_zone.get(zone, 0) + 1

    def get_weapon_balance_metrics(self) -> Dict[str, int]:
        return dict(sorted(self.kill_counts_by_weapon.items(), key=lambda x: -x[1]))
