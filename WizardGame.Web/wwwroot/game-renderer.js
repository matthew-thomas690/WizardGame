export function init(hostElement, cellSize) {
    if (!hostElement) {
        throw new Error('Host element not provided.');
    }
    if (!window.PIXI) {
        throw new Error('PixiJS not loaded.');
    }

    const density = window.devicePixelRatio || 1;
    const adjustedCellSize = cellSize / density;

    const app = new window.PIXI.Application({
        background: 0x141414,
        antialias: false,
        resolution: density,
        autoDensity: true
    });

    hostElement.innerHTML = '';
    hostElement.appendChild(app.view);

    const state = {
        app,
        cellSize: adjustedCellSize,
        width: 0,
        height: 0,
        terrainSprites: [],
        overlaySprites: [],
        lemmingSprites: [],
        textures: createTextures(app, adjustedCellSize),
        containers: {
            terrain: new window.PIXI.Container(),
            overlayImage: new window.PIXI.Container(),
            overlayMarkers: new window.PIXI.Container(),
            lemmings: new window.PIXI.Container()
        },
        overlayImageSprite: null,
        overlayImageNaturalWidth: 0,
        overlayImageNaturalHeight: 0,
        overlayCanvas: null,
        overlayContext: null,
        overlayImageData: null,
        overlayTileWidth: 0,
        overlayTileHeight: 0,
        clearedTiles: null,
        clickHandler: null,
        clickTarget: null
    };

    app.stage.addChild(state.containers.terrain);
    app.stage.addChild(state.containers.overlayImage);
    app.stage.addChild(state.containers.overlayMarkers);
    app.stage.addChild(state.containers.lemmings);

    hostElement.__pixiState = state;
    return true;
}

export function renderState(hostElement, renderState) {
    const state = hostElement.__pixiState;
    if (!state) {
        return;
    }

    const width = renderState.width ?? 0;
    const height = renderState.height ?? 0;

    ensureGrid(state, width, height);
    updateTerrain(state, renderState.tiles);
    updateOverlayImagePixels(state, renderState.lemmings);
    updateOverlay(state, renderState.spawns, renderState.exits);
    updateLemmings(state, renderState.lemmings);
}

export function setOverlayImage(hostElement, base64, mimeType) {
    const state = hostElement.__pixiState;
    if (!state || !base64) {
        return;
    }

    const dataUrl = base64.startsWith('data:')
        ? base64
        : `data:${mimeType || 'image/png'};base64,${base64}`;

    const image = new Image();
    image.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = image.width;
        canvas.height = image.height;
        const context = canvas.getContext('2d');
        context.clearRect(0, 0, canvas.width, canvas.height);
        context.drawImage(image, 0, 0);
        const texture = window.PIXI.Texture.from(canvas);

        if (!state.overlayImageSprite) {
            state.overlayImageSprite = new window.PIXI.Sprite(texture);
            state.containers.overlayImage.addChild(state.overlayImageSprite);
        } else {
            state.overlayImageSprite.texture = texture;
        }

        state.overlayCanvas = canvas;
        state.overlayContext = context;
        state.overlayImageData = context.getImageData(0, 0, canvas.width, canvas.height);
        state.overlayImageSprite.x = 0;
        state.overlayImageSprite.y = 0;
        state.overlayImageNaturalWidth = image.width;
        state.overlayImageNaturalHeight = image.height;
        state.overlayTileWidth = 0;
        state.overlayTileHeight = 0;
        state.clearedTiles = null;
        resizeOverlayImage(state);
    };

    image.src = dataUrl;
}

export function registerClickHandler(hostElement, dotNetRef) {
    const state = hostElement.__pixiState;
    if (!state || !dotNetRef) {
        return;
    }

    const target = state.app.view;
    if (state.clickHandler && state.clickTarget) {
        state.clickTarget.removeEventListener('click', state.clickHandler);
    }

    const handler = (event) => {
        if (state.width <= 0 || state.height <= 0) {
            return;
        }

        const rect = target.getBoundingClientRect();
        const localX = event.clientX - rect.left;
        const localY = event.clientY - rect.top;
        if (localX < 0 || localY < 0 || localX > rect.width || localY > rect.height) {
            return;
        }

        const worldX = (localX / rect.width) * state.width;
        const worldY = (localY / rect.height) * state.height;
        const clampedX = Math.min(Math.max(worldX, 0), state.width - 0.0001);
        const clampedY = Math.min(Math.max(worldY, 0), state.height - 0.0001);
        const invoke = dotNetRef.invokeMethodAsync('HandleCanvasClick', clampedX, clampedY);
        if (invoke && typeof invoke.catch === 'function') {
            invoke.catch(() => {});
        }
    };

    state.clickHandler = handler;
    state.clickTarget = target;
    target.addEventListener('click', handler);
}

export function unregisterClickHandler(hostElement) {
    const state = hostElement.__pixiState;
    if (!state || !state.clickHandler || !state.clickTarget) {
        return;
    }

    state.clickTarget.removeEventListener('click', state.clickHandler);
    state.clickHandler = null;
    state.clickTarget = null;
}

function createTextures(app, cellSize) {
    const makeRect = (color, alpha = 1) => {
        const graphics = new window.PIXI.Graphics();
        graphics.beginFill(color, alpha);
        graphics.drawRect(0, 0, cellSize, cellSize);
        graphics.endFill();
        return app.renderer.generateTexture(graphics);
    };

    return {
        empty: makeRect(0x000000, 0),
        solid: makeRect(0x5b4636),
        spawn: makeRect(0x2f7ed8),
        exit: makeRect(0xd9534f),
        lemming: makeRect(0x4caf50)
    };
}

function ensureGrid(state, width, height) {
    if (state.width === width && state.height === height) {
        return;
    }

    state.width = width;
    state.height = height;
    state.clearedTiles = null;
    state.overlayTileWidth = 0;
    state.overlayTileHeight = 0;

    state.containers.terrain.removeChildren();
    state.containers.overlayMarkers.removeChildren();
    state.terrainSprites = [];
    state.overlaySprites = [];

    const total = width * height;
    for (let i = 0; i < total; i++) {
        const x = (i % width) * state.cellSize;
        const y = Math.floor(i / width) * state.cellSize;

        const terrainSprite = new window.PIXI.Sprite(state.textures.empty);
        terrainSprite.x = x;
        terrainSprite.y = y;
        state.containers.terrain.addChild(terrainSprite);
        state.terrainSprites.push(terrainSprite);

        const overlaySprite = new window.PIXI.Sprite(state.textures.empty);
        overlaySprite.x = x;
        overlaySprite.y = y;
        state.containers.overlayMarkers.addChild(overlaySprite);
        state.overlaySprites.push(overlaySprite);
    }

    state.app.renderer.resize(width * state.cellSize, height * state.cellSize);
    resizeOverlayImage(state);
}

function updateTerrain(state, rows) {
    const width = state.width;
    for (let y = 0; y < state.height; y++) {
        const row = rows[y] || '';
        for (let x = 0; x < width; x++) {
            const index = y * width + x;
            const tile = row[x];
            state.terrainSprites[index].texture = tile === '#' ? state.textures.solid : state.textures.empty;
        }
    }
}

function updateOverlayImagePixels(state, lemmings) {
    if (!state.overlayImageSprite || !state.overlayCanvas || !state.overlayContext || !state.overlayImageData) {
        return;
    }

    const width = state.width;
    const height = state.height;
    if (width <= 0 || height <= 0) {
        return;
    }

    if (!state.overlayTileWidth || !state.overlayTileHeight) {
        if (state.overlayImageNaturalWidth > 0 && state.overlayImageNaturalHeight > 0) {
            state.overlayTileWidth = Math.floor(state.overlayImageNaturalWidth / width);
            state.overlayTileHeight = Math.floor(state.overlayImageNaturalHeight / height);
        }
    }

    const tileW = state.overlayTileWidth;
    const tileH = state.overlayTileHeight;
    if (tileW <= 0 || tileH <= 0) {
        return;
    }

    if (!state.clearedTiles || state.clearedTiles.length !== width * height) {
        state.clearedTiles = new Array(width * height).fill(false);
    }

    const data = state.overlayImageData.data;
    let didChange = false;

    if (Array.isArray(lemmings)) {
        const epsilon = 0.001;
        for (const lemming of lemmings) {
            if (!lemming || (!lemming.isDigger && !lemming.isBasher && !lemming.isMiner)) {
                continue;
            }

            const leftCell = Math.floor(lemming.x + epsilon);
            const rightCell = Math.floor(lemming.x + lemming.width - epsilon);
            const topCell = Math.floor(lemming.y + epsilon);
            const bottomCell = Math.floor(lemming.y + lemming.height - epsilon);

            for (let y = topCell; y <= bottomCell; y++) {
                if (y < 0 || y >= height) {
                    continue;
                }

                for (let x = leftCell; x <= rightCell; x++) {
                    if (x < 0 || x >= width) {
                        continue;
                    }

                    const index = (y * width) + x;
                    if (state.clearedTiles[index]) {
                        continue;
                    }

                    state.clearedTiles[index] = true;
                    clearTilePixels(state, data, x, y, tileW, tileH);
                    didChange = true;
                }
            }
        }
    }

    if (didChange) {
        state.overlayContext.putImageData(state.overlayImageData, 0, 0);
        if (state.overlayImageSprite.texture) {
            state.overlayImageSprite.texture.update();
        }
    }
}

function clearTilePixels(state, data, tileX, tileY, tileW, tileH) {
    const pixelStartX = tileX * tileW;
    const pixelStartY = tileY * tileH;
    for (let py = 0; py < tileH; py++) {
        const rowStart = ((pixelStartY + py) * state.overlayImageNaturalWidth + pixelStartX) * 4;
        for (let px = 0; px < tileW; px++) {
            data[rowStart + (px * 4) + 3] = 0;
        }
    }
}

function updateOverlay(state, spawns, exits) {
    const width = state.width;
    for (let i = 0; i < state.overlaySprites.length; i++) {
        state.overlaySprites[i].texture = state.textures.empty;
    }

    if (Array.isArray(spawns)) {
        for (const spawn of spawns) {
            const index = spawn.y * width + spawn.x;
            if (index >= 0 && index < state.overlaySprites.length) {
                state.overlaySprites[index].texture = state.textures.spawn;
            }
        }
    }

    if (Array.isArray(exits)) {
        for (const exit of exits) {
            const index = exit.y * width + exit.x;
            if (index >= 0 && index < state.overlaySprites.length) {
                state.overlaySprites[index].texture = state.textures.exit;
            }
        }
    }
}

function updateLemmings(state, lemmings) {
    const count = Array.isArray(lemmings) ? lemmings.length : 0;
    while (state.lemmingSprites.length < count) {
        const sprite = new window.PIXI.Sprite(state.textures.lemming);
        sprite.anchor.set(0, 0);
        state.containers.lemmings.addChild(sprite);
        state.lemmingSprites.push(sprite);
    }

    for (let i = 0; i < state.lemmingSprites.length; i++) {
        const sprite = state.lemmingSprites[i];
        if (i >= count) {
            sprite.visible = false;
            continue;
        }

        const lemming = lemmings[i];
        sprite.visible = true;
        sprite.x = lemming.x * state.cellSize;
        sprite.y = lemming.y * state.cellSize;
        sprite.width = lemming.width * state.cellSize;
        sprite.height = lemming.height * state.cellSize;
    }
}

function resizeOverlayImage(state) {
    if (!state.overlayImageSprite) {
        return;
    }

    if (state.width > 0 && state.height > 0) {
        state.overlayImageSprite.width = state.width * state.cellSize;
        state.overlayImageSprite.height = state.height * state.cellSize;
        return;
    }

    if (state.overlayImageNaturalWidth > 0 && state.overlayImageNaturalHeight > 0) {
        state.overlayImageSprite.width = state.overlayImageNaturalWidth;
        state.overlayImageSprite.height = state.overlayImageNaturalHeight;
    }
}
