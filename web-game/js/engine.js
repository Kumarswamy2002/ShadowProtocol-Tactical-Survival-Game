/**
 * Shadow Protocol - 2D Tactical Engine Canvas Renderer & Game Loop
 */

class TacticalEngine {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas ? this.canvas.getContext('2d') : null;
        this.lastTime = performance.now();
        this.entities = [];
        this.particles = [];
        this.camera = { x: 0, y: 0, zoom: 1.0 };
        this.isRunning = false;
    }

    init() {
        if (!this.canvas) return;
        this.resize();
        window.addEventListener('resize', () => this.resize());
        this.isRunning = true;
        requestAnimationFrame((t) => this.gameLoop(t));
    }

    resize() {
        if (!this.canvas) return;
        this.canvas.width = window.innerWidth;
        this.canvas.height = window.innerHeight;
    }

    gameLoop(currentTime) {
        if (!this.isRunning) return;
        const dt = Math.min(0.1, (currentTime - this.lastTime) / 1000.0);
        this.lastTime = currentTime;

        this.update(dt);
        this.render();

        requestAnimationFrame((t) => this.gameLoop(t));
    }

    update(dt) {
        for (let i = this.particles.length - 1; i >= 0; i--) {
            const p = this.particles[i];
            p.x += p.vx * dt;
            p.y += p.vy * dt;
            p.life -= dt;
            if (p.life <= 0) this.particles.splice(i, 1);
        }
    }

    render() {
        if (!this.ctx) return;
        this.ctx.fillStyle = '#0a0d14';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

        // Grid lines
        this.ctx.strokeStyle = 'rgba(0, 255, 204, 0.05)';
        this.ctx.lineWidth = 1;
        const gridSize = 50;
        for (let x = 0; x < this.canvas.width; x += gridSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(x, 0);
            this.ctx.lineTo(x, this.canvas.height);
            this.ctx.stroke();
        }
        for (let y = 0; y < this.canvas.height; y += gridSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(0, y);
            this.ctx.lineTo(this.canvas.width, y);
            this.ctx.stroke();
        }

        // Render particles
        for (const p of this.particles) {
            this.ctx.fillStyle = p.color || '#00ffcc';
            this.ctx.beginPath();
            this.ctx.arc(p.x, p.y, p.radius || 2, 0, Math.PI * 2);
            this.ctx.fill();
        }
    }

    spawnMuzzleFlash(x, y) {
        for (let i = 0; i < 8; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = 50 + Math.random() * 100;
            this.particles.push({
                x, y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                life: 0.15,
                radius: 2,
                color: '#ffcc00'
            });
        }
    }
}

window.TacticalEngine = TacticalEngine;
