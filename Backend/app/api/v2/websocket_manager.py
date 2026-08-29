"""
Shadow Protocol - High-Performance Real-Time WebSocket Channel Manager.
"""

import asyncio
from typing import Dict, Set
from fastapi import WebSocket

class WebSocketConnectionManager:
    def __init__(self):
        self.active_rooms: Dict[str, Set[WebSocket]] = {}
        self.player_sockets: Dict[str, WebSocket] = {}

    async def connect(self, room_id: str, player_id: str, websocket: WebSocket):
        await websocket.accept()
        if room_id not in self.active_rooms:
            self.active_rooms[room_id] = set()
        self.active_rooms[room_id].add(websocket)
        self.player_sockets[player_id] = websocket

    def disconnect(self, room_id: str, player_id: str, websocket: WebSocket):
        if room_id in self.active_rooms:
            self.active_rooms[room_id].discard(websocket)
            if not self.active_rooms[room_id]:
                del self.active_rooms[room_id]
        self.player_sockets.pop(player_id, None)

    async def broadcast_to_room(self, room_id: str, message: dict):
        if room_id in self.active_rooms:
            dead_sockets = set()
            for connection in self.active_rooms[room_id]:
                try:
                    await connection.send_json(message)
                except Exception:
                    dead_sockets.add(connection)
            for dead in dead_sockets:
                self.active_rooms[room_id].discard(dead)
