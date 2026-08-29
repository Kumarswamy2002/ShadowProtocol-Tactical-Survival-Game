from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select
from app.core.database import get_db
from app.core.security import get_password_hash, verify_password, create_access_token
from app.models.entities import User, Player
from app.schemas.dto import UserRegisterRequest, UserLoginRequest, TokenResponse

router = APIRouter(prefix="/auth", tags=["Authentication"])


@router.post("/register", response_model=TokenResponse, status_code=status.HTTP_201_CREATED)
async def register(req: UserRegisterRequest, db: AsyncSession = Depends(get_db)):
    # Check if username or email exists
    result = await db.execute(select(User).where((User.username == req.username) | (User.email == req.email)))
    existing_user = result.scalars().first()
    if existing_user:
        raise HTTPException(status_code=400, detail="Username or email already registered")

    # Create User
    new_user = User(
        username=req.username,
        email=req.email,
        hashed_password=get_password_hash(req.password)
    )
    db.add(new_user)
    await db.flush()

    # Create Initial Player Profile
    callsign = req.callsign or req.username
    new_player = Player(
        user_id=new_user.id,
        callsign=callsign
    )
    db.add(new_player)
    await db.commit()

    token = create_access_token(subject=new_user.id)
    return TokenResponse(
        access_token=token,
        token_type="bearer",
        user_id=new_user.id,
        callsign=callsign
    )


@router.post("/login", response_model=TokenResponse)
async def login(req: UserLoginRequest, db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(User).where(User.username == req.username))
    user = result.scalars().first()
    if not user or not verify_password(req.password, user.hashed_password):
        raise HTTPException(status_code=401, detail="Invalid username or password")

    player_result = await db.execute(select(Player).where(Player.user_id == user.id))
    player = player_result.scalars().first()

    token = create_access_token(subject=user.id)
    return TokenResponse(
        access_token=token,
        token_type="bearer",
        user_id=user.id,
        callsign=player.callsign if player else user.username
    )
