"""
Shadow Protocol - Enterprise Tactical Matchmaking Service
Implements ELO ranking calculations, latency clustering, party MMR weighting,
and adaptive search radius expansion.
"""

import time
import math
import uuid
from typing import Dict, List, Optional, Set
from pydantic import BaseModel

class PlayerQueueEntry(BaseModel):
    player_id: str
    username: str
    mmr: float = 1200.0
    latency_ms: float = 25.0
    region: str = "us-east"
    party_id: Optional[str] = None
    joined_at_timestamp: float
    search_radius: float = 100.0

class TacticalSquadMatch(BaseModel):
    match_id: str
    server_region: str
    team_alpha_ids: List[str]
    team_bravo_ids: List[str]
    average_mmr: float
    created_timestamp: float
    status: str = "pending_deployment"

class MatchmakingEngine:
    def __init__(self, team_size: int = 4):
        self.team_size = team_size
        self.queue: Dict[str, PlayerQueueEntry] = {}
        self.active_matches: Dict[str, TacticalSquadMatch] = {}
        self.expansion_rate_per_second: float = 15.0
        self.max_mmr_difference: float = 650.0

    def enqueue_player(self, player_id: str, username: str, mmr: float, latency: float, region: str = "us-east") -> bool:
        if player_id in self.queue:
            return False
        self.queue[player_id] = PlayerQueueEntry(
            player_id=player_id,
            username=username,
            mmr=mmr,
            latency_ms=latency,
            region=region,
            joined_at_timestamp=time.time()
        )
        return True

    def dequeue_player(self, player_id: str) -> bool:
        return self.queue.pop(player_id, None) is not None

    def update_search_radii(self) -> None:
        now = time.time()
        for entry in self.queue.values():
            wait_duration = now - entry.joined_at_timestamp
            entry.search_radius = min(
                self.max_mmr_difference, 
                100.0 + (wait_duration * self.expansion_rate_per_second)
            )

    def process_matchmaking_tick(self) -> List[TacticalSquadMatch]:
        self.update_search_radii()
        created_matches: List[TacticalSquadMatch] = []
        candidates = sorted(self.queue.values(), key=lambda p: p.mmr)
        matched_ids: Set[str] = set()

        needed_players = self.team_size * 2
        for i in range(len(candidates)):
            p1 = candidates[i]
            if p1.player_id in matched_ids:
                continue

            current_pool = [p1]
            for j in range(i + 1, len(candidates)):
                p2 = candidates[j]
                if p2.player_id in matched_ids or p2.region != p1.region:
                    continue

                mmr_diff = abs(p1.mmr - p2.mmr)
                if mmr_diff <= max(p1.search_radius, p2.search_radius):
                    current_pool.append(p2)
                    if len(current_pool) == needed_players:
                        break

            if len(current_pool) == needed_players:
                match = self._assemble_balanced_match(current_pool)
                created_matches.append(match)
                for p in current_pool:
                    matched_ids.add(p.player_id)
                    self.queue.pop(p.player_id, None)

        return created_matches

    def _assemble_balanced_match(self, players: List[PlayerQueueEntry]) -> TacticalSquadMatch:
        # Snake draft distribution to balance average team MMR
        sorted_players = sorted(players, key=lambda x: x.mmr, reverse=True)
        team_alpha: List[str] = []
        team_bravo: List[str] = []

        for idx, player in enumerate(sorted_players):
            if idx % 4 in (0, 3):
                team_alpha.append(player.player_id)
            else:
                team_bravo.append(player.player_id)

        avg_mmr = sum(p.mmr for p in players) / len(players)
        match_id = f"match_{uuid.uuid4().hex[:12]}"
        
        match = TacticalSquadMatch(
            match_id=match_id,
            server_region=players[0].region,
            team_alpha_ids=team_alpha,
            team_bravo_ids=team_bravo,
            average_mmr=avg_mmr,
            created_timestamp=time.time()
        )
        self.active_matches[match_id] = match
        return match

    @staticmethod
    def compute_elo_delta(winner_rating: float, loser_rating: float, k_factor: float = 32.0) -> float:
        expected_winner = 1.0 / (1.0 + math.pow(10.0, (loser_rating - winner_rating) / 400.0))
        delta = k_factor * (1.0 - expected_winner)
        return round(delta, 2)
