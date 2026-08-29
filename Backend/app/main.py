from fastapi import FastAPI
from fastapi.responses import HTMLResponse
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
from app.core.config import settings
from app.core.database import engine, Base
from app.api.v1.auth import router as auth_router
from app.api.v1.endpoints import player_router, inventory_router, save_router, leaderboard_router


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Initialize DB schemas on startup
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    yield


app = FastAPI(
    title=settings.PROJECT_NAME,
    version=settings.VERSION,
    lifespan=lifespan,
    docs_url="/docs",
    redoc_url="/redoc"
)

# Enable CORS for Unity clients and web dashboards
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Register API Routers
app.include_router(auth_router, prefix=settings.API_V1_STR)
app.include_router(player_router, prefix=settings.API_V1_STR)
app.include_router(inventory_router, prefix=settings.API_V1_STR)
app.include_router(save_router, prefix=settings.API_V1_STR)
app.include_router(leaderboard_router, prefix=settings.API_V1_STR)


@app.get("/", response_class=HTMLResponse, tags=["Dashboard"])
async def dashboard():
    return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SHADOW PROTOCOL | Tactical Command Center</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;700;900&family=JetBrains+Mono:wght@400;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-primary: #0a0d14;
            --bg-card: rgba(16, 22, 34, 0.75);
            --bg-card-hover: rgba(24, 33, 52, 0.9);
            --border-glow: rgba(0, 240, 255, 0.25);
            --border-active: #00f0ff;
            --accent-cyan: #00f0ff;
            --accent-emerald: #00ffaa;
            --accent-crimson: #ff0055;
            --accent-amber: #ffaa00;
            --text-primary: #f0f4fc;
            --text-secondary: #8a9bb8;
            --text-muted: #53627c;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            background-color: var(--bg-primary);
            background-image: 
                radial-gradient(circle at 15% 15%, rgba(0, 240, 255, 0.08) 0%, transparent 40%),
                radial-gradient(circle at 85% 85%, rgba(255, 0, 85, 0.06) 0%, transparent 45%),
                linear-gradient(rgba(10, 13, 20, 0.85) 1px, transparent 1px),
                linear-gradient(90deg, rgba(10, 13, 20, 0.85) 1px, transparent 1px);
            background-size: 100% 100%, 100% 100%, 40px 40px, 40px 40px;
            color: var(--text-primary);
            font-family: 'Outfit', sans-serif;
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            overflow-x: hidden;
        }

        /* Glassmorphism Header */
        header {
            background: rgba(10, 13, 20, 0.85);
            backdrop-filter: blur(16px);
            border-bottom: 1px solid rgba(0, 240, 255, 0.15);
            padding: 18px 40px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            position: sticky;
            top: 0;
            z-index: 100;
        }

        .logo-group {
            display: flex;
            align-items: center;
            gap: 14px;
        }

        .logo-badge {
            background: linear-gradient(135deg, var(--accent-cyan), #0077ff);
            color: #000;
            font-weight: 900;
            font-size: 16px;
            padding: 6px 12px;
            border-radius: 6px;
            letter-spacing: 1.5px;
            box-shadow: 0 0 20px rgba(0, 240, 255, 0.4);
        }

        .logo-title {
            font-size: 22px;
            font-weight: 800;
            letter-spacing: 3px;
            text-transform: uppercase;
            background: linear-gradient(90deg, #fff, var(--text-secondary));
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }

        .status-pill {
            display: flex;
            align-items: center;
            gap: 8px;
            background: rgba(0, 255, 170, 0.1);
            border: 1px solid rgba(0, 255, 170, 0.3);
            color: var(--accent-emerald);
            padding: 6px 16px;
            border-radius: 20px;
            font-family: 'JetBrains Mono', monospace;
            font-size: 13px;
            font-weight: 600;
        }

        .pulse-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: var(--accent-emerald);
            box-shadow: 0 0 10px var(--accent-emerald);
            animation: pulse 1.8s infinite;
        }

        @keyframes pulse {
            0%, 100% { opacity: 1; transform: scale(1); }
            50% { opacity: 0.4; transform: scale(0.85); }
        }

        /* Container */
        .container {
            max-width: 1300px;
            margin: 0 auto;
            padding: 40px 24px;
            flex: 1;
            width: 100%;
        }

        /* Hero Banner */
        .hero {
            background: linear-gradient(135deg, rgba(16, 25, 42, 0.8), rgba(12, 17, 28, 0.9));
            border: 1px solid var(--border-glow);
            border-radius: 16px;
            padding: 40px;
            margin-bottom: 36px;
            position: relative;
            overflow: hidden;
            box-shadow: 0 20px 50px rgba(0, 0, 0, 0.5);
        }

        .hero::before {
            content: "";
            position: absolute;
            top: 0;
            left: 0;
            width: 4px;
            height: 100%;
            background: linear-gradient(to bottom, var(--accent-cyan), var(--accent-emerald));
        }

        .hero-tag {
            color: var(--accent-cyan);
            font-family: 'JetBrains Mono', monospace;
            font-size: 13px;
            letter-spacing: 2px;
            text-transform: uppercase;
            margin-bottom: 12px;
        }

        .hero h1 {
            font-size: 38px;
            font-weight: 900;
            letter-spacing: -0.5px;
            margin-bottom: 12px;
            line-height: 1.2;
        }

        .hero p {
            color: var(--text-secondary);
            font-size: 16px;
            max-width: 780px;
            line-height: 1.6;
            margin-bottom: 24px;
        }

        .btn-group {
            display: flex;
            gap: 16px;
            flex-wrap: wrap;
        }

        .btn {
            display: inline-flex;
            align-items: center;
            gap: 10px;
            padding: 12px 24px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 700;
            letter-spacing: 0.5px;
            text-decoration: none;
            cursor: pointer;
            transition: all 0.25s ease;
            font-family: 'Outfit', sans-serif;
            border: none;
        }

        .btn-primary {
            background: linear-gradient(135deg, var(--accent-cyan), #0088ff);
            color: #050811;
            box-shadow: 0 0 25px rgba(0, 240, 255, 0.35);
        }

        .btn-primary:hover {
            transform: translateY(-2px);
            box-shadow: 0 0 35px rgba(0, 240, 255, 0.6);
        }

        .btn-secondary {
            background: rgba(255, 255, 255, 0.05);
            color: var(--text-primary);
            border: 1px solid rgba(255, 255, 255, 0.15);
        }

        .btn-secondary:hover {
            background: rgba(255, 255, 255, 0.1);
            border-color: var(--accent-cyan);
            transform: translateY(-2px);
        }

        /* Grid Cards */
        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
            gap: 24px;
            margin-bottom: 36px;
        }

        .card {
            background: var(--bg-card);
            border: 1px solid rgba(255, 255, 255, 0.08);
            backdrop-filter: blur(12px);
            border-radius: 14px;
            padding: 28px;
            transition: all 0.3s cubic-bezier(0.2, 0.8, 0.2, 1);
            position: relative;
        }

        .card:hover {
            background: var(--bg-card-hover);
            border-color: var(--border-glow);
            transform: translateY(-4px);
            box-shadow: 0 12px 30px rgba(0, 240, 255, 0.1);
        }

        .card-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 16px;
        }

        .card-title {
            font-size: 18px;
            font-weight: 700;
            color: var(--text-primary);
        }

        .card-icon {
            font-size: 22px;
            color: var(--accent-cyan);
        }

        .card-desc {
            color: var(--text-secondary);
            font-size: 14px;
            line-height: 1.6;
            margin-bottom: 20px;
        }

        .endpoint-list {
            list-style: none;
            display: flex;
            flex-direction: column;
            gap: 10px;
        }

        .endpoint-item {
            display: flex;
            align-items: center;
            justify-content: space-between;
            background: rgba(0, 0, 0, 0.3);
            border: 1px solid rgba(255, 255, 255, 0.05);
            padding: 10px 14px;
            border-radius: 6px;
            font-family: 'JetBrains Mono', monospace;
            font-size: 12px;
        }

        .method {
            padding: 2px 6px;
            border-radius: 4px;
            font-weight: 700;
            font-size: 11px;
        }

        .get { background: rgba(0, 255, 170, 0.15); color: var(--accent-emerald); }
        .post { background: rgba(0, 240, 255, 0.15); color: var(--accent-cyan); }
        .put { background: rgba(255, 170, 0, 0.15); color: var(--accent-amber); }

        /* Interactive Console Tester */
        .tester-card {
            background: rgba(12, 16, 26, 0.95);
            border: 1px solid var(--border-glow);
            border-radius: 14px;
            padding: 28px;
            margin-bottom: 36px;
        }

        .tester-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
        }

        .terminal-output {
            background: #05070c;
            border: 1px solid rgba(0, 240, 255, 0.15);
            border-radius: 8px;
            padding: 18px;
            font-family: 'JetBrains Mono', monospace;
            font-size: 13px;
            color: var(--accent-emerald);
            min-height: 140px;
            max-height: 260px;
            overflow-y: auto;
            line-height: 1.6;
            white-space: pre-wrap;
        }

        footer {
            border-top: 1px solid rgba(255, 255, 255, 0.06);
            padding: 24px;
            text-align: center;
            color: var(--text-muted);
            font-size: 13px;
            background: rgba(10, 13, 20, 0.9);
        }
    </style>
</head>
<body>

    <header>
        <div class="logo-group">
            <span class="logo-badge">SP-V1</span>
            <span class="logo-title">Shadow Protocol</span>
        </div>
        <div class="status-pill">
            <span class="pulse-dot"></span>
            SYSTEM ONLINE & OPERATIONAL
        </div>
    </header>

    <div class="container">
        <!-- Hero Section -->
        <section class="hero">
            <div class="hero-tag">// VEYRA TACTICAL COMMAND NODE</div>
            <h1>Shadow Protocol Backend Microservice</h1>
            <p>
                Real-time persistent services for the open-world tactical survival game. Featuring decoupled clean architecture, JWT authentication, asynchronous database persistence, cloud game state snapshotting, inventory synchronization, and competitive leaderboards.
            </p>
            <div class="btn-group">
                <a href="/docs" target="_blank" class="btn btn-primary" id="btn-swagger">
                    <span>⚡</span> Open Interactive Swagger UI
                </a>
                <a href="/redoc" target="_blank" class="btn btn-secondary" id="btn-redoc">
                    <span>📄</span> View ReDoc Specification
                </a>
                <button onclick="pingHealth()" class="btn btn-secondary" id="btn-ping">
                    <span>📡</span> Ping /health Status
                </button>
            </div>
        </section>

        <!-- Live Terminal Inspector -->
        <section class="tester-card">
            <div class="tester-header">
                <div>
                    <h2 style="font-size: 20px; font-weight: 700;">Live Endpoint Inspector</h2>
                    <p style="color: var(--text-secondary); font-size: 14px;">Real-time interactive query engine testing active database and memory subsystems.</p>
                </div>
                <div style="display: flex; gap: 10px;">
                    <button onclick="pingHealth()" class="btn btn-secondary" style="padding: 8px 16px; font-size: 12px;">Ping Health</button>
                    <button onclick="fetchLeaderboard()" class="btn btn-secondary" style="padding: 8px 16px; font-size: 12px;">Top Leaderboard</button>
                </div>
            </div>
            <div class="terminal-output" id="terminal-log">Connecting to Shadow Protocol tactical network node...
[READY] Host: 127.0.0.1:8000 | Status: Healthy | Database: Connected
Click 'Ping Health' or 'Top Leaderboard' above to query live endpoints in real-time.</div>
        </section>

        <!-- Grid of Services -->
        <div class="grid">
            <!-- Auth Service -->
            <div class="card">
                <div class="card-header">
                    <h3 class="card-title">Authentication & Identity</h3>
                    <span class="card-icon">🔐</span>
                </div>
                <p class="card-desc">Operative registration and salted bcrypt credential hashing with JWT access tokens.</p>
                <ul class="endpoint-list">
                    <li class="endpoint-item"><span>/api/v1/auth/register</span><span class="method post">POST</span></li>
                    <li class="endpoint-item"><span>/api/v1/auth/login</span><span class="method post">POST</span></li>
                </ul>
            </div>

            <!-- Player Profile -->
            <div class="card">
                <div class="card-header">
                    <h3 class="card-title">Player Profile & Vitals</h3>
                    <span class="card-icon">🪖</span>
                </div>
                <p class="card-desc">Vitals synchronization (HP, Stamina, Armor, Hunger, Hydration) and GPS world coordinates.</p>
                <ul class="endpoint-list">
                    <li class="endpoint-item"><span>/api/v1/player/profile</span><span class="method get">GET</span></li>
                    <li class="endpoint-item"><span>/api/v1/player/profile</span><span class="method put">PUT</span></li>
                </ul>
            </div>

            <!-- Inventory & Cloud Save -->
            <div class="card">
                <div class="card-header">
                    <h3 class="card-title">Cloud Inventory & Persistence</h3>
                    <span class="card-icon">💾</span>
                </div>
                <p class="card-desc">Multi-slot delta game state snapshots, durability records, and cloud inventory transfer.</p>
                <ul class="endpoint-list">
                    <li class="endpoint-item"><span>/api/v1/inventory/sync</span><span class="method post">POST</span></li>
                    <li class="endpoint-item"><span>/api/v1/save</span><span class="method post">POST</span></li>
                    <li class="endpoint-item"><span>/api/v1/save/{slot_index}</span><span class="method get">GET</span></li>
                </ul>
            </div>

            <!-- Leaderboard -->
            <div class="card">
                <div class="card-header">
                    <h3 class="card-title">Global Leaderboards</h3>
                    <span class="card-icon">🏆</span>
                </div>
                <p class="card-desc">High-throughput ranking engine for survival metrics, mission completions, and operative score.</p>
                <ul class="endpoint-list">
                    <li class="endpoint-item"><span>/api/v1/leaderboard/submit</span><span class="method post">POST</span></li>
                    <li class="endpoint-item"><span>/api/v1/leaderboard</span><span class="method get">GET</span></li>
                </ul>
            </div>
        </div>
    </div>

    <footer>
        &copy; 2026 SHADOW PROTOCOL &bull; Open-World Tactical Survival Game &bull; MIT License
    </footer>

    <script>
        const terminal = document.getElementById('terminal-log');

        async function pingHealth() {
            terminal.textContent = ">> GET /health ...\\n";
            try {
                const res = await fetch('/health');
                const data = await res.json();
                terminal.textContent += JSON.stringify(data, null, 2);
            } catch (err) {
                terminal.textContent += "[ERROR] " + err.message;
            }
        }

        async function fetchLeaderboard() {
            terminal.textContent = ">> GET /api/v1/leaderboard ...\\n";
            try {
                const res = await fetch('/api/v1/leaderboard');
                const data = await res.json();
                terminal.textContent += JSON.stringify(data, null, 2);
            } catch (err) {
                terminal.textContent += "[ERROR] " + err.message;
            }
        }
    </script>
</body>
</html>
    """


@app.get("/health", tags=["Health"])
async def health_check():
    return {
        "status": "healthy",
        "service": settings.PROJECT_NAME,
        "version": settings.VERSION
    }
