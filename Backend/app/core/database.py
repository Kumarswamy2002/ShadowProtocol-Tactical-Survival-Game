import os
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession, async_sessionmaker
from sqlalchemy.orm import declarative_base
from app.core.config import settings

# If DATABASE_URL explicitly provided, use it; otherwise, default to SQLite for standalone zero-config local runs
DATABASE_URL = os.getenv("DATABASE_URL")
if not DATABASE_URL:
    # If in Docker or POSTGRES_SERVER is set to a dedicated host
    if settings.POSTGRES_SERVER != "localhost":
        DATABASE_URL = f"postgresql+asyncpg://{settings.POSTGRES_USER}:{settings.POSTGRES_PASSWORD}@{settings.POSTGRES_SERVER}:{settings.POSTGRES_PORT}/{settings.POSTGRES_DB}"
    else:
        DATABASE_URL = "sqlite+aiosqlite:///./shadow_protocol.db"

engine = create_async_engine(
    DATABASE_URL,
    echo=False,
    future=True
)

AsyncSessionLocal = async_sessionmaker(
    bind=engine,
    class_=AsyncSession,
    expire_on_commit=False,
    autocommit=False,
    autoflush=False
)

Base = declarative_base()


async def get_db():
    async with AsyncSessionLocal() as session:
        try:
            yield session
        finally:
            await session.close()
