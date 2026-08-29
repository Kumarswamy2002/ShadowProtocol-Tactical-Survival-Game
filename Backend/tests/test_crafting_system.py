from Backend.src.systems.crafting_system import CraftingSystem

def test_crafting_success():
    inv = {"cloth": 3, "antiseptic": 2}
    assert CraftingSystem.can_craft("medkit", inv) is True
    res = CraftingSystem.craft("medkit", inv)
    assert res["medkit"] == 1
    assert res["cloth"] == 1
