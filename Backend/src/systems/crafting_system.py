"""
Crafting & Item Durability System
"""
from typing import Dict, List, Any, Optional

class CraftingSystem:
    RECIPES = {
        "medkit": {"cloth": 2, "antiseptic": 1},
        "improvised_suppressor": {"oil_filter": 1, "duct_tape": 2},
        "reinforced_barricade": {"wood_plank": 4, "scrap_metal": 2}
    }

    @classmethod
    def can_craft(cls, item_name: str, inventory: Dict[str, int]) -> bool:
        if item_name not in cls.RECIPES:
            return False
        req = cls.RECIPES[item_name]
        return all(inventory.get(mat, 0) >= qty for mat, qty in req.items())

    @classmethod
    def craft(cls, item_name: str, inventory: Dict[str, int]) -> Optional[Dict[str, int]]:
        if not cls.can_craft(item_name, inventory):
            return None
        new_inv = dict(inventory)
        for mat, qty in cls.RECIPES[item_name].items():
            new_inv[mat] -= qty
        new_inv[item_name] = new_inv.get(item_name, 0) + 1
        return new_inv
