const form = document.getElementById("tuning-form");
const formStatus = document.getElementById("form-status");
const renderList = document.getElementById("render-list");
const generateButton = document.getElementById("generate");
const showWaterOverlayToggle = document.getElementById("show-water-overlay");
const sizePowerInput = document.getElementById("sizePower");
const sizePowerOutput = document.getElementById("sizePower-value");
const upscalePowerInput = document.getElementById("upscalePower");
const upscalePowerOutput = document.getElementById("upscalePower-value");
const smoothnessPowerInput = document.getElementById("smoothnessPower");
const smoothnessPowerOutput = document.getElementById("smoothnessPower-value");
const tabButtons = Array.from(document.querySelectorAll(".tab-button"));
const tabPanels = Array.from(document.querySelectorAll(".tab-panel"));

const renderEntries = [];

function sizeFromPower(power) {
    return 2 ** power;
}

function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
}

function powerFromSize(size) {
    if (size <= 1) {
        return Number(sizePowerInput.min);
    }

    const exactPower = Math.log2(size);
    const roundedPower = Math.round(exactPower);
    const minPower = Number(sizePowerInput.min);
    const maxPower = Number(sizePowerInput.max);
    return clamp(roundedPower, minPower, maxPower);
}

function powerFromUpscaleFactor(factor) {
    if (factor <= 1) {
        return Number(upscalePowerInput.min);
    }

    const exactPower = Math.log2(factor);
    const roundedPower = Math.round(exactPower);
    const minPower = Number(upscalePowerInput.min);
    const maxPower = Number(upscalePowerInput.max);
    return clamp(roundedPower, minPower, maxPower);
}

function powerFromSmoothnessStopStep(step) {
    if (step <= 1) {
        return Number(smoothnessPowerInput.min);
    }

    const exactPower = Math.log2(step);
    const roundedPower = Math.round(exactPower);
    const minPower = Number(smoothnessPowerInput.min);
    const maxPower = Number(smoothnessPowerInput.max);
    return clamp(roundedPower, minPower, maxPower);
}

const fieldIds = [
    "seed",
    "roughness",
    "initialDisplacement",
    "raindrops",
    "dropPathLength",
    "neighborSampleCount",
    "erosionStrength",
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

function createWaterTexture(size, waterAccumulationBytes, riverMaskBytes, lakeMaskBytes) {
    const textureCanvas = document.createElement("canvas");
    textureCanvas.width = size;
    textureCanvas.height = size;

    const textureContext = textureCanvas.getContext("2d");
    if (textureContext === null) {
        throw new Error("Unable to initialize water texture context.");
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
    const showWaterOverlay = showWaterOverlayToggle !== null ? showWaterOverlayToggle.checked : true;
    const waterTexture = showWaterOverlay
        ? createWaterTexture(size, waterBytes, riverBytes, lakeBytes)
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

    if (waterTexture !== null) {
        context.drawImage(waterTexture, drawX, drawY, drawWidth, drawHeight);
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

function syncSmoothnessPowerLimit() {
    const maxSmoothnessPower = Number(sizePowerInput.value);
    smoothnessPowerInput.max = String(maxSmoothnessPower);

    if (Number(smoothnessPowerInput.value) > maxSmoothnessPower) {
        smoothnessPowerInput.value = String(maxSmoothnessPower);
    }
}

function updateOutputValues() {
    for (const id of fieldIds) {
        const { input, output } = fieldElements[id];
        output.textContent = input.value;
    }

    const sizePower = Number(sizePowerInput.value);
    const selectedSize = sizeFromPower(sizePower);
    sizePowerOutput.textContent = `${selectedSize} (2^${sizePower})`;

    const upscalePower = Number(upscalePowerInput.value);
    const selectedUpscaleFactor = sizeFromPower(upscalePower);
    upscalePowerOutput.textContent = `${selectedUpscaleFactor} (2^${upscalePower})`;

    const smoothnessPower = Number(smoothnessPowerInput.value);
    const selectedSmoothnessStopStep = sizeFromPower(smoothnessPower);
    smoothnessPowerOutput.textContent = `${selectedSmoothnessStopStep} (2^${smoothnessPower})`;
}

function buildRequestBody() {
    return {
        seed: Number(fieldElements.seed.input.value),
        size: sizeFromPower(Number(sizePowerInput.value)),
        upscaleFactor: sizeFromPower(Number(upscalePowerInput.value)),
        diamondSquare: {
            smoothnessStopStep: sizeFromPower(Number(smoothnessPowerInput.value)),
            roughness: Number(fieldElements.roughness.input.value),
            initialDisplacement: Number(fieldElements.initialDisplacement.input.value),
        },
        erosion: {
            raindrops: Number(fieldElements.raindrops.input.value),
            dropPathLength: Number(fieldElements.dropPathLength.input.value),
            neighborSampleCount: Number(fieldElements.neighborSampleCount.input.value),
            erosionStrength: Number(fieldElements.erosionStrength.input.value),
            depositionRatio: Number(fieldElements.depositionRatio.input.value),
        },
    };
}

function buildConfigSnippet(configuration) {
    const lines = [
        "  \"Terrain\": {",
        `    \"Seed\": ${configuration.seed},`,
        `    \"Size\": ${configuration.size},`,
        `    \"UpscaleFactor\": ${configuration.upscaleFactor},`,
        "    \"DiamondSquare\": {",
        `      \"SmoothnessStopStep\": ${configuration.diamondSquare.smoothnessStopStep},`,
        `      \"Roughness\": ${Number(configuration.diamondSquare.roughness.toFixed(4))},`,
        `      \"InitialDisplacement\": ${Number(configuration.diamondSquare.initialDisplacement.toFixed(4))}`,
        "    },",
        "    \"Erosion\": {",
        `      \"Raindrops\": ${configuration.erosion.raindrops},`,
        `      \"DropPathLength\": ${configuration.erosion.dropPathLength},`,
        `      \"NeighborSampleCount\": ${configuration.erosion.neighborSampleCount},`,
        `      \"ErosionStrength\": ${Number(configuration.erosion.erosionStrength.toFixed(4))},`,
        `      \"DepositionRatio\": ${Number(configuration.erosion.depositionRatio.toFixed(4))}`,
        "    }",
        "  },",
    ];

    return lines.join("\n");
}

function activateTab(panelId) {
    for (const button of tabButtons) {
        const isSelected = button.dataset.panel === panelId;
        button.setAttribute("aria-selected", String(isSelected));
    }

    for (const panel of tabPanels) {
        const isSelected = panel.id === panelId;
        panel.hidden = !isSelected;
    }
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
        configuration: {
            seed: payload.configuration.seed,
            size: payload.configuration.size,
            upscaleFactor: payload.configuration.upscaleFactor,
            diamondSquare: {
                smoothnessStopStep: payload.configuration.diamondSquare.smoothnessStopStep,
                roughness: Number(payload.configuration.diamondSquare.roughness.toFixed(3)),
                initialDisplacement: Number(payload.configuration.diamondSquare.initialDisplacement.toFixed(3)),
            },
            erosion: {
                raindrops: payload.configuration.erosion.raindrops,
                dropPathLength: payload.configuration.erosion.dropPathLength,
                neighborSampleCount: payload.configuration.erosion.neighborSampleCount,
                erosionStrength: Number(payload.configuration.erosion.erosionStrength.toFixed(3)),
                depositionRatio: Number(payload.configuration.erosion.depositionRatio.toFixed(3)),
            },
        },
        renderedSize: payload.renderedSize,
        elapsedMs: Number(elapsedMs.toFixed(2)),
    }, null, 2);

    copyButton.addEventListener("click", async () => {
        const defaultLabel = "Copy to config";
        const snippet = buildConfigSnippet(payload.configuration);
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

    drawCompositeMap(canvas, payload.renderedSize, heightBytes, waterBytes, riverBytes, lakeBytes);

    renderEntries.push({
        canvas,
        size: payload.renderedSize,
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
    upscalePowerInput.value = powerFromUpscaleFactor(defaults.upscaleFactor);
    smoothnessPowerInput.value = powerFromSmoothnessStopStep(defaults.diamondSquare.smoothnessStopStep);
    fieldElements.roughness.input.value = defaults.diamondSquare.roughness;
    fieldElements.initialDisplacement.input.value = defaults.diamondSquare.initialDisplacement;
    fieldElements.raindrops.input.value = defaults.erosion.raindrops;
    fieldElements.dropPathLength.input.value = defaults.erosion.dropPathLength;
    fieldElements.neighborSampleCount.input.value = defaults.erosion.neighborSampleCount;
    fieldElements.erosionStrength.input.value = defaults.erosion.erosionStrength;
    fieldElements.depositionRatio.input.value = defaults.erosion.depositionRatio;
    syncSmoothnessPowerLimit();
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

const missingElements = [];

if (form === null) {
    missingElements.push("tuning-form");
}

if (formStatus === null) {
    missingElements.push("form-status");
}

if (renderList === null) {
    missingElements.push("render-list");
}

if (generateButton === null) {
    missingElements.push("generate");
}

if (sizePowerInput === null) {
    missingElements.push("sizePower");
}

if (sizePowerOutput === null) {
    missingElements.push("sizePower-value");
}

if (upscalePowerInput === null) {
    missingElements.push("upscalePower");
}

if (upscalePowerOutput === null) {
    missingElements.push("upscalePower-value");
}

if (smoothnessPowerInput === null) {
    missingElements.push("smoothnessPower");
}

if (smoothnessPowerOutput === null) {
    missingElements.push("smoothnessPower-value");
}

if (tabButtons.length === 0) {
    missingElements.push("tab-buttons");
}

if (tabPanels.length === 0) {
    missingElements.push("tab-panels");
}

for (const id of fieldIds) {
    const { input, output } = fieldElements[id];

    if (input === null) {
        missingElements.push(id);
    }

    if (output === null) {
        missingElements.push(`${id}-value`);
    }
}

if (missingElements.length > 0) {
    console.warn(`Terrain lab controls not initialized. Missing elements: ${missingElements.join(", ")}`);
} else {
    for (const id of fieldIds) {
        const { input } = fieldElements[id];
        input.addEventListener("input", updateOutputValues);
    }

    sizePowerInput.addEventListener("input", () => {
        syncSmoothnessPowerLimit();
        updateOutputValues();
    });
    upscalePowerInput.addEventListener("input", updateOutputValues);
    smoothnessPowerInput.addEventListener("input", updateOutputValues);

    for (const button of tabButtons) {
        button.addEventListener("click", () => {
            activateTab(button.dataset.panel);
        });
    }

    if (showWaterOverlayToggle !== null) {
        showWaterOverlayToggle.addEventListener("change", () => {
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
        activateTab("general-panel");
        formStatus.textContent = "Defaults loaded. Adjust sliders and generate.";
    }).catch((error) => {
        formStatus.textContent = error instanceof Error ? error.message : String(error);
    });
}
