from Backend.src.systems.spatial_grid import SpatialGridSystem

def test_spatial_grid_insertion_and_query():
    grid = SpatialGridSystem(cell_size=50)
    grid.insert("player_1", 10, 10)
    grid.insert("enemy_1", 20, 20)
    grid.insert("enemy_2", 200, 200)

    nearby = grid.query_radius(15, 15, 30)
    assert "player_1" in nearby
    assert "enemy_1" in nearby
    assert "enemy_2" not in nearby
