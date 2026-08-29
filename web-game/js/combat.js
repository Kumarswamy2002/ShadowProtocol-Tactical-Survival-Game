/**
 * Shadow Protocol - Web Client Combat Simulation & Sound FX Synthesizer
 */

class TacticalCombatController {
    constructor(engine) {
        this.engine = engine;
        this.audioCtx = null;
    }

    initAudio() {
        if (!this.audioCtx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext) this.audioCtx = new AudioContext();
        }
    }

    playGunshotSound(pitch = 180) {
        this.initAudio();
        if (!this.audioCtx) return;

        const osc = this.audioCtx.createOscillator();
        const gain = this.audioCtx.createGain();
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(pitch, this.audioCtx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(30, this.audioCtx.currentTime + 0.15);

        gain.gain.setValueAtTime(0.3, this.audioCtx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, this.audioCtx.currentTime + 0.15);

        osc.connect(gain);
        gain.connect(this.audioCtx.destination);

        osc.start();
        osc.stop(this.audioCtx.currentTime + 0.15);
    }
}

window.TacticalCombatController = TacticalCombatController;
