const terrainSeedElement = document.getElementById("terrain-seed");
const terrainMapStatusElement = document.getElementById("terrain-map-status");
const streamStatusElement = document.getElementById("stream-status");
const canvas = document.getElementById("sim-canvas");
const context = canvas.getContext("2d");

const minZoom = 0.25;
const maxZoom = 4;
let zoom = 1;
let terrainTextureCanvas = null;
let viewCenterX = 0;
let viewCenterY = 0;
let isDragging = false;
let lastDragPoint = null;
const actors = new Map();
let lastTickSequence = null;
let lastTickAppliedAtMs = 0;
let tickIntervalMs = 100;
let animationFrameRequestId = null;

function clamp(value, minValue, maxValue) {
    return Math.min(maxValue, Math.max(minValue, value));
}

function wrapCoordinate(value, size) {
    if (size <= 0) {
        return value;
    }

    return ((value % size) + size) % size;
}

function interpolateWrapped(previous, current, alpha, size) {
    if (size <= 0) {
        return previous + ((current - previous) * alpha);
    }

    let delta = current - previous;
    const halfSize = size * 0.5;

    if (delta > halfSize) {
        delta -= size;
    } else if (delta < -halfSize) {
        delta += size;
    }

    return wrapCoordinate(previous + (delta * alpha), size);
}

function getTerrainSize() {
    if (terrainTextureCanvas === null) {
        return 0;
    }

    return terrainTextureCanvas.width;
}

function updateActorState(actor, x, y) {
    actor.previousX = actor.currentX;
    actor.previousY = actor.currentY;
    actor.currentX = x;
    actor.currentY = y;
}

function upsertActor(entityId, x, y) {
    const existingActor = actors.get(entityId);

    if (existingActor !== undefined) {
        updateActorState(existingActor, x, y);
        return;
    }

    actors.set(entityId, {
        previousX: x,
        previousY: y,
        currentX: x,
        currentY: y,
        drawX: x,
        drawY: y,
    });
}

function updateInterpolatedActors(nowMs) {
    const terrainSize = getTerrainSize();
    const hasTickAnchor = lastTickAppliedAtMs > 0;
    const rawAlpha = hasTickAnchor ? ((nowMs - lastTickAppliedAtMs) / tickIntervalMs) : 1;
    const alpha = clamp(rawAlpha, 0, 1);

    for (const actor of actors.values()) {
        actor.drawX = interpolateWrapped(actor.previousX, actor.currentX, alpha, terrainSize);
        actor.drawY = interpolateWrapped(actor.previousY, actor.currentY, alpha, terrainSize);
    }
}

function renderFrame(nowMs) {
    updateInterpolatedActors(nowMs);
    renderTerrain();
    animationFrameRequestId = requestAnimationFrame(renderFrame);
}

function startRenderLoop() {
    if (animationFrameRequestId !== null) {
        return;
    }

    animationFrameRequestId = requestAnimationFrame(renderFrame);
}

function eventToCanvasPoint(event) {
    const rect = canvas.getBoundingClientRect();
    return {
        x: (event.clientX - rect.left) * (canvas.width / rect.width),
        y: (event.clientY - rect.top) * (canvas.height / rect.height),
    };
}

function getBaseTerrainScale() {
    if (terrainTextureCanvas === null) {
        return 1;
    }

    const terrainSize = terrainTextureCanvas.width;
    return Math.min(canvas.width / terrainSize, canvas.height / terrainSize);
}

function getDrawScale() {
    return getBaseTerrainScale() * zoom;
}

function clampZoom(nextZoom) {
    return Math.min(maxZoom, Math.max(minZoom, nextZoom));
}

function zoomAtCanvasPoint(nextZoom, anchorX, anchorY) {
    const clampedZoom = clampZoom(nextZoom);
    if (terrainTextureCanvas === null || Math.abs(clampedZoom - zoom) < 0.0001) {
        zoom = clampedZoom;
        return;
    }

    const currentScale = getDrawScale();
    const nextScale = getBaseTerrainScale() * clampedZoom;

    const worldX = viewCenterX + ((anchorX - canvas.width * 0.5) / currentScale);
    const worldY = viewCenterY + ((anchorY - canvas.height * 0.5) / currentScale);

    zoom = clampedZoom;
    viewCenterX = worldX - ((anchorX - canvas.width * 0.5) / nextScale);
    viewCenterY = worldY - ((anchorY - canvas.height * 0.5) / nextScale);
}

function renderPlaceholder() {
    if (context === null) {
        return;
    }

    context.clearRect(0, 0, canvas.width, canvas.height);
    context.save();
    context.scale(zoom, zoom);

    const gradient = context.createLinearGradient(0, 0, 0, canvas.height);
    gradient.addColorStop(0, "#7f9f72");
    gradient.addColorStop(1, "#345244");
    context.fillStyle = gradient;
    context.fillRect(0, 0, canvas.width, canvas.height);

    context.fillStyle = "rgba(239, 225, 191, 0.65)";
    context.fillRect(40, 48, 180, 92);
    context.fillStyle = "#1c1a16";
    context.font = "28px Georgia";
    context.fillText("Viewport Scaffold", 56, 102);

    context.restore();
}

function decodeBase64(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);

    for (let i = 0; i < binary.length; i += 1) {
        bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
}

function createTerrainTexture(size, heightBytes) {
    const textureCanvas = document.createElement("canvas");
    textureCanvas.width = size;
    textureCanvas.height = size;

    const textureContext = textureCanvas.getContext("2d");
    if (textureContext === null) {
        throw new Error("Unable to initialize terrain texture canvas context.");
    }

    const imageData = textureContext.createImageData(size, size);
    const rgba = imageData.data;

    for (let i = 0; i < heightBytes.length; i += 1) {
        const grayscale = heightBytes[i];
        const pixelOffset = i * 4;
        rgba[pixelOffset] = grayscale;
        rgba[pixelOffset + 1] = grayscale;
        rgba[pixelOffset + 2] = grayscale;
        rgba[pixelOffset + 3] = 255;
    }

    textureContext.putImageData(imageData, 0, 0);
    return textureCanvas;
}

function renderTerrain() {
    if (context === null) {
        return;
    }

    if (terrainTextureCanvas === null) {
        renderPlaceholder();
        return;
    }

    const drawScale = getDrawScale();

    context.clearRect(0, 0, canvas.width, canvas.height);
    context.save();
    context.imageSmoothingEnabled = false;
    context.setTransform(
        drawScale,
        0,
        0,
        drawScale,
        (canvas.width * 0.5) - (viewCenterX * drawScale),
        (canvas.height * 0.5) - (viewCenterY * drawScale));

    context.drawImage(terrainTextureCanvas, 0, 0, terrainTextureCanvas.width, terrainTextureCanvas.height);

    for (const actor of actors.values()) {
        context.fillStyle = "#f7f3e6";
        context.beginPath();
        context.arc(actor.drawX, actor.drawY, 3.5, 0, Math.PI * 2);
        context.fill();

        context.strokeStyle = "#1c1a16";
        context.lineWidth = 0.7;
        context.stroke();
    }

    context.restore();
}

function updateTerrainMapStatus(size, byteCount, decodeDurationMs, renderDurationMs) {
    if (terrainMapStatusElement === null) {
        return;
    }

    terrainMapStatusElement.textContent = JSON.stringify({
        size,
        tileCount: size * size,
        byteCount,
        decodeMs: Number(decodeDurationMs.toFixed(2)),
        renderMs: Number(renderDurationMs.toFixed(2)),
    }, null, 2);
}

async function loadTerrainSeed() {
    const response = await fetch("/api/terrain/seed");

    if (!response.ok) {
        throw new Error(`Failed to load terrain seed (${response.status}).`);
    }

    const payload = await response.json();
    terrainSeedElement.textContent = JSON.stringify(payload, null, 2);
}

async function loadTerrainHeightMap() {
    const response = await fetch("/api/terrain/heightmap");

    if (!response.ok) {
        throw new Error(`Failed to load terrain height map (${response.status}).`);
    }

    const payload = await response.json();
    const decodeStart = performance.now();
    const size = payload.size;
    const heightBytes = decodeBase64(payload.heightDataBase64);

    if (heightBytes.length !== size * size) {
        throw new Error("Height map payload size does not match map dimensions.");
    }

    terrainTextureCanvas = createTerrainTexture(size, heightBytes);
    viewCenterX = size * 0.5;
    viewCenterY = size * 0.5;
    const decodeEnd = performance.now();
    const renderStart = performance.now();
    renderTerrain();
    const renderEnd = performance.now();
    updateTerrainMapStatus(size, heightBytes.length, decodeEnd - decodeStart, renderEnd - renderStart);
}

function connectSimulationStream() {
    const eventSource = new EventSource("/api/simulation/stream");

    streamStatusElement.textContent = JSON.stringify({
        status: "connecting",
        trackedActors: actors.size,
    }, null, 2);

    eventSource.addEventListener("tick", (event) => {
        const payload = JSON.parse(event.data);
        const sequence = Number.isInteger(payload.sequence) ? payload.sequence : null;

        if (sequence === null) {
            return;
        }

        if (lastTickSequence !== null && sequence <= lastTickSequence) {
            return;
        }

        if (Number.isFinite(payload.tickRateHz) && payload.tickRateHz > 0) {
            tickIntervalMs = 1000 / payload.tickRateHz;
        }

        if (Array.isArray(payload.locationChanges)) {
            for (const change of payload.locationChanges) {
                if (!Number.isInteger(change.entityId) || !Number.isFinite(change.x) || !Number.isFinite(change.y)) {
                    continue;
                }

                upsertActor(change.entityId, change.x, change.y);
            }
        }

        lastTickAppliedAtMs = performance.now();
        lastTickSequence = sequence;
        streamStatusElement.textContent = JSON.stringify({
            status: "connected",
            sequence,
            changes: Array.isArray(payload.locationChanges) ? payload.locationChanges.length : 0,
            trackedActors: actors.size,
        }, null, 2);
    });

    eventSource.onerror = () => {
        streamStatusElement.textContent = JSON.stringify({
            status: "disconnected",
            lastSequence: lastTickSequence,
            trackedActors: actors.size,
        }, null, 2);
    };
}

async function loadActorsSnapshot() {
    const response = await fetch("/api/simulation/actors");

    if (!response.ok) {
        throw new Error(`Failed to load actors (${response.status}).`);
    }

    const payload = await response.json();
    actors.clear();

    for (const actor of payload) {
        upsertActor(actor.entityId, actor.x, actor.y);
    }
}

function panByViewportRatio(xRatio, yRatio) {
    if (terrainTextureCanvas === null) {
        return;
    }

    const drawScale = getDrawScale();
    const viewportWorldWidth = canvas.width / drawScale;
    const viewportWorldHeight = canvas.height / drawScale;
    viewCenterX += viewportWorldWidth * xRatio;
    viewCenterY += viewportWorldHeight * yRatio;
}

canvas.addEventListener("wheel", (event) => {
    event.preventDefault();

    const point = eventToCanvasPoint(event);
    const zoomFactor = Math.exp(event.deltaY * -0.0015);
    zoomAtCanvasPoint(zoom * zoomFactor, point.x, point.y);
}, { passive: false });

canvas.addEventListener("mousedown", (event) => {
    if (event.button !== 0) {
        return;
    }

    isDragging = true;
    lastDragPoint = eventToCanvasPoint(event);
    canvas.classList.add("is-dragging");
});

canvas.addEventListener("mousemove", (event) => {
    if (!isDragging || lastDragPoint === null || terrainTextureCanvas === null) {
        return;
    }

    const point = eventToCanvasPoint(event);
    const dx = point.x - lastDragPoint.x;
    const dy = point.y - lastDragPoint.y;
    const drawScale = getDrawScale();

    viewCenterX -= dx / drawScale;
    viewCenterY -= dy / drawScale;
    lastDragPoint = point;
});

function endDrag() {
    isDragging = false;
    lastDragPoint = null;
    canvas.classList.remove("is-dragging");
}

canvas.addEventListener("mouseup", endDrag);
canvas.addEventListener("mouseleave", endDrag);
canvas.addEventListener("blur", endDrag);

document.getElementById("zoom-in").addEventListener("click", () => {
    zoomAtCanvasPoint(zoom * 1.1, canvas.width * 0.5, canvas.height * 0.5);
});

document.getElementById("zoom-out").addEventListener("click", () => {
    zoomAtCanvasPoint(zoom / 1.1, canvas.width * 0.5, canvas.height * 0.5);
});

document.getElementById("pan-up").addEventListener("click", () => panByViewportRatio(0, -0.1));
document.getElementById("pan-down").addEventListener("click", () => panByViewportRatio(0, 0.1));
document.getElementById("pan-left").addEventListener("click", () => panByViewportRatio(-0.1, 0));
document.getElementById("pan-right").addEventListener("click", () => panByViewportRatio(0.1, 0));

renderPlaceholder();
startRenderLoop();
Promise.all([loadTerrainSeed(), loadTerrainHeightMap()]).catch((error) => {
    terrainSeedElement.textContent = error instanceof Error ? error.message : String(error);
    if (terrainMapStatusElement !== null) {
        terrainMapStatusElement.textContent = error instanceof Error ? error.message : String(error);
    }
});

loadActorsSnapshot().catch((error) => {
    streamStatusElement.textContent = JSON.stringify({
        status: "waiting-for-simulation",
        message: error instanceof Error ? error.message : String(error),
        trackedActors: actors.size,
    }, null, 2);
});

connectSimulationStream();
