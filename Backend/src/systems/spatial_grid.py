"""
Spatial Hash Grid for High-Performance 2D Tactical Collision
"""
import math
from typing import Dict, List, Tuple, Any

class SpatialGridSystem:
    def __init__(self, cell_size: int = 64):
        self.cell_size = cell_size
        self.grid: Dict[Tuple[int, int], List[str]] = {}

    def _hash(self, x: float, y: float) -> Tuple[int, int]:
        return (int(x // self.cell_size), int(y // self.cell_size))

    def insert(self, entity_id: str, x: float, y: float):
        key = self._hash(x, y)
        if key not in self.grid:
            self.grid[key] = []
        self.grid[key].append(entity_id)

    def query_radius(self, x: float, y: float, radius: float) -> List[str]:
        min_cell = self._hash(x - radius, y - radius)
        max_cell = self._hash(x + radius, y + radius)
        results = set()
        for cx in range(min_cell[0], max_cell[0] + 1):
            for cy in range(min_cell[1], max_cell[1] + 1):
                if (cx, cy) in self.grid:
                    for entity in self.grid[(cx, cy)]:
                        results.add(entity)
        return list(results)
