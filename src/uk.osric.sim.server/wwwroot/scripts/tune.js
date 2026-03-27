const form = document.getElementById("tuning-form");
const formStatus = document.getElementById("form-status");
const renderList = document.getElementById("render-list");
const generateButton = document.getElementById("generate");
const showHydraulicsToggle = document.getElementById("show-hydraulics");
const resizeToggle = document.getElementById("enable-resize");
const sizeControl = document.getElementById("size-control");
const sizePowerInput = document.getElementById("sizePower");
const sizePowerOutput = document.getElementById("sizePower-value");

const renderEntries = [];

function sizeFromPower(power) {
    return (2 ** power) + 1;
}

function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
}

function powerFromSize(size) {
    if (size <= 1) {
        return Number(sizePowerInput.min);
    }

    const exactPower = Math.log2(size - 1);
    const roundedPower = Math.round(exactPower);
    const minPower = Number(sizePowerInput.min);
    const maxPower = Number(sizePowerInput.max);
    return clamp(roundedPower, minPower, maxPower);
}

const fieldIds = [
    "seed",
    "erosionPasses",
    "topologyRefreshInterval",
    "neighborSampleCount",
    "baseFlow",
    "erosionCapFactor",
    "slopeFlowFactor",
    "depositionRatio",
];

const fieldElements = fieldIds.reduce((lookup, id) => {
    const input = document.getElementById(id);
    const output = document.getElementById(`${id}-value`);
    lookup[id] = { input, output };
    return lookup;
}, {});

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
        throw new Error("Unable to initialize terrain texture context.");
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
        throw new Error("Unable to initialize hydraulics texture context.");
    }

    const imageData = textureContext.createImageData(size, size);
    const rgba = imageData.data;

    for (let i = 0; i < waterAccumulationBytes.length; i += 1) {
        const flow = waterAccumulationBytes[i];
        const isRiver = riverMaskBytes[i] > 0;
        const isLake = lakeMaskBytes[i] > 0;
        const pixelOffset = i * 4;

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

function drawCompositeMap(canvas, size, heightBytes, waterBytes, riverBytes, lakeBytes) {
    const terrainTexture = createTerrainTexture(size, heightBytes);
    const showHydraulics = showHydraulicsToggle !== null ? showHydraulicsToggle.checked : true;
    const hydraulicsTexture = showHydraulics
        ? createHydraulicsTexture(size, waterBytes, riverBytes, lakeBytes)
        : null;

    const context = canvas.getContext("2d");
    if (context === null) {
        throw new Error("Unable to initialize output canvas context.");
    }

    canvas.width = 960;
    canvas.height = 540;

    context.clearRect(0, 0, canvas.width, canvas.height);
    context.imageSmoothingEnabled = false;

    const fitScale = Math.min(canvas.width / size, canvas.height / size);
    const drawWidth = size * fitScale;
    const drawHeight = size * fitScale;
    const drawX = (canvas.width - drawWidth) * 0.5;
    const drawY = (canvas.height - drawHeight) * 0.5;

    context.drawImage(terrainTexture, drawX, drawY, drawWidth, drawHeight);

    if (hydraulicsTexture !== null) {
        context.drawImage(hydraulicsTexture, drawX, drawY, drawWidth, drawHeight);
    }
}

function redrawAllCards() {
    for (const entry of renderEntries) {
        drawCompositeMap(
            entry.canvas,
            entry.size,
            entry.heightBytes,
            entry.waterBytes,
            entry.riverBytes,
            entry.lakeBytes);
    }
}

function updateOutputValues() {
    for (const id of fieldIds) {
        const { input, output } = fieldElements[id];
        output.textContent = input.value;
    }

    const power = Number(sizePowerInput.value);
    const selectedSize = sizeFromPower(power);
    const resizeEnabled = resizeToggle !== null && resizeToggle.checked;
    sizePowerOutput.textContent = resizeEnabled
        ? `${selectedSize} (2^${power} + 1)`
        : `${selectedSize} (locked unless resizing is enabled)`;
}

function syncResizeControlState() {
    const enabled = resizeToggle !== null && resizeToggle.checked;
    sizePowerInput.disabled = !enabled;

    if (sizeControl !== null) {
        sizeControl.classList.toggle("is-disabled", !enabled);
    }

    updateOutputValues();
}

function buildRequestBody() {
    return {
        seed: Number(fieldElements.seed.input.value),
        sourceSize: sizeFromPower(Number(sizePowerInput.value)),
        resizeEnabled: resizeToggle !== null && resizeToggle.checked,
        erosionPasses: Number(fieldElements.erosionPasses.input.value),
        topologyRefreshInterval: Number(fieldElements.topologyRefreshInterval.input.value),
        neighborSampleCount: Number(fieldElements.neighborSampleCount.input.value),
        baseFlow: Number(fieldElements.baseFlow.input.value),
        erosionCapFactor: Number(fieldElements.erosionCapFactor.input.value),
        slopeFlowFactor: Number(fieldElements.slopeFlowFactor.input.value),
        depositionRatio: Number(fieldElements.depositionRatio.input.value),
    };
}

function buildConfigSnippet(payload) {
    return JSON.stringify({
        Terrain: {
            DefaultSeed: payload.seed,
            DefaultSize: payload.sourceSize,
            UpscaleFactor: payload.upscaleFactor,
            ErosionPasses: payload.erosionPasses,
            HydraulicErosion: {
                TopologyRefreshInterval: payload.topologyRefreshInterval,
                NeighborSampleCount: payload.neighborSampleCount,
                BaseFlow: Number(payload.baseFlow.toFixed(4)),
                ErosionCapFactor: Number(payload.erosionCapFactor.toFixed(4)),
                SlopeFlowFactor: Number(payload.slopeFlowFactor.toFixed(4)),
                DepositionRatio: Number(payload.depositionRatio.toFixed(4)),
            },
        },
    }, null, 2);
}

async function writeClipboardText(text) {
    if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text);
        return;
    }

    const helperTextArea = document.createElement("textarea");
    helperTextArea.value = text;
    helperTextArea.setAttribute("readonly", "readonly");
    helperTextArea.style.position = "fixed";
    helperTextArea.style.opacity = "0";
    document.body.appendChild(helperTextArea);
    helperTextArea.select();
    document.execCommand("copy");
    document.body.removeChild(helperTextArea);
}

function addRenderCard(payload, elapsedMs) {
    const card = document.createElement("article");
    card.className = "render-card";

    const canvas = document.createElement("canvas");
    const meta = document.createElement("aside");
    meta.className = "render-meta";

    const title = document.createElement("h2");
    title.textContent = `Render ${new Date().toLocaleTimeString()}`;

    const copyButton = document.createElement("button");
    copyButton.type = "button";
    copyButton.className = "copy-config";
    copyButton.textContent = "Copy to config";

    const details = document.createElement("pre");
    details.textContent = JSON.stringify({
        seed: payload.seed,
        sourceSize: payload.sourceSize,
        renderedSize: payload.size,
        upscaleFactor: payload.upscaleFactor,
        resizeEnabled: payload.resizeEnabled,
        erosionPasses: payload.erosionPasses,
        topologyRefreshInterval: payload.topologyRefreshInterval,
        neighborSampleCount: payload.neighborSampleCount,
        baseFlow: Number(payload.baseFlow.toFixed(3)),
        erosionCapFactor: Number(payload.erosionCapFactor.toFixed(3)),
        slopeFlowFactor: Number(payload.slopeFlowFactor.toFixed(3)),
        depositionRatio: Number(payload.depositionRatio.toFixed(3)),
        elapsedMs: Number(elapsedMs.toFixed(2)),
    }, null, 2);

    copyButton.addEventListener("click", async () => {
        const defaultLabel = "Copy to config";
        const snippet = buildConfigSnippet(payload);
        copyButton.disabled = true;

        try {
            await writeClipboardText(snippet);
            copyButton.textContent = "Copied";
        } catch {
            copyButton.textContent = "Copy failed";
        }

        window.setTimeout(() => {
            copyButton.disabled = false;
            copyButton.textContent = defaultLabel;
        }, 1200);
    });

    meta.appendChild(title);
    meta.appendChild(copyButton);
    meta.appendChild(details);
    card.appendChild(canvas);
    card.appendChild(meta);

    const heightBytes = decodeBase64(payload.heightDataBase64);
    const waterBytes = decodeBase64(payload.waterAccumulationDataBase64);
    const riverBytes = decodeBase64(payload.riverMaskDataBase64);
    const lakeBytes = decodeBase64(payload.lakeMaskDataBase64);

    drawCompositeMap(canvas, payload.size, heightBytes, waterBytes, riverBytes, lakeBytes);

    renderEntries.push({
        canvas,
        size: payload.size,
        heightBytes,
        waterBytes,
        riverBytes,
        lakeBytes,
    });

    renderList.prepend(card);
}

async function loadDefaults() {
    const response = await fetch("/api/terrain/tuning-defaults");
    if (!response.ok) {
        throw new Error(`Failed to load tuning defaults (${response.status}).`);
    }

    const defaults = await response.json();
    fieldElements.seed.input.value = defaults.seed;
    sizePowerInput.value = powerFromSize(defaults.size);
    fieldElements.erosionPasses.input.value = defaults.erosionPasses;
    fieldElements.topologyRefreshInterval.input.value = defaults.topologyRefreshInterval;
    fieldElements.neighborSampleCount.input.value = defaults.neighborSampleCount;
    fieldElements.baseFlow.input.value = defaults.baseFlow;
    fieldElements.erosionCapFactor.input.value = defaults.erosionCapFactor;
    fieldElements.slopeFlowFactor.input.value = defaults.slopeFlowFactor;
    fieldElements.depositionRatio.input.value = defaults.depositionRatio;
    updateOutputValues();
}

async function submitRender() {
    const requestBody = buildRequestBody();
    const startedAt = performance.now();
    const response = await fetch("/api/terrain/render", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(requestBody),
    });

    if (!response.ok) {
        const body = await response.text();
        throw new Error(`Render request failed (${response.status}): ${body}`);
    }

    const payload = await response.json();
    const endedAt = performance.now();
    addRenderCard(payload, endedAt - startedAt);
}

for (const id of fieldIds) {
    const { input } = fieldElements[id];
    input.addEventListener("input", updateOutputValues);
}

sizePowerInput.addEventListener("input", updateOutputValues);

if (resizeToggle !== null) {
    resizeToggle.addEventListener("change", () => {
        syncResizeControlState();
    });
}

if (showHydraulicsToggle !== null) {
    showHydraulicsToggle.addEventListener("change", () => {
        redrawAllCards();
    });
}

form.addEventListener("submit", async (event) => {
    event.preventDefault();
    generateButton.disabled = true;
    formStatus.textContent = "Generating terrain...";

    try {
        await submitRender();
        formStatus.textContent = "Render complete. New map inserted at the top.";
    } catch (error) {
        formStatus.textContent = error instanceof Error ? error.message : String(error);
    } finally {
        generateButton.disabled = false;
    }
});

loadDefaults().then(() => {
    syncResizeControlState();
    formStatus.textContent = "Defaults loaded. Adjust sliders and generate.";
}).catch((error) => {
    formStatus.textContent = error instanceof Error ? error.message : String(error);
});
