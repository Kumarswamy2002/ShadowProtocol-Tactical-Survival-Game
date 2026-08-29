import uuid
from datetime import datetime, timezone
from sqlalchemy import Column, String, Integer, Float, Boolean, DateTime, ForeignKey, JSON, Text
from sqlalchemy.orm import relationship
from app.core.database import Base


class User(Base):
    __tablename__ = "users"

    id = Column(String(36), primary_key=True, default=lambda: str(uuid.uuid4()))
    username = Column(String(50), unique=True, index=True, nullable=False)
    email = Column(String(100), unique=True, index=True, nullable=False)
    hashed_password = Column(String(255), nullable=False)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=lambda: datetime.now(timezone.utc))

    player = relationship("Player", back_populates="user", uselist=False, cascade="all, delete-orphan")


class Player(Base):
    __tablename__ = "players"

    id = Column(String(36), primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String(36), ForeignKey("users.id"), unique=True, nullable=False)
    callsign = Column(String(50), nullable=False)
    level = Column(Integer, default=1)
    experience = Column(Integer, default=0)
    currency = Column(Integer, default=500)
    skill_points = Column(Integer, default=0)
    
    # Survival stats
    health = Column(Float, default=100.0)
    stamina = Column(Float, default=100.0)
    armor = Column(Float, default=50.0)
    hunger = Column(Float, default=100.0)
    hydration = Column(Float, default=100.0)

    # Location
    current_region = Column(String(50), default="OldCity")
    pos_x = Column(Float, default=0.0)
    pos_y = Column(Float, default=0.0)
    pos_z = Column(Float, default=0.0)

    created_at = Column(DateTime, default=lambda: datetime.now(timezone.utc))
    updated_at = Column(DateTime, default=lambda: datetime.now(timezone.utc), onupdate=lambda: datetime.now(timezone.utc))

    user = relationship("User", back_populates="player")
    inventory = relationship("InventoryItem", back_populates="player", cascade="all, delete-orphan")
    save_games = relationship("SaveGame", back_populates="player", cascade="all, delete-orphan")


class InventoryItem(Base):
    __tablename__ = "inventory_items"

    id = Column(String(36), primary_key=True, default=lambda: str(uuid.uuid4()))
    player_id = Column(String(36), ForeignKey("players.id"), nullable=False)
    item_id = Column(String(100), nullable=False)
    quantity = Column(Integer, default=1)
    durability = Column(Float, default=100.0)
    slot_index = Column(Integer, default=0)

    player = relationship("Player", back_populates="inventory")


class SaveGame(Base):
    __tablename__ = "save_games"

    id = Column(String(36), primary_key=True, default=lambda: str(uuid.uuid4()))
    player_id = Column(String(36), ForeignKey("players.id"), nullable=False)
    slot_index = Column(Integer, default=0)
    save_name = Column(String(100), default="QuickSave")
    game_version = Column(String(20), default="1.0.0")
    snapshot_json = Column(Text, nullable=False)
    created_at = Column(DateTime, default=lambda: datetime.now(timezone.utc))

    player = relationship("Player", back_populates="save_games")


class LeaderboardEntry(Base):
    __tablename__ = "leaderboard_entries"

    id = Column(String(36), primary_key=True, default=lambda: str(uuid.uuid4()))
    callsign = Column(String(50), nullable=False, index=True)
    score = Column(Integer, default=0, index=True)
    level = Column(Integer, default=1)
    missions_completed = Column(Integer, default=0)
    enemies_eliminated = Column(Integer, default=0)
    survival_time_seconds = Column(Integer, default=0)
    recorded_at = Column(DateTime, default=lambda: datetime.now(timezone.utc))
