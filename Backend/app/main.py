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

# Enable CORS
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


@app.get("/play", response_class=HTMLResponse, tags=["Playable Game"])
@app.get("/", response_class=HTMLResponse, tags=["Playable Game"])
async def play_game():
    return """<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SHADOW PROTOCOL | Playable Tactical Survival Game</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;700;900&family=JetBrains+Mono:wght@400;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-dark: #07090e;
            --panel-bg: rgba(13, 18, 28, 0.85);
            --panel-border: rgba(0, 240, 255, 0.2);
            --accent-cyan: #00f0ff;
            --accent-emerald: #00ffaa;
            --accent-crimson: #ff0055;
            --accent-amber: #ffaa00;
            --text-primary: #f0f4fc;
            --text-secondary: #8a9bb8;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            user-select: none;
        }

        body {
            background-color: var(--bg-dark);
            color: var(--text-primary);
            font-family: 'Outfit', sans-serif;
            overflow: hidden;
            display: flex;
            flex-direction: column;
            height: 100vh;
        }

        /* Top Nav */
        header {
            background: rgba(10, 13, 20, 0.95);
            backdrop-filter: blur(12px);
            border-bottom: 1px solid var(--panel-border);
            padding: 10px 24px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            z-index: 50;
        }

        .brand {
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .badge {
            background: linear-gradient(135deg, var(--accent-cyan), #0077ff);
            color: #000;
            font-weight: 900;
            font-size: 13px;
            padding: 4px 10px;
            border-radius: 4px;
            letter-spacing: 1px;
        }

        .title {
            font-size: 18px;
            font-weight: 800;
            letter-spacing: 2px;
            text-transform: uppercase;
        }

        .top-stats {
            display: flex;
            gap: 20px;
            font-family: 'JetBrains Mono', monospace;
            font-size: 13px;
        }

        .stat-item {
            display: flex;
            align-items: center;
            gap: 6px;
            background: rgba(255, 255, 255, 0.04);
            border: 1px solid rgba(255, 255, 255, 0.08);
            padding: 4px 12px;
            border-radius: 6px;
        }

        /* Game Layout */
        #game-container {
            position: relative;
            flex: 1;
            display: flex;
            justify-content: center;
            align-items: center;
            background: #050608;
            overflow: hidden;
        }

        canvas {
            display: block;
            box-shadow: 0 0 50px rgba(0, 0, 0, 0.8);
            cursor: crosshair;
        }

        /* HUD Overlays */
        .hud-overlay {
            position: absolute;
            pointer-events: none;
            z-index: 10;
        }

        #hud-vitals {
            bottom: 20px;
            left: 20px;
            display: flex;
            flex-direction: column;
            gap: 8px;
            background: var(--panel-bg);
            border: 1px solid var(--panel-border);
            padding: 14px 18px;
            border-radius: 10px;
            backdrop-filter: blur(10px);
            min-width: 220px;
        }

        .bar-group {
            display: flex;
            flex-direction: column;
            gap: 3px;
            font-family: 'JetBrains Mono', monospace;
            font-size: 11px;
        }

        .bar-label {
            display: flex;
            justify-content: space-between;
            color: var(--text-secondary);
        }

        .bar-container {
            width: 100%;
            height: 7px;
            background: rgba(255, 255, 255, 0.08);
            border-radius: 4px;
            overflow: hidden;
        }

        .bar-fill {
            height: 100%;
            width: 100%;
            border-radius: 4px;
            transition: width 0.15s ease-out;
        }

        .bar-hp { background: linear-gradient(90deg, #ff0055, #ff5577); }
        .bar-stm { background: linear-gradient(90deg, #00f0ff, #00aaff); }
        .bar-arm { background: linear-gradient(90deg, #ffaa00, #ffdd55); }

        #hud-weapons {
            bottom: 20px;
            right: 20px;
            background: var(--panel-bg);
            border: 1px solid var(--panel-border);
            padding: 14px 20px;
            border-radius: 10px;
            backdrop-filter: blur(10px);
            display: flex;
            flex-direction: column;
            align-items: flex-end;
            gap: 4px;
            font-family: 'JetBrains Mono', monospace;
        }

        #weapon-name {
            color: var(--accent-cyan);
            font-size: 14px;
            font-weight: 700;
        }

        #ammo-count {
            font-size: 24px;
            font-weight: 900;
            color: #fff;
        }

        #hud-objective {
            top: 20px;
            left: 20px;
            background: var(--panel-bg);
            border: 1px solid var(--panel-border);
            padding: 12px 18px;
            border-radius: 8px;
            backdrop-filter: blur(10px);
            font-size: 13px;
        }

        .obj-tag {
            color: var(--accent-amber);
            font-family: 'JetBrains Mono', monospace;
            font-size: 11px;
            margin-bottom: 2px;
            text-transform: uppercase;
        }

        /* Controls Floating Panel */
        #hud-controls {
            top: 20px;
            right: 20px;
            background: var(--panel-bg);
            border: 1px solid var(--panel-border);
            padding: 12px 16px;
            border-radius: 8px;
            backdrop-filter: blur(10px);
            font-family: 'JetBrains Mono', monospace;
            font-size: 11px;
            color: var(--text-secondary);
            pointer-events: auto;
        }

        .btn-autoplay {
            margin-top: 8px;
            width: 100%;
            background: linear-gradient(135deg, var(--accent-cyan), #0088ff);
            border: none;
            color: #000;
            font-weight: 800;
            padding: 8px;
            border-radius: 6px;
            cursor: pointer;
            font-family: 'Outfit', sans-serif;
            font-size: 12px;
            letter-spacing: 0.5px;
            transition: transform 0.15s ease;
        }

        .btn-autoplay:hover {
            transform: scale(1.03);
        }

        .btn-autoplay.active {
            background: linear-gradient(135deg, var(--accent-emerald), #00aa66);
            color: #fff;
        }

        /* Event Toast */
        #event-toast {
            position: absolute;
            top: 80px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(0, 240, 255, 0.15);
            border: 1px solid var(--accent-cyan);
            color: #fff;
            padding: 8px 24px;
            border-radius: 20px;
            font-family: 'JetBrains Mono', monospace;
            font-size: 13px;
            opacity: 0;
            transition: opacity 0.3s ease;
            pointer-events: none;
            z-index: 100;
        }
    </style>
</head>
<body>

    <header>
        <div class="brand">
            <span class="badge">TACTICAL</span>
            <span class="title">Shadow Protocol // Live Simulator</span>
        </div>
        <div class="top-stats">
            <div class="stat-item"><span style="color:var(--accent-amber);">SCORE:</span> <span id="score-display">0</span></div>
            <div class="stat-item"><span style="color:var(--accent-cyan);">CREDITS:</span> <span id="credits-display">500</span></div>
            <div class="stat-item"><span style="color:var(--accent-emerald);">LEVEL:</span> <span id="level-display">1</span></div>
            <div class="stat-item"><span style="color:var(--accent-crimson);">KILLS:</span> <span id="kills-display">0</span></div>
        </div>
    </header>

    <div id="game-container">
        <canvas id="gameCanvas" width="1280" height="720"></canvas>

        <div id="event-toast">Mission Started: Infiltrate Sector 7</div>

        <!-- HUD: Objective -->
        <div class="hud-overlay" id="hud-objective">
            <div class="obj-tag">CURRENT OBJECTIVE</div>
            <div id="objective-text" style="font-weight: 700; color: #fff;">Infiltrate Sector 7 Bunker & Eliminate Directorate Sentinels</div>
        </div>

        <!-- HUD: Controls & Autoplay -->
        <div class="hud-overlay" id="hud-controls">
            <div><b style="color:#fff;">[W,A,S,D]</b> Move</div>
            <div><b style="color:#fff;">[MOUSE]</b> Aim & Shoot</div>
            <div><b style="color:#fff;">[SHIFT]</b> Sprint  <b style="color:#fff;">[C]</b> Crouch</div>
            <div><b style="color:#fff;">[1,2,3,4]</b> Switch Weapon</div>
            <div><b style="color:#fff;">[R]</b> Reload  <b style="color:#fff;">[E]</b> Interact/Loot</div>
            <button class="btn-autoplay" id="btn-autopilot" onclick="toggleAutopilot()">🤖 WATCH AI PLAY (AUTOPILOT)</button>
        </div>

        <!-- HUD: Vitals -->
        <div class="hud-overlay" id="hud-vitals">
            <div class="bar-group">
                <div class="bar-label"><span>HEALTH</span><span id="hp-val">100</span></div>
                <div class="bar-container"><div class="bar-fill bar-hp" id="bar-hp" style="width: 100%;"></div></div>
            </div>
            <div class="bar-group">
                <div class="bar-label"><span>STAMINA</span><span id="stm-val">100</span></div>
                <div class="bar-container"><div class="bar-fill bar-stm" id="bar-stm" style="width: 100%;"></div></div>
            </div>
            <div class="bar-group">
                <div class="bar-label"><span>ARMOR</span><span id="arm-val">50</span></div>
                <div class="bar-container"><div class="bar-fill bar-arm" id="bar-arm" style="width: 50%;"></div></div>
            </div>
        </div>

        <!-- HUD: Weapons -->
        <div class="hud-overlay" id="hud-weapons">
            <div id="weapon-name">AR-556 Directorate Rifle</div>
            <div id="ammo-count">30 / 120</div>
            <div style="font-size: 11px; color: var(--text-secondary);">TACTICAL AUTO</div>
        </div>
    </div>

    <script>
        const canvas = document.getElementById('gameCanvas');
        const ctx = canvas.getContext('2d');

        // Audio synthesizer
        const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        function playSound(freq, type, duration, vol=0.1) {
            try {
                const osc = audioCtx.createOscillator();
                const gain = audioCtx.createGain();
                osc.type = type;
                osc.frequency.setValueAtTime(freq, audioCtx.currentTime);
                gain.gain.setValueAtTime(vol, audioCtx.currentTime);
                gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + duration);
                osc.connect(gain);
                gain.connect(audioCtx.destination);
                osc.start();
                osc.stop(audioCtx.currentTime + duration);
            } catch(e){}
        }

        function showToast(msg) {
            const toast = document.getElementById('event-toast');
            toast.textContent = msg;
            toast.style.opacity = '1';
            setTimeout(() => { toast.style.opacity = '0'; }, 2500);
        }

        // Game State
        const Game = {
            width: 1280,
            height: 720,
            autopilot: false,
            score: 0,
            credits: 500,
            level: 1,
            kills: 0,
            keys: {},
            mouse: { x: 640, y: 360, down: false },
            player: {
                x: 200,
                y: 360,
                vx: 0,
                vy: 0,
                angle: 0,
                health: 100,
                maxHealth: 100,
                stamina: 100,
                armor: 50,
                speed: 3.8,
                isCrouching: false,
                isSprinting: false,
                currentWeaponIndex: 0,
                weapons: [
                    { name: "AR-556 Directorate Rifle", damage: 35, range: 450, fireRate: 110, ammo: 30, maxAmmo: 30, reserve: 120, reloadTime: 1400, reloading: false, lastShot: 0, soundFreq: 240 },
                    { name: "Ghost-762 Sniper", damage: 120, range: 700, fireRate: 650, ammo: 5, maxAmmo: 5, reserve: 25, reloadTime: 2200, reloading: false, lastShot: 0, soundFreq: 140 },
                    { name: "Spectre-45 SMG", damage: 22, range: 350, fireRate: 75, ammo: 30, maxAmmo: 30, reserve: 180, reloadTime: 1200, reloading: false, lastShot: 0, soundFreq: 320 },
                    { name: "Breacher-12 Shotgun", damage: 85, range: 240, fireRate: 450, ammo: 8, maxAmmo: 8, reserve: 40, reloadTime: 1800, reloading: false, lastShot: 0, soundFreq: 180 }
                ]
            },
            bullets: [],
            enemies: [],
            loot: [],
            particles: [],
            structures: [],
            directorateBase: { x: 1050, y: 360, radius: 120 }
        };

        // Initialize Map Structures & Loot
        function initMap() {
            Game.structures = [
                { x: 450, y: 150, w: 120, h: 200, label: "REFUELING STATION" },
                { x: 450, y: 420, w: 120, h: 180, label: "STORAGE DEPOT" },
                { x: 750, y: 220, w: 160, h: 280, label: "SECTOR 7 MAINFRAME" }
            ];

            // Spawn Initial Enemies
            spawnEnemy(800, 180, "Scout");
            spawnEnemy(820, 520, "Scout");
            spawnEnemy(1050, 300, "Heavy");
            spawnEnemy(1080, 420, "Commander");
            spawnEnemy(650, 360, "Soldier");

            // Spawn Initial Loot
            spawnLoot(480, 220, "medkit");
            spawnLoot(480, 480, "ammo");
            spawnLoot(780, 360, "cipher");
        }

        function spawnEnemy(x, y, type) {
            let hp = 60, speed = 2.4, color = "#ff4466", radius = 16;
            if (type === "Scout") { hp = 45; speed = 3.2; color = "#ffaa00"; radius = 14; }
            if (type === "Heavy") { hp = 160; speed = 1.4; color = "#ff0055"; radius = 22; }
            if (type === "Commander") { hp = 110; speed = 2.0; color = "#cc00ff"; radius = 18; }

            Game.enemies.push({
                x, y, type, hp, maxHp: hp, speed, color, radius,
                angle: Math.PI,
                state: "PATROL",
                patrolOrigin: { x, y },
                alertness: 0,
                lastShot: 0
            });
        }

        function spawnLoot(x, y, type) {
            Game.loot.push({ x, y, type, radius: 12, collected: false });
        }

        function createParticles(x, y, color, count=8) {
            for (let i = 0; i < count; i++) {
                const angle = Math.random() * Math.PI * 2;
                const spd = Math.random() * 4 + 1;
                Game.particles.push({
                    x, y,
                    vx: Math.cos(angle) * spd,
                    vy: Math.sin(angle) * spd,
                    color,
                    life: 1.0,
                    decay: Math.random() * 0.04 + 0.02
                });
            }
        }

        // Input Listeners
        window.addEventListener('keydown', (e) => {
            Game.keys[e.key.toLowerCase()] = true;
            if (e.key >= '1' && e.key <= '4') {
                Game.player.currentWeaponIndex = parseInt(e.key) - 1;
                updateHUD();
                playSound(400, 'sine', 0.08);
            }
            if (e.key.toLowerCase() === 'r') {
                reloadCurrentWeapon();
            }
        });
        window.addEventListener('keyup', (e) => { Game.keys[e.key.toLowerCase()] = false; });
        canvas.addEventListener('mousemove', (e) => {
            const rect = canvas.getBoundingClientRect();
            Game.mouse.x = (e.clientX - rect.left) * (Game.width / rect.width);
            Game.mouse.y = (e.clientY - rect.top) * (Game.height / rect.height);
        });
        canvas.addEventListener('mousedown', () => { Game.mouse.down = true; });
        canvas.addEventListener('mouseup', () => { Game.mouse.down = false; });

        function toggleAutopilot() {
            Game.autopilot = !Game.autopilot;
            const btn = document.getElementById('btn-autopilot');
            btn.classList.toggle('active', Game.autopilot);
            btn.textContent = Game.autopilot ? "🟢 AUTOPILOT ACTIVE (CLICK TO TAKE CONTROL)" : "🤖 WATCH AI PLAY (AUTOPILOT)";
            showToast(Game.autopilot ? "AI Autopilot Activated: Operative in autonomous mode" : "Manual Control Returned to Player");
        }

        function reloadCurrentWeapon() {
            const w = Game.player.weapons[Game.player.currentWeaponIndex];
            if (w.reloading || w.ammo >= w.maxAmmo || w.reserve <= 0) return;
            w.reloading = true;
            playSound(300, 'triangle', 0.2);
            showToast(`Reloading ${w.name}...`);
            setTimeout(() => {
                const needed = w.maxAmmo - w.ammo;
                const toAdd = Math.min(needed, w.reserve);
                w.ammo += toAdd;
                w.reserve -= toAdd;
                w.reloading = false;
                updateHUD();
                playSound(600, 'triangle', 0.15);
            }, w.reloadTime);
        }

        function shootWeapon(x, y, angle, isPlayer = true, dmg = 35) {
            const w = isPlayer ? Game.player.weapons[Game.player.currentWeaponIndex] : null;
            if (isPlayer) {
                if (w.reloading || w.ammo <= 0) {
                    if (w.ammo <= 0) reloadCurrentWeapon();
                    return;
                }
                w.ammo--;
                playSound(w.soundFreq, 'square', 0.08, 0.15);
            } else {
                playSound(180, 'sawtooth', 0.06, 0.08);
            }

            const spread = (Math.random() - 0.5) * 0.08;
            const finalAngle = angle + spread;
            Game.bullets.push({
                x, y,
                vx: Math.cos(finalAngle) * 14,
                vy: Math.sin(finalAngle) * 14,
                isPlayer,
                damage: dmg,
                distance: 0,
                maxDistance: isPlayer ? w.range : 400
            });
            updateHUD();
        }

        // Main Game Loop
        function update(deltaTime) {
            const p = Game.player;

            // Autopilot AI Behavior
            if (Game.autopilot) {
                let nearestEnemy = null, minDist = 9999;
                for (const e of Game.enemies) {
                    const d = Math.hypot(e.x - p.x, e.y - p.y);
                    if (d < minDist) { minDist = d; nearestEnemy = e; }
                }

                let nearestLoot = null, minLootDist = 9999;
                for (const l of Game.loot) {
                    if (!l.collected) {
                        const d = Math.hypot(l.x - p.x, l.y - p.y);
                        if (d < minLootDist) { minLootDist = d; nearestLoot = l; }
                    }
                }

                if (nearestEnemy && minDist < 450) {
                    p.angle = Math.atan2(nearestEnemy.y - p.y, nearestEnemy.x - p.x);
                    // Maintain tactical distance
                    if (minDist < 200) {
                        p.vx = -Math.cos(p.angle) * p.speed;
                        p.vy = -Math.sin(p.angle) * p.speed;
                    } else if (minDist > 280) {
                        p.vx = Math.cos(p.angle) * p.speed;
                        p.vy = Math.sin(p.angle) * p.speed;
                    } else {
                        // Strafe
                        p.vx = -Math.sin(p.angle) * p.speed;
                        p.vy = Math.cos(p.angle) * p.speed;
                    }

                    // Shoot
                    const now = Date.now();
                    const w = p.weapons[p.currentWeaponIndex];
                    if (now - w.lastShot > w.fireRate && !w.reloading) {
                        shootWeapon(p.x, p.y, p.angle, true, w.damage);
                        w.lastShot = now;
                    }
                } else if (nearestLoot) {
                    const lAngle = Math.atan2(nearestLoot.y - p.y, nearestLoot.x - p.x);
                    p.angle = lAngle;
                    p.vx = Math.cos(lAngle) * p.speed;
                    p.vy = Math.sin(lAngle) * p.speed;
                } else {
                    // Push toward Directorate Mainframe
                    const bAngle = Math.atan2(Game.directorateBase.y - p.y, Game.directorateBase.x - p.x);
                    p.angle = bAngle;
                    p.vx = Math.cos(bAngle) * p.speed;
                    p.vy = Math.sin(bAngle) * p.speed;
                }
            } else {
                // Manual Player Controls
                let moveX = 0, moveY = 0;
                if (Game.keys['w']) moveY -= 1;
                if (Game.keys['s']) moveY += 1;
                if (Game.keys['a']) moveX -= 1;
                if (Game.keys['d']) moveX += 1;

                p.isSprinting = Game.keys['shift'] && p.stamina > 5;
                p.isCrouching = Game.keys['c'];

                let currentSpeed = p.speed;
                if (p.isSprinting) {
                    currentSpeed *= 1.7;
                    p.stamina = Math.max(0, p.stamina - 0.4);
                } else if (p.isCrouching) {
                    currentSpeed *= 0.5;
                } else if (p.stamina < 100) {
                    p.stamina = Math.min(100, p.stamina + 0.25);
                }

                if (moveX !== 0 && moveY !== 0) {
                    moveX *= 0.7071; moveY *= 0.7071;
                }

                p.vx = moveX * currentSpeed;
                p.vy = moveY * currentSpeed;
                p.angle = Math.atan2(Game.mouse.y - p.y, Game.mouse.x - p.x);

                // Manual shooting
                if (Game.mouse.down) {
                    const now = Date.now();
                    const w = p.weapons[p.currentWeaponIndex];
                    if (now - w.lastShot > w.fireRate && !w.reloading) {
                        shootWeapon(p.x, p.y, p.angle, true, w.damage);
                        w.lastShot = now;
                    }
                }
            }

            p.x = Math.max(30, Math.min(Game.width - 30, p.x + p.vx));
            p.y = Math.max(30, Math.min(Game.height - 30, p.y + p.vy));

            // Bullets update
            for (let i = Game.bullets.length - 1; i >= 0; i--) {
                const b = Game.bullets[i];
                b.x += b.vx;
                b.y += b.vy;
                b.distance += Math.hypot(b.vx, b.vy);

                if (b.distance > b.maxDistance || b.x < 0 || b.x > Game.width || b.y < 0 || b.y > Game.height) {
                    Game.bullets.splice(i, 1);
                    continue;
                }

                // Bullet vs Enemies
                if (b.isPlayer) {
                    for (let j = Game.enemies.length - 1; j >= 0; j--) {
                        const e = Game.enemies[j];
                        if (Math.hypot(b.x - e.x, b.y - e.y) < e.radius) {
                            createParticles(b.x, b.y, "#ff3366", 6);
                            e.hp -= b.damage;
                            e.alertness = 100;
                            e.state = "ATTACK";
                            Game.bullets.splice(i, 1);

                            if (e.hp <= 0) {
                                createParticles(e.x, e.y, "#ffaa00", 16);
                                Game.score += (e.type === "Commander" ? 1500 : (e.type === "Heavy" ? 800 : 400));
                                Game.credits += 120;
                                Game.kills++;
                                if (Game.kills % 5 === 0) Game.level++;
                                if (Math.random() < 0.6) spawnLoot(e.x, e.y, Math.random() < 0.5 ? "medkit" : "ammo");
                                showToast(`Directorate ${e.type} Neutralized (+${e.type === 'Commander'?1500:500} pts)`);
                                Game.enemies.splice(j, 1);

                                // Check respawn reinforcements
                                if (Game.enemies.length < 3) {
                                    setTimeout(() => {
                                        spawnEnemy(Game.directorateBase.x + (Math.random()*60-30), Game.directorateBase.y + (Math.random()*60-30), "Soldier");
                                    }, 2000);
                                }
                            }
                            break;
                        }
                    }
                } else {
                    // Bullet vs Player
                    if (Math.hypot(b.x - p.x, b.y - p.y) < 18) {
                        createParticles(b.x, b.y, "#00f0ff", 8);
                        let dmg = b.damage;
                        if (p.armor > 0) {
                            const armorDmg = Math.min(p.armor, dmg * 0.7);
                            p.armor -= armorDmg;
                            dmg -= armorDmg;
                        }
                        p.health = Math.max(0, p.health - dmg);
                        playSound(120, 'sawtooth', 0.15, 0.2);
                        Game.bullets.splice(i, 1);

                        if (p.health <= 0) {
                            showToast("CRITICAL: Operative Down! Respawning at Safehouse...");
                            p.health = 100;
                            p.armor = 50;
                            p.x = 200;
                            p.y = 360;
                        }
                    }
                }
            }

            // Enemies AI Update
            for (const e of Game.enemies) {
                const distToPlayer = Math.hypot(p.x - e.x, p.y - e.y);
                const angleToPlayer = Math.atan2(p.y - e.y, p.x - e.x);

                if (distToPlayer < 380) {
                    e.alertness = Math.min(100, e.alertness + 2);
                    if (e.alertness > 50) e.state = "ATTACK";
                } else {
                    e.alertness = Math.max(0, e.alertness - 0.2);
                    if (e.alertness === 0) e.state = "PATROL";
                }

                if (e.state === "ATTACK") {
                    e.angle = angleToPlayer;
                    if (distToPlayer > 180) {
                        e.x += Math.cos(e.angle) * e.speed;
                        e.y += Math.sin(e.angle) * e.speed;
                    } else if (distToPlayer < 120) {
                        e.x -= Math.cos(e.angle) * (e.speed * 0.8);
                        e.y -= Math.sin(e.angle) * (e.speed * 0.8);
                    }

                    // Enemy Shoot
                    const now = Date.now();
                    if (now - e.lastShot > 1100 && distToPlayer < 350) {
                        shootWeapon(e.x, e.y, e.angle, false, e.type === "Heavy" ? 22 : 14);
                        e.lastShot = now;
                    }
                } else {
                    // Patrol around origin
                    const dOrigin = Math.hypot(e.patrolOrigin.x - e.x, e.patrolOrigin.y - e.y);
                    if (dOrigin > 80) {
                        e.angle = Math.atan2(e.patrolOrigin.y - e.y, e.patrolOrigin.x - e.x);
                    }
                    e.x += Math.cos(e.angle) * (e.speed * 0.4);
                    e.y += Math.sin(e.angle) * (e.speed * 0.4);
                }
            }

            // Loot collection
            for (const l of Game.loot) {
                if (!l.collected && Math.hypot(p.x - l.x, p.y - l.y) < 28) {
                    l.collected = true;
                    createParticles(l.x, l.y, "#00ffaa", 10);
                    playSound(800, 'sine', 0.15, 0.2);

                    if (l.type === "medkit") {
                        p.health = Math.min(100, p.health + 40);
                        showToast("+40 HP: Military Medkit Used");
                    } else if (l.type === "ammo") {
                        for (const w of p.weapons) w.reserve += w.maxAmmo * 2;
                        showToast("+Ammo Cache Restocked");
                    } else if (l.type === "cipher") {
                        Game.score += 5000;
                        Game.credits += 800;
                        showToast("MISSION OBJECTIVE: Blackout Cipher Drive Acquired! (+5000 pts)");
                    }
                }
            }

            // Particles update
            for (let i = Game.particles.length - 1; i >= 0; i--) {
                const pt = Game.particles[i];
                pt.x += pt.vx;
                pt.y += pt.vy;
                pt.life -= pt.decay;
                if (pt.life <= 0) Game.particles.splice(i, 1);
            }

            updateHUD();
        }

        function updateHUD() {
            const p = Game.player;
            const w = p.weapons[p.currentWeaponIndex];
            document.getElementById('hp-val').textContent = Math.round(p.health);
            document.getElementById('bar-hp').style.width = p.health + '%';
            document.getElementById('stm-val').textContent = Math.round(p.stamina);
            document.getElementById('bar-stm').style.width = p.stamina + '%';
            document.getElementById('arm-val').textContent = Math.round(p.armor);
            document.getElementById('bar-arm').style.width = (p.armor * 2) + '%';

            document.getElementById('weapon-name').textContent = w.name;
            document.getElementById('ammo-count').textContent = w.reloading ? "RELOADING..." : `${w.ammo} / ${w.reserve}`;

            document.getElementById('score-display').textContent = Game.score.toLocaleString();
            document.getElementById('credits-display').textContent = Game.credits;
            document.getElementById('level-display').textContent = Game.level;
            document.getElementById('kills-display').textContent = Game.kills;
        }

        // Render Canvas
        function draw() {
            ctx.fillStyle = "#07090e";
            ctx.fillRect(0, 0, Game.width, Game.height);

            // Grid background
            ctx.strokeStyle = "rgba(0, 240, 255, 0.04)";
            ctx.lineWidth = 1;
            for (let x = 0; x < Game.width; x += 40) {
                ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, Game.height); ctx.stroke();
            }
            for (let y = 0; y < Game.height; y += 40) {
                ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(Game.width, y); ctx.stroke();
            }

            // Structures
            for (const s of Game.structures) {
                ctx.fillStyle = "rgba(16, 24, 38, 0.9)";
                ctx.strokeStyle = "rgba(0, 240, 255, 0.25)";
                ctx.lineWidth = 2;
                ctx.fillRect(s.x, s.y, s.w, s.h);
                ctx.strokeRect(s.x, s.y, s.w, s.h);

                ctx.fillStyle = "rgba(0, 240, 255, 0.6)";
                ctx.font = "10px 'JetBrains Mono'";
                ctx.fillText(s.label, s.x + 10, s.y + 20);
            }

            // Directorate Citadel Base
            ctx.fillStyle = "rgba(255, 0, 85, 0.06)";
            ctx.strokeStyle = "rgba(255, 0, 85, 0.35)";
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.arc(Game.directorateBase.x, Game.directorateBase.y, Game.directorateBase.radius, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();
            ctx.fillStyle = "rgba(255, 0, 85, 0.7)";
            ctx.font = "12px 'JetBrains Mono'";
            ctx.fillText("DIRECTORATE CITADEL // RESTRICTED", Game.directorateBase.x - 110, Game.directorateBase.y - Game.directorateBase.radius - 10);

            // Loot Items
            for (const l of Game.loot) {
                if (l.collected) continue;
                ctx.beginPath();
                ctx.arc(l.x, l.y, l.radius, 0, Math.PI * 2);
                ctx.fillStyle = l.type === "medkit" ? "#00ffaa" : (l.type === "cipher" ? "#00f0ff" : "#ffaa00");
                ctx.shadowBlur = 10;
                ctx.shadowColor = ctx.fillStyle;
                ctx.fill();
                ctx.shadowBlur = 0;

                ctx.fillStyle = "#fff";
                ctx.font = "10px 'JetBrains Mono'";
                ctx.fillText(l.type.toUpperCase(), l.x - 14, l.y - 16);
            }

            // Bullets
            for (const b of Game.bullets) {
                ctx.beginPath();
                ctx.arc(b.x, b.y, b.isPlayer ? 3 : 2.5, 0, Math.PI * 2);
                ctx.fillStyle = b.isPlayer ? "#00f0ff" : "#ff3366";
                ctx.shadowBlur = 8;
                ctx.shadowColor = ctx.fillStyle;
                ctx.fill();
                ctx.shadowBlur = 0;
            }

            // Enemies
            for (const e of Game.enemies) {
                // Vision Cone
                ctx.fillStyle = e.state === "ATTACK" ? "rgba(255, 0, 85, 0.12)" : "rgba(255, 170, 0, 0.06)";
                ctx.beginPath();
                ctx.moveTo(e.x, e.y);
                ctx.arc(e.x, e.y, 160, e.angle - 0.6, e.angle + 0.6);
                ctx.closePath();
                ctx.fill();

                // Body
                ctx.beginPath();
                ctx.arc(e.x, e.y, e.radius, 0, Math.PI * 2);
                ctx.fillStyle = e.color;
                ctx.fill();
                ctx.strokeStyle = "#fff";
                ctx.lineWidth = 1.5;
                ctx.stroke();

                // Gun Barrel
                ctx.strokeStyle = "#fff";
                ctx.lineWidth = 3;
                ctx.beginPath();
                ctx.moveTo(e.x, e.y);
                ctx.lineTo(e.x + Math.cos(e.angle) * (e.radius + 10), e.y + Math.sin(e.angle) * (e.radius + 10));
                ctx.stroke();

                // Health bar
                ctx.fillStyle = "rgba(0,0,0,0.6)";
                ctx.fillRect(e.x - 18, e.y - e.radius - 12, 36, 4);
                ctx.fillStyle = "#ff3366";
                ctx.fillRect(e.x - 18, e.y - e.radius - 12, 36 * (e.hp / e.maxHp), 4);

                // Type label
                ctx.fillStyle = "#fff";
                ctx.font = "10px 'JetBrains Mono'";
                ctx.fillText(e.type, e.x - 14, e.y + e.radius + 14);
            }

            // Particles
            for (const pt of Game.particles) {
                ctx.fillStyle = pt.color;
                ctx.globalAlpha = pt.life;
                ctx.fillRect(pt.x, pt.y, 3, 3);
            }
            ctx.globalAlpha = 1.0;

            // Player
            const p = Game.player;
            ctx.beginPath();
            ctx.arc(p.x, p.y, 16, 0, Math.PI * 2);
            ctx.fillStyle = "#00f0ff";
            ctx.shadowBlur = 15;
            ctx.shadowColor = "#00f0ff";
            ctx.fill();
            ctx.shadowBlur = 0;
            ctx.strokeStyle = "#fff";
            ctx.lineWidth = 2;
            ctx.stroke();

            // Player Gun
            ctx.strokeStyle = "#fff";
            ctx.lineWidth = 4;
            ctx.beginPath();
            ctx.moveTo(p.x, p.y);
            ctx.lineTo(p.x + Math.cos(p.angle) * 26, p.y + Math.sin(p.angle) * 26);
            ctx.stroke();

            // Laser Sight
            ctx.strokeStyle = "rgba(0, 240, 255, 0.35)";
            ctx.lineWidth = 1;
            ctx.setLineDash([4, 4]);
            ctx.beginPath();
            ctx.moveTo(p.x + Math.cos(p.angle) * 26, p.y + Math.sin(p.angle) * 26);
            ctx.lineTo(p.x + Math.cos(p.angle) * 600, p.y + Math.sin(p.angle) * 600);
            ctx.stroke();
            ctx.setLineDash([]);
        }

        // Game Loop Runner
        let lastTime = performance.now();
        function loop(now) {
            const dt = (now - lastTime) / 1000;
            lastTime = now;
            update(dt);
            draw();
            requestAnimationFrame(loop);
        }

        initMap();
        requestAnimationFrame(loop);
    </script>
</body>
</html>"""
