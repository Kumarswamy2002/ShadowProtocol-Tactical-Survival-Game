from Backend.src.ai.squad_coordinator import SquadCoordinator

def test_flanking_positions():
    pos = SquadCoordinator.calculate_flanking_positions(0, 0, 4, 100.0)
    assert len(pos) == 4
    assert pos[0]["x"] == 100.0
    assert pos[0]["y"] == 0.0
