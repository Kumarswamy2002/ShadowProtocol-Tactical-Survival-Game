"""
Delta Snapshot Compression for Real-Time Multiplayer
"""
import json
import zlib
from typing import Dict, Any

class SnapshotCompressor:
    @staticmethod
    def compress_world_state(state: Dict[str, Any]) -> bytes:
        raw = json.dumps(state).encode('utf-8')
        return zlib.compress(raw, level=6)

    @staticmethod
    def decompress_world_state(compressed: bytes) -> Dict[str, Any]:
        raw = zlib.decompress(compressed).decode('utf-8')
        return json.loads(raw)
