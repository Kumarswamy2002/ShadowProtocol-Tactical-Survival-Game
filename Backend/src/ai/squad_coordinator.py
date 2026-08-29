"""
Tactical Squad AI Coordinator
"""
import math
from typing import List, Dict, Any

class SquadCoordinator:
    @staticmethod
    def calculate_flanking_positions(target_x: float, target_y: float, squad_count: int, distance: float = 100.0) -> List[Dict[str, float]]:
        positions = []
        angle_step = (2 * math.pi) / max(1, squad_count)
        for i in range(squad_count):
            angle = i * angle_step
            px = target_x + distance * math.cos(angle)
            py = target_y + distance * math.sin(angle)
            positions.append({"slot": i, "x": round(px, 2), "y": round(py, 2), "angle_rad": round(angle, 2)})
        return positions
