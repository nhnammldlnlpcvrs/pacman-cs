window.gameInterop = {
    dotNetRef: null,
    animFrameId: null,
    lastTime: 0,
    entities: {},

    init: function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.lastTime = performance.now();
        this.cacheEntities();
    },

    cacheEntities: function () {
        var elements = document.querySelectorAll('.entity');
        elements.forEach(function (el) {
            this.entities[el.id] = el;
        }.bind(this));
    },

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
    },

    updateEntity: function (state) {
        var el = this.entities[state.id];
        if (!el) return;

        el.style.transform = 'translate(' + state.x + 'px, ' + state.y + 'px)';
        el.style.display = state.visible ? 'block' : 'none';

        if (state.sprite && el.src.indexOf(state.sprite) === -1) {
            el.src = state.sprite;
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
    }
};
