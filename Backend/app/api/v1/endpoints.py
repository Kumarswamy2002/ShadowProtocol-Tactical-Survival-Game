from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, delete
from typing import List
from app.core.database import get_db
from app.api.v1.deps import get_current_player
from app.models.entities import Player, InventoryItem, SaveGame, LeaderboardEntry
from app.schemas.dto import (
    PlayerProfileResponse,
    PlayerProfileUpdate,
    InventorySyncRequest,
    ItemSyncDTO,
    SaveGameCreateRequest,
    SaveGameResponse,
    LeaderboardSubmitRequest,
    LeaderboardEntryResponse
)

# === Player Router ===
player_router = APIRouter(prefix="/player", tags=["Player Profile"])


@player_router.get("/profile", response_model=PlayerProfileResponse)
async def get_profile(player: Player = Depends(get_current_player)):
    return PlayerProfileResponse(
        id=player.id,
        callsign=player.callsign,
        level=player.level,
        experience=player.experience,
        currency=player.currency,
        skill_points=player.skill_points,
        health=player.health,
        stamina=player.stamina,
        armor=player.armor,
        hunger=player.hunger,
        hydration=player.hydration,
        current_region=player.current_region,
        pos_x=player.pos_x,
        pos_y=player.pos_y,
        pos_z=player.pos_z
    )


@player_router.put("/profile", response_model=PlayerProfileResponse)
async def update_profile(
    update: PlayerProfileUpdate,
    player: Player = Depends(get_current_player),
    db: AsyncSession = Depends(get_db)
):
    for key, val in update.model_dump(exclude_unset=True).items():
        setattr(player, key, val)
    await db.commit()
    await db.refresh(player)
    return await get_profile(player)


# === Inventory Router ===
inventory_router = APIRouter(prefix="/inventory", tags=["Inventory"])


@inventory_router.get("", response_model=List[ItemSyncDTO])
async def get_inventory(player: Player = Depends(get_current_player), db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(InventoryItem).where(InventoryItem.player_id == player.id))
    items = result.scalars().all()
    return [
        ItemSyncDTO(
            item_id=item.item_id,
            quantity=item.quantity,
            durability=item.durability,
            slot_index=item.slot_index
        )
        for item in items
    ]


@inventory_router.post("/sync", response_model=List[ItemSyncDTO])
async def sync_inventory(
    req: InventorySyncRequest,
    player: Player = Depends(get_current_player),
    db: AsyncSession = Depends(get_db)
):
    # Clear old items and write current items
    await db.execute(delete(InventoryItem).where(InventoryItem.player_id == player.id))
    for item_dto in req.items:
        db.add(InventoryItem(
            player_id=player.id,
            item_id=item_dto.item_id,
            quantity=item_dto.quantity,
            durability=item_dto.durability,
            slot_index=item_dto.slot_index
        ))
    await db.commit()
    return req.items


# === Save Games Router ===
save_router = APIRouter(prefix="/save", tags=["Cloud Save"])


@save_router.post("", response_model=SaveGameResponse)
async def create_or_update_save(
    req: SaveGameCreateRequest,
    player: Player = Depends(get_current_player),
    db: AsyncSession = Depends(get_db)
):
    result = await db.execute(
        select(SaveGame).where((SaveGame.player_id == player.id) & (SaveGame.slot_index == req.slot_index))
    )
    save_game = result.scalars().first()

    if save_game:
        save_game.save_name = req.save_name
        save_game.game_version = req.game_version
        save_game.snapshot_json = req.snapshot_json
    else:
        save_game = SaveGame(
            player_id=player.id,
            slot_index=req.slot_index,
            save_name=req.save_name,
            game_version=req.game_version,
            snapshot_json=req.snapshot_json
        )
        db.add(save_game)

    await db.commit()
    await db.refresh(save_game)
    return SaveGameResponse(
        id=save_game.id,
        slot_index=save_game.slot_index,
        save_name=save_game.save_name,
        game_version=save_game.game_version,
        created_at=save_game.created_at
    )


@save_router.get("/{slot_index}")
async def get_save_slot(
    slot_index: int,
    player: Player = Depends(get_current_player),
    db: AsyncSession = Depends(get_db)
):
    result = await db.execute(
        select(SaveGame).where((SaveGame.player_id == player.id) & (SaveGame.slot_index == slot_index))
    )
    save_game = result.scalars().first()
    if not save_game:
        raise HTTPException(status_code=404, detail="Save slot not found")
    return {
        "slot_index": save_game.slot_index,
        "save_name": save_game.save_name,
        "game_version": save_game.game_version,
        "snapshot_json": save_game.snapshot_json,
        "created_at": save_game.created_at
    }


# === Leaderboard Router ===
leaderboard_router = APIRouter(prefix="/leaderboard", tags=["Leaderboard"])


@leaderboard_router.post("/submit", response_model=LeaderboardEntryResponse)
async def submit_score(
    req: LeaderboardSubmitRequest,
    player: Player = Depends(get_current_player),
    db: AsyncSession = Depends(get_db)
):
    entry = LeaderboardEntry(
        callsign=player.callsign,
        score=req.score,
        level=req.level,
        missions_completed=req.missions_completed,
        enemies_eliminated=req.enemies_eliminated,
        survival_time_seconds=req.survival_time_seconds
    )
    db.add(entry)
    await db.commit()
    await db.refresh(entry)
    return LeaderboardEntryResponse(
        callsign=entry.callsign,
        score=entry.score,
        level=entry.level,
        missions_completed=entry.missions_completed,
        enemies_eliminated=entry.enemies_eliminated,
        survival_time_seconds=entry.survival_time_seconds,
        recorded_at=entry.recorded_at
    )


@leaderboard_router.get("", response_model=List[LeaderboardEntryResponse])
async def get_top_leaderboard(limit: int = 50, db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(LeaderboardEntry).order_by(LeaderboardEntry.score.desc()).limit(limit))
    entries = result.scalars().all()
    return [
        LeaderboardEntryResponse(
            callsign=e.callsign,
            score=e.score,
            level=e.level,
            missions_completed=e.missions_completed,
            enemies_eliminated=e.enemies_eliminated,
            survival_time_seconds=e.survival_time_seconds,
            recorded_at=e.recorded_at
        )
        for e in entries
    ]
