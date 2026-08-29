from datetime import datetime
from typing import Optional, List, Dict, Any
from pydantic import BaseModel, EmailStr, Field


# Auth Schemas
class UserRegisterRequest(BaseModel):
    username: str = Field(..., min_length=3, max_length=50)
    email: EmailStr
    password: str = Field(..., min_length=6)
    callsign: Optional[str] = None


class UserLoginRequest(BaseModel):
    username: str
    password: str


class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"
    user_id: str
    callsign: str


# Player Profile Schemas
class PlayerProfileResponse(BaseModel):
    id: str
    callsign: str
    level: int
    experience: int
    currency: int
    skill_points: int
    health: float
    stamina: float
    armor: float
    hunger: float
    hydration: float
    current_region: str
    pos_x: float
    pos_y: float
    pos_z: float


class PlayerProfileUpdate(BaseModel):
    health: Optional[float] = None
    stamina: Optional[float] = None
    armor: Optional[float] = None
    hunger: Optional[float] = None
    hydration: Optional[float] = None
    pos_x: Optional[float] = None
    pos_y: Optional[float] = None
    pos_z: Optional[float] = None
    current_region: Optional[str] = None


# Inventory Item Schemas
class ItemSyncDTO(BaseModel):
    item_id: str
    quantity: int
    durability: float
    slot_index: int


class InventorySyncRequest(BaseModel):
    items: List[ItemSyncDTO]


# Save Game Schemas
class SaveGameCreateRequest(BaseModel):
    slot_index: int = 0
    save_name: str = "QuickSave"
    game_version: str = "1.0.0"
    snapshot_json: str


class SaveGameResponse(BaseModel):
    id: str
    slot_index: int
    save_name: str
    game_version: str
    created_at: datetime


# Leaderboard Schemas
class LeaderboardSubmitRequest(BaseModel):
    score: int
    level: int
    missions_completed: int
    enemies_eliminated: int
    survival_time_seconds: int


class LeaderboardEntryResponse(BaseModel):
    callsign: str
    score: int
    level: int
    missions_completed: int
    enemies_eliminated: int
    survival_time_seconds: int
    recorded_at: datetime
