from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession, async_sessionmaker
from sqlalchemy.orm import declarative_base
from app.core.config import settings

# If no explicit DATABASE_URL, default to local sqlite async for testability or postgres async
DATABASE_URL = settings.DATABASE_URL or f"postgresql+asyncpg://{settings.POSTGRES_USER}:{settings.POSTGRES_PASSWORD}@{settings.POSTGRES_SERVER}:{settings.POSTGRES_PORT}/{settings.POSTGRES_DB}"

# Use aiosqlite fallback if postgres is not available in local test mode
ASYNC_DB_URL = DATABASE_URL if "postgresql" in DATABASE_URL or "sqlite" in DATABASE_URL else "sqlite+aiosqlite:///./shadow_protocol.db"

engine = create_async_engine(
    ASYNC_DB_URL,
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
