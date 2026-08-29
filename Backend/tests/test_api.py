import pytest
import json


@pytest.mark.asyncio
async def test_health_endpoint(client):
    response = await client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"


@pytest.mark.asyncio
async def test_register_and_login_flow(client):
    # 1. Register
    reg_payload = {
        "username": "shadow_operative",
        "email": "operative@shadowprotocol.com",
        "password": "SuperSecretPassword123!",
        "callsign": "Vanguard-01"
    }
    reg_res = await client.post("/api/v1/auth/register", json=reg_payload)
    assert reg_res.status_code == 201
    reg_data = reg_res.json()
    assert "access_token" in reg_data
    assert reg_data["callsign"] == "Vanguard-01"

    # 2. Login
    login_payload = {
        "username": "shadow_operative",
        "password": "SuperSecretPassword123!"
    }
    login_res = await client.post("/api/v1/auth/login", json=login_payload)
    assert login_res.status_code == 200
    token = login_res.json()["access_token"]

    # 3. Get Player Profile
    headers = {"Authorization": f"Bearer {token}"}
    profile_res = await client.get("/api/v1/player/profile", headers=headers)
    assert profile_res.status_code == 200
    prof_data = profile_res.json()
    assert prof_data["callsign"] == "Vanguard-01"
    assert prof_data["level"] == 1

    # 4. Update Profile
    update_res = await client.put("/api/v1/player/profile", json={"health": 85.0, "current_region": "IndustrialDistrict"}, headers=headers)
    assert update_res.status_code == 200
    assert update_res.json()["health"] == 85.0
    assert update_res.json()["current_region"] == "IndustrialDistrict"

    # 5. Sync Inventory
    inv_payload = {
        "items": [
            {"item_id": "scrap_metal", "quantity": 12, "durability": 100.0, "slot_index": 0},
            {"item_id": "medkit_military", "quantity": 2, "durability": 100.0, "slot_index": 1}
        ]
    }
    inv_res = await client.post("/api/v1/inventory/sync", json=inv_payload, headers=headers)
    assert inv_res.status_code == 200

    get_inv_res = await client.get("/api/v1/inventory", headers=headers)
    assert get_inv_res.status_code == 200
    assert len(get_inv_res.json()) == 2

    # 6. Save Game Cloud Sync
    save_payload = {
        "slot_index": 1,
        "save_name": "Camp Checkpoint",
        "game_version": "1.0.0",
        "snapshot_json": json.dumps({"health": 85.0, "location": "IndustrialDistrict"})
    }
    save_res = await client.post("/api/v1/save", json=save_payload, headers=headers)
    assert save_res.status_code == 200
    assert save_res.json()["slot_index"] == 1

    get_save_res = await client.get("/api/v1/save/1", headers=headers)
    assert get_save_res.status_code == 200
    assert get_save_res.json()["save_name"] == "Camp Checkpoint"

    # 7. Leaderboard Submission
    lb_payload = {
        "score": 15400,
        "level": 5,
        "missions_completed": 8,
        "enemies_eliminated": 42,
        "survival_time_seconds": 3600
    }
    lb_res = await client.post("/api/v1/leaderboard/submit", json=lb_payload, headers=headers)
    assert lb_res.status_code == 200
    assert lb_res.json()["score"] == 15400

    top_lb_res = await client.get("/api/v1/leaderboard")
    assert top_lb_res.status_code == 200
    assert len(top_lb_res.json()) >= 1
