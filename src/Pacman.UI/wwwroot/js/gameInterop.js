window.gameInterop = {
    dotNetRef: null,
    animFrameId: null,
    lastTime: 0,
    entities: {},
    audioCtx: null,
    musicInterval: null,

    // ── Init ──────────────────────────────────────────────
    init: function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.lastTime = performance.now();
        this.cacheEntities();
        this.initAudio();
        this.initKeyboard();
    },

    cacheEntities: function () {
        var elements = document.querySelectorAll('.entity');
        elements.forEach(function (el) {
            this.entities[el.id] = el;
        }.bind(this));
    },

    // ── Keyboard (backup for Blazor @onkeydown) ───────────
    initKeyboard: function () {
        var self = this;
        document.addEventListener('keydown', function (e) {
            var dir = 0;
            switch (e.key) {
                case 'ArrowUp':    case 'w': case 'W': dir = 1; break;
                case 'ArrowDown':  case 's': case 'S': dir = 2; break;
                case 'ArrowLeft':  case 'a': case 'A': dir = 3; break;
                case 'ArrowRight': case 'd': case 'D': dir = 4; break;
            }
            if (dir !== 0 && self.dotNetRef) {
                e.preventDefault();
                self.dotNetRef.invokeMethodAsync('HandleDirection', dir);
            }
        });
    },

    // ── Game Loop ─────────────────────────────────────────
    startLoop: function () {
        if (this.animFrameId) return;
        var self = this;
        function loop(timestamp) {
            var deltaTime = (timestamp - self.lastTime) / 1000.0;
            self.lastTime = timestamp;

            if (self.dotNetRef) {
                self.dotNetRef.invokeMethodAsync('GameLoop', deltaTime)
                    .then(function (states) {
                        if (states) {
                            for (var i = 0; i < states.length; i++) {
                                self.updateEntity(states[i]);
                            }
                        }
                    })
                    .catch(function () {
                        self.stopLoop();
                    });
            }

            self.animFrameId = requestAnimationFrame(loop);
        }
        this.animFrameId = requestAnimationFrame(loop);
    },

    stopLoop: function () {
        if (this.animFrameId) {
            cancelAnimationFrame(this.animFrameId);
            this.animFrameId = null;
        }
        this.stopMusic();
    },

    focusGame: function () {
        var el = document.querySelector('.game-frame');
        if (el) el.focus();
    },

    // ── Entity Rendering ──────────────────────────────────
    updateEntity: function (state) {
        var el = this.entities[state.id];
        if (!el) {
            this.cacheEntities();
            el = this.entities[state.id];
            if (!el) return;
        }

        el.style.transform = 'translate(' + state.x + 'px, ' + state.y + 'px)';
        el.style.display = state.visible ? 'block' : 'none';

        if (state.sprite && el.src.indexOf(state.sprite) === -1) {
            el.src = state.sprite;
        }

        // Mouth animation for Pacman
        if (state.id === 'pacman') {
            if (state.isMouthOpen) {
                el.classList.add('mouth-open');
                el.classList.remove('mouth-closed');
            } else {
                el.classList.add('mouth-closed');
                el.classList.remove('mouth-open');
            }
        }

        // Ghost floating — add class if not already present
        if (state.isGhost && !el.classList.contains('ghost-float')) {
            el.classList.add('ghost-float');
        }
    },

    updatePellets: function (mazeJson) {
        var maze = JSON.parse(mazeJson);
        var cells = document.querySelectorAll('.maze-cell');
        var idx = 0;
        for (var y = 0; y < maze.length; y++) {
            for (var x = 0; x < maze[y].length; x++) {
                var cell = cells[idx];
                if (!cell) { idx++; continue; }
                idx++;

                cell.classList.remove('pellet', 'power-pellet', 'empty');

                if (maze[y][x] === 0) {
                    cell.classList.add('pellet');
                } else if (maze[y][x] === 2) {
                    cell.classList.add('power-pellet');
                } else {
                    cell.classList.add('empty');
                }
            }
        }
    },

    // ── Audio System (Web Audio API) ──────────────────────
    initAudio: function () {
        try {
            this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        } catch (e) {
            this.audioCtx = null;
        }
    },

    resumeAudio: function () {
        if (this.audioCtx && this.audioCtx.state === 'suspended') {
            this.audioCtx.resume();
        }
    },

    _beep: function (freq, duration, type, vol) {
        if (!this.audioCtx) return;
        this.resumeAudio();
        type = type || 'square';
        vol = vol || 0.08;
        var ctx = this.audioCtx;
        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.type = type;
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(vol, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start(ctx.currentTime);
        osc.stop(ctx.currentTime + duration);
    },

    _sweep: function (startFreq, endFreq, duration, type, vol) {
        if (!this.audioCtx) return;
        this.resumeAudio();
        type = type || 'square';
        vol = vol || 0.08;
        var ctx = this.audioCtx;
        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.type = type;
        osc.frequency.setValueAtTime(startFreq, ctx.currentTime);
        osc.frequency.linearRampToValueAtTime(endFreq, ctx.currentTime + duration);
        gain.gain.setValueAtTime(vol, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start(ctx.currentTime);
        osc.stop(ctx.currentTime + duration);
    },

    playSound: function (name) {
        switch (name) {
            case 'eat':
                this._beep(600, 0.06, 'square', 0.06);
                break;
            case 'powerup':
                this._sweep(200, 1200, 0.4, 'square', 0.08);
                break;
            case 'death':
                this._sweep(800, 80, 0.7, 'sawtooth', 0.1);
                break;
            case 'ghostEat':
                this._beep(900, 0.08, 'square', 0.07);
                setTimeout(function (self) {
                    self._beep(1200, 0.08, 'square', 0.07);
                }, 80, this);
                break;
            case 'start':
                this._beep(440, 0.1, 'square', 0.06);
                setTimeout(function (self) {
                    self._beep(660, 0.1, 'square', 0.06);
                }, 120, this);
                setTimeout(function (self) {
                    self._beep(880, 0.15, 'square', 0.06);
                }, 240, this);
                break;
        }
    },

    playMusic: function () {
        if (!this.audioCtx || this.musicInterval) return;
        this.resumeAudio();
        var self = this;
        var notes = [220, 277, 330, 277, 220, 247, 277, 330];
        var step = 0;
        this.musicInterval = setInterval(function () {
            if (!self.musicInterval) return;
            self._beep(notes[step % notes.length], 0.12, 'triangle', 0.03);
            step++;
        }, 180);
    },

    stopMusic: function () {
        if (this.musicInterval) {
            clearInterval(this.musicInterval);
            this.musicInterval = null;
        }
    }
};
