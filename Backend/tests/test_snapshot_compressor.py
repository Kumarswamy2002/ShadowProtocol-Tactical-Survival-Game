from Backend.src.network.snapshot_compressor import SnapshotCompressor

def test_compression_roundtrip():
    state = {"tick": 1024, "entities": [{"id": "p1", "x": 102.5, "y": 99.1, "hp": 100}]}
    compressed = SnapshotCompressor.compress_world_state(state)
    assert len(compressed) > 0
    decompressed = SnapshotCompressor.decompress_world_state(compressed)
    assert decompressed["tick"] == 1024
