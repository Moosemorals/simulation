const terrainSeedElement = document.getElementById("terrain-seed");
const terrainMapStatusElement = document.getElementById("terrain-map-status");
const streamStatusElement = document.getElementById("stream-status");
const canvas = document.getElementById("sim-canvas");
const context = canvas.getContext("2d");

let zoom = 1;
let terrainTextureCanvas = null;
let hydraulicsTextureCanvas = null;

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

function createHydraulicsTexture(size, waterAccumulationBytes, riverMaskBytes, lakeMaskBytes) {
    const textureCanvas = document.createElement("canvas");
    textureCanvas.width = size;
    textureCanvas.height = size;

    const textureContext = textureCanvas.getContext("2d");
    if (textureContext === null) {
        throw new Error("Unable to initialize hydraulics texture canvas context.");
    }

    const imageData = textureContext.createImageData(size, size);
    const rgba = imageData.data;

    for (let i = 0; i < waterAccumulationBytes.length; i += 1) {
        const flow = waterAccumulationBytes[i];
        const isRiver = riverMaskBytes[i] > 0;
        const isLake = lakeMaskBytes[i] > 0;

        const pixelOffset = i * 4;

        // Render flow as subtle cyan haze, then force strong colors for river/lake masks.
        let red = 10;
        let green = Math.min(180, Math.round(flow * 0.6));
        let blue = Math.min(220, Math.round(flow * 0.9));
        let alpha = Math.min(180, Math.round(flow * 0.55));

        if (isRiver) {
            red = 16;
            green = 196;
            blue = 255;
            alpha = 235;
        }

        if (isLake) {
            red = 8;
            green = 112;
            blue = 255;
            alpha = 215;
        }

        rgba[pixelOffset] = red;
        rgba[pixelOffset + 1] = green;
        rgba[pixelOffset + 2] = blue;
        rgba[pixelOffset + 3] = alpha;
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

    context.clearRect(0, 0, canvas.width, canvas.height);
    context.imageSmoothingEnabled = false;

    const terrainSize = terrainTextureCanvas.width;
    const fitScale = Math.min(canvas.width / terrainSize, canvas.height / terrainSize);
    const drawScale = fitScale * zoom;
    const drawWidth = terrainSize * drawScale;
    const drawHeight = terrainSize * drawScale;
    const drawX = (canvas.width - drawWidth) * 0.5;
    const drawY = (canvas.height - drawHeight) * 0.5;

    context.drawImage(terrainTextureCanvas, drawX, drawY, drawWidth, drawHeight);

    if (hydraulicsTextureCanvas !== null) {
        context.drawImage(hydraulicsTextureCanvas, drawX, drawY, drawWidth, drawHeight);
    }
}

function countTruthyBytes(bytes) {
    let count = 0;
    for (let i = 0; i < bytes.length; i += 1) {
        if (bytes[i] > 0) {
            count += 1;
        }
    }

    return count;
}

function updateTerrainMapStatus(size, byteCount, riverCount, lakeCount, decodeDurationMs, renderDurationMs) {
    if (terrainMapStatusElement === null) {
        return;
    }

    terrainMapStatusElement.textContent = JSON.stringify({
        size,
        tileCount: size * size,
        byteCount,
        riverTiles: riverCount,
        lakeTiles: lakeCount,
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
    const size = payload.size;
    const heightBytes = decodeBase64(payload.heightDataBase64);

    if (heightBytes.length !== size * size) {
        throw new Error("Height map payload size does not match map dimensions.");
    }

    terrainTextureCanvas = createTerrainTexture(size, heightBytes);
    return { size, byteCount: heightBytes.length };
}

async function loadTerrainHydraulicsMap() {
    const response = await fetch("/api/terrain/hydraulics");

    if (!response.ok) {
        throw new Error(`Failed to load terrain hydraulics map (${response.status}).`);
    }

    const payload = await response.json();
    const size = payload.size;

    const waterAccumulationBytes = decodeBase64(payload.waterAccumulationDataBase64);
    const riverMaskBytes = decodeBase64(payload.riverMaskDataBase64);
    const lakeMaskBytes = decodeBase64(payload.lakeMaskDataBase64);

    const expectedLength = size * size;
    if (waterAccumulationBytes.length !== expectedLength || riverMaskBytes.length !== expectedLength || lakeMaskBytes.length !== expectedLength) {
        throw new Error("Hydraulics payload size does not match map dimensions.");
    }

    hydraulicsTextureCanvas = createHydraulicsTexture(size, waterAccumulationBytes, riverMaskBytes, lakeMaskBytes);

    return {
        size,
        riverCount: countTruthyBytes(riverMaskBytes),
        lakeCount: countTruthyBytes(lakeMaskBytes),
    };
}

function connectSimulationStream() {
    const eventSource = new EventSource("/api/simulation/stream");

    eventSource.addEventListener("tick", (event) => {
        streamStatusElement.textContent = event.data;
    });

    eventSource.onerror = () => {
        streamStatusElement.textContent = "Simulation stream disconnected.";
    };
}

document.getElementById("zoom-in").addEventListener("click", () => {
    zoom = Math.min(zoom + 0.1, 2);
    renderTerrain();
});

document.getElementById("zoom-out").addEventListener("click", () => {
    zoom = Math.max(zoom - 0.1, 0.5);
    renderTerrain();
});

renderPlaceholder();
const decodeStart = performance.now();
Promise.all([loadTerrainSeed(), loadTerrainHeightMap(), loadTerrainHydraulicsMap()]).then((results) => {
    const height = results[1];
    const hydraulics = results[2];

    if (height.size !== hydraulics.size) {
        throw new Error("Height and hydraulics map sizes do not match.");
    }

    const decodeEnd = performance.now();
    const renderStart = performance.now();
    renderTerrain();
    const renderEnd = performance.now();

    updateTerrainMapStatus(
        height.size,
        height.byteCount,
        hydraulics.riverCount,
        hydraulics.lakeCount,
        decodeEnd - decodeStart,
        renderEnd - renderStart);
}).catch((error) => {
    terrainSeedElement.textContent = error instanceof Error ? error.message : String(error);
    if (terrainMapStatusElement !== null) {
        terrainMapStatusElement.textContent = error instanceof Error ? error.message : String(error);
    }
});
connectSimulationStream();
