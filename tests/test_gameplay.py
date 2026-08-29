import pytest
import math
from Backend.app.services.matchmaking_service import MatchmakingEngine, PlayerQueueEntry
from Backend.app.services.anticheat_service import AntiCheatEngine, PlayerMovementSample

def test_matchmaking_queue_and_elo():
    engine = MatchmakingEngine(team_size=2)
    assert engine.enqueue_player("p1", "Alpha", 1200.0, 20.0) is True
    assert engine.enqueue_player("p2", "Bravo", 1250.0, 25.0) is True
    assert engine.enqueue_player("p3", "Charlie", 1180.0, 22.0) is True
    assert engine.enqueue_player("p4", "Delta", 1220.0, 28.0) is True

    matches = engine.process_matchmaking_tick()
    assert len(matches) == 1
    match = matches[0]
    assert len(match.team_alpha_ids) == 2
    assert len(match.team_bravo_ids) == 2
    assert 1180.0 <= match.average_mmr <= 1250.0

def test_anticheat_speed_detection():
    ac = AntiCheatEngine()
    s1 = PlayerMovementSample(timestamp=100.0, pos_x=0.0, pos_y=0.0, pos_z=0.0, yaw=0.0, pitch=0.0, is_sprinting=False, is_in_vehicle=False)
    ac.ingest_movement_sample("player_1", s1)

    # Move 100 meters in 0.1s -> 1000 m/s (speedhack)
    s2 = PlayerMovementSample(timestamp=100.1, pos_x=100.0, pos_y=0.0, pos_z=0.0, yaw=0.0, pitch=0.0, is_sprinting=False, is_in_vehicle=False)
    violation = ac.ingest_movement_sample("player_1", s2)
    assert violation is not None
    assert violation.violation_type == "SpeedHack_Or_Teleport"

def test_trajectory_validation():
    ac = AntiCheatEngine()
    valid = ac.validate_shot_trajectory((0, 0, 0), (100, 0, 0), 200.0)
    assert valid is True
    invalid = ac.validate_shot_trajectory((0, 0, 0), (500, 0, 0), 200.0)
    assert invalid is False
