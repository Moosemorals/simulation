const terrainSeedElement = document.getElementById("terrain-seed");
const terrainMapStatusElement = document.getElementById("terrain-map-status");
const streamStatusElement = document.getElementById("stream-status");
const canvas = document.getElementById("sim-canvas");
const gl = canvas.getContext("webgl2", { antialias: true });

if (gl === null) {
    throw new Error("WebGL2 is required but was not available in this browser.");
}

const minZoom = 0.35;
const maxZoom = 3.5;
const terrainRenderScale = 16;
let zoom = 1;
let zoomTarget = 1;
let terrainSize = 0;
let terrainHeightBytes = null;
let terrainElevationScale = 42;
let viewCenterX = 0;
let viewCenterY = 0;
let zoomAnchorCanvasX = 0;
let zoomAnchorCanvasY = 0;
let zoomAnchorWorldX = 0;
let zoomAnchorWorldY = 0;
let zoomAnimationActive = false;
let isDragging = false;
let lastDragPoint = null;
const actors = new Map();
let lastTickSequence = null;
let lastTickAppliedAtMs = 0;
let tickIntervalMs = 100;
let animationFrameRequestId = null;

let panAnimationStartTimeMs = null;
let panAnimationDurationMs = 250;
let panAnimationStartX = 0;
let panAnimationStartY = 0;
let panAnimationTargetDeltaX = 0;
let panAnimationTargetDeltaY = 0;

const cameraUp = [0, 0, -1];
const cameraLookHeight = terrainElevationScale * 0.35;

const projectionMatrix = mat4Identity();
const viewMatrix = mat4Identity();
const viewProjectionMatrix = mat4Identity();
const terrainModelMatrix = mat4Identity();
const modelViewProjectionMatrix = mat4Identity();
const normalMatrix = mat3Identity();

let terrainProgram = null;
let terrainVertexArray = null;
let terrainIndexCount = 0;

let actorProgram = null;
let actorVertexArray = null;
let actorBuffer = null;
let actorPointCount = 0;

function clamp(value, minValue, maxValue) {
    return Math.min(maxValue, Math.max(minValue, value));
}

function easeInOutCubic(t) {
    return t < 0.5
        ? 4 * t * t * t
        : 1 - Math.pow(-2 * t + 2, 3) / 2;
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

function normalizeViewCenter() {
    if (terrainSize > 0) {
        const terrainWorldSize = terrainSize * terrainRenderScale;
        viewCenterX = wrapCoordinate(viewCenterX, terrainWorldSize);
        viewCenterY = wrapCoordinate(viewCenterY, terrainWorldSize);
    }
}

function eventToCanvasPoint(event) {
    const rect = canvas.getBoundingClientRect();
    return {
        x: (event.clientX - rect.left) * (canvas.width / rect.width),
        y: (event.clientY - rect.top) * (canvas.height / rect.height),
    };
}

function clampZoom(nextZoom) {
    return Math.min(maxZoom, Math.max(minZoom, nextZoom));
}

function getCameraDistance() {
    if (terrainSize <= 0) {
        return 640;
    }

    const baseDistance = terrainSize * 1.15;
    return baseDistance / zoom;
}

function getOrthographicHalfHeight() {
    if (terrainSize <= 0) {
        return 320 / zoom;
    }

    return (terrainSize * 0.62) / zoom;
}

function updatePanAnimation(nowMs) {
    if (panAnimationStartTimeMs === null) {
        return;
    }

    const elapsedMs = nowMs - panAnimationStartTimeMs;
    const rawProgress = elapsedMs / panAnimationDurationMs;

    if (rawProgress >= 1.0) {
        viewCenterX = panAnimationStartX + panAnimationTargetDeltaX;
        viewCenterY = panAnimationStartY + panAnimationTargetDeltaY;
        normalizeViewCenter();
        panAnimationStartTimeMs = null;
        return;
    }

    const easeProgress = easeInOutCubic(rawProgress);
    viewCenterX = panAnimationStartX + (panAnimationTargetDeltaX * easeProgress);
    viewCenterY = panAnimationStartY + (panAnimationTargetDeltaY * easeProgress);
    normalizeViewCenter();
}

function updateInterpolatedActors(nowMs) {
    const hasTickAnchor = lastTickAppliedAtMs > 0;
    const rawAlpha = hasTickAnchor ? ((nowMs - lastTickAppliedAtMs) / tickIntervalMs) : 1;
    const alpha = clamp(rawAlpha, 0, 1);

    for (const actor of actors.values()) {
        actor.drawX = interpolateWrapped(actor.previousX, actor.currentX, alpha, terrainSize);
        actor.drawY = interpolateWrapped(actor.previousY, actor.currentY, alpha, terrainSize);
    }
}

function updateZoomAnimation() {
    if (!zoomAnimationActive || terrainSize <= 0) {
        return;
    }

    const delta = zoomTarget - zoom;
    if (Math.abs(delta) < 0.00015) {
        zoom = zoomTarget;
        zoomAnimationActive = false;
        return;
    }

    zoom += delta * 0.22;
    updateCameraMatrices();
    const anchoredWorld = screenToGround(zoomAnchorCanvasX, zoomAnchorCanvasY);
    viewCenterX += zoomAnchorWorldX - anchoredWorld.x;
    viewCenterY += zoomAnchorWorldY - anchoredWorld.y;
    normalizeViewCenter();
}

function decodeBase64(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);

    for (let i = 0; i < binary.length; i += 1) {
        bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
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

function compileShader(kind, source) {
    const shader = gl.createShader(kind);

    if (shader === null) {
        throw new Error("Failed to allocate shader object.");
    }

    gl.shaderSource(shader, source);
    gl.compileShader(shader);

    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        const infoLog = gl.getShaderInfoLog(shader);
        gl.deleteShader(shader);
        throw new Error(`Shader compilation failed: ${infoLog ?? "unknown error"}`);
    }

    return shader;
}

function createProgram(vertexSource, fragmentSource) {
    const vertexShader = compileShader(gl.VERTEX_SHADER, vertexSource);
    const fragmentShader = compileShader(gl.FRAGMENT_SHADER, fragmentSource);
    const program = gl.createProgram();

    if (program === null) {
        gl.deleteShader(vertexShader);
        gl.deleteShader(fragmentShader);
        throw new Error("Failed to allocate shader program.");
    }

    gl.attachShader(program, vertexShader);
    gl.attachShader(program, fragmentShader);
    gl.linkProgram(program);
    gl.deleteShader(vertexShader);
    gl.deleteShader(fragmentShader);

    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        const infoLog = gl.getProgramInfoLog(program);
        gl.deleteProgram(program);
        throw new Error(`Program linking failed: ${infoLog ?? "unknown error"}`);
    }

    return program;
}

function createTerrainProgram() {
    const vertexSource = `#version 300 es
precision highp float;

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uViewProjection;
uniform mat3 uNormalMatrix;

out vec3 vWorldPosition;
out vec3 vNormal;

void main() {
    vec4 worldPosition = uModel * vec4(aPosition, 1.0);
    vWorldPosition = worldPosition.xyz;
    vNormal = normalize(uNormalMatrix * aNormal);
    gl_Position = uViewProjection * worldPosition;
}
`;

    const fragmentSource = `#version 300 es
precision highp float;

in vec3 vWorldPosition;
in vec3 vNormal;

uniform vec3 uLightDirection;
uniform float uElevationScale;

out vec4 outColor;

vec3 terrainRamp(float h) {
    vec3 low = vec3(0.18, 0.30, 0.23);
    vec3 mid = vec3(0.42, 0.53, 0.37);
    vec3 high = vec3(0.67, 0.64, 0.54);
    vec3 peak = vec3(0.85, 0.84, 0.80);

    float t = clamp(h, 0.0, 1.0);
    if (t < 0.45) {
        return mix(low, mid, t / 0.45);
    }

    if (t < 0.78) {
        return mix(mid, high, (t - 0.45) / 0.33);
    }

    return mix(high, peak, (t - 0.78) / 0.22);
}

void main() {
    float height01 = clamp(vWorldPosition.y / uElevationScale, 0.0, 1.0);
    vec3 albedo = terrainRamp(height01);
    vec3 normal = normalize(vNormal);
    float diffuse = max(dot(normal, -normalize(uLightDirection)), 0.0);
    float ambient = 0.35;
    float lighting = ambient + (0.65 * diffuse);
    outColor = vec4(albedo * lighting, 1.0);
}
`;

    return {
        program: createProgram(vertexSource, fragmentSource),
        locations: {
            model: "uModel",
            viewProjection: "uViewProjection",
            normalMatrix: "uNormalMatrix",
            lightDirection: "uLightDirection",
            elevationScale: "uElevationScale",
        },
    };
}

function createActorProgram() {
    const vertexSource = `#version 300 es
precision highp float;

layout(location = 0) in vec3 aPosition;

uniform mat4 uModel;
uniform mat4 uViewProjection;
uniform float uPointSize;

void main() {
    vec4 worldPosition = uModel * vec4(aPosition, 1.0);
    gl_Position = uViewProjection * worldPosition;
    gl_PointSize = uPointSize;
}
`;

    const fragmentSource = `#version 300 es
precision highp float;

out vec4 outColor;

void main() {
    vec2 uv = (gl_PointCoord * 2.0) - 1.0;
    float radiusSq = dot(uv, uv);
    if (radiusSq > 1.0) {
        discard;
    }

    float edge = smoothstep(0.78, 1.0, radiusSq);
    vec3 fill = vec3(0.96, 0.94, 0.90);
    vec3 border = vec3(0.12, 0.10, 0.08);
    outColor = vec4(mix(fill, border, edge), 1.0);
}
`;

    return {
        program: createProgram(vertexSource, fragmentSource),
        locations: {
            model: "uModel",
            viewProjection: "uViewProjection",
            pointSize: "uPointSize",
        },
    };
}

function getElevationAt(worldX, worldY) {
    if (terrainHeightBytes === null || terrainSize <= 0) {
        return 0;
    }

    const x = wrapCoordinate(Math.round(worldX), terrainSize);
    const y = wrapCoordinate(Math.round(worldY), terrainSize);
    const sample = terrainHeightBytes[(y * terrainSize) + x] ?? 0;
    return (sample / 255) * terrainElevationScale;
}

function getNormalAt(heightBytes, size, x, y) {
    const left = heightBytes[(y * size) + wrapCoordinate(x - 1, size)] / 255;
    const right = heightBytes[(y * size) + wrapCoordinate(x + 1, size)] / 255;
    const up = heightBytes[(wrapCoordinate(y - 1, size) * size) + x] / 255;
    const down = heightBytes[(wrapCoordinate(y + 1, size) * size) + x] / 255;

    const dx = ((right - left) * terrainElevationScale) / terrainRenderScale;
    const dz = ((down - up) * terrainElevationScale) / terrainRenderScale;
    const nx = -dx;
    const ny = 2;
    const nz = -dz;
    const invLength = 1 / Math.hypot(nx, ny, nz);

    return [nx * invLength, ny * invLength, nz * invLength];
}

function uploadTerrainMesh(size, heightBytes) {
    if (terrainVertexArray !== null) {
        gl.deleteVertexArray(terrainVertexArray);
        terrainVertexArray = null;
    }

    const vertexCount = size * size;
    const stride = 6;
    const vertices = new Float32Array(vertexCount * stride);

    for (let y = 0; y < size; y += 1) {
        for (let x = 0; x < size; x += 1) {
            const index = (y * size) + x;
            const offset = index * stride;
            const height = (heightBytes[index] / 255) * terrainElevationScale;
            const normal = getNormalAt(heightBytes, size, x, y);

            vertices[offset] = x * terrainRenderScale;
            vertices[offset + 1] = height;
            vertices[offset + 2] = y * terrainRenderScale;
            vertices[offset + 3] = normal[0];
            vertices[offset + 4] = normal[1];
            vertices[offset + 5] = normal[2];
        }
    }

    const quadCount = (size - 1) * (size - 1);
    const indexCount = quadCount * 6;
    const indices = new Uint32Array(indexCount);
    let write = 0;

    for (let y = 0; y < size - 1; y += 1) {
        for (let x = 0; x < size - 1; x += 1) {
            const i0 = (y * size) + x;
            const i1 = i0 + 1;
            const i2 = i0 + size;
            const i3 = i2 + 1;

            indices[write] = i0;
            indices[write + 1] = i2;
            indices[write + 2] = i1;
            indices[write + 3] = i1;
            indices[write + 4] = i2;
            indices[write + 5] = i3;
            write += 6;
        }
    }

    terrainVertexArray = gl.createVertexArray();

    if (terrainVertexArray === null) {
        throw new Error("Unable to allocate terrain vertex array.");
    }

    const vertexBuffer = gl.createBuffer();
    const indexBuffer = gl.createBuffer();

    if (vertexBuffer === null || indexBuffer === null) {
        throw new Error("Unable to allocate terrain buffers.");
    }

    gl.bindVertexArray(terrainVertexArray);
    gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

    const byteStride = stride * Float32Array.BYTES_PER_ELEMENT;
    gl.enableVertexAttribArray(0);
    gl.vertexAttribPointer(0, 3, gl.FLOAT, false, byteStride, 0);
    gl.enableVertexAttribArray(1);
    gl.vertexAttribPointer(1, 3, gl.FLOAT, false, byteStride, 3 * Float32Array.BYTES_PER_ELEMENT);

    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBuffer);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, indices, gl.STATIC_DRAW);
    gl.bindVertexArray(null);

    terrainIndexCount = indexCount;
}

function ensureActorBuffers() {
    if (actorVertexArray !== null && actorBuffer !== null) {
        return;
    }

    actorVertexArray = gl.createVertexArray();
    actorBuffer = gl.createBuffer();

    if (actorVertexArray === null || actorBuffer === null) {
        throw new Error("Unable to allocate actor rendering buffers.");
    }

    gl.bindVertexArray(actorVertexArray);
    gl.bindBuffer(gl.ARRAY_BUFFER, actorBuffer);
    gl.enableVertexAttribArray(0);
    gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 3 * Float32Array.BYTES_PER_ELEMENT, 0);
    gl.bindVertexArray(null);
}

function updateActorBuffer() {
    ensureActorBuffers();

    const data = new Float32Array(actors.size * 3);
    let write = 0;

    for (const actor of actors.values()) {
        data[write] = actor.drawX * terrainRenderScale;
        data[write + 1] = getElevationAt(actor.drawX, actor.drawY) + 2;
        data[write + 2] = actor.drawY * terrainRenderScale;
        write += 3;
    }

    gl.bindBuffer(gl.ARRAY_BUFFER, actorBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, data, gl.DYNAMIC_DRAW);
    actorPointCount = actors.size;
}

function initializePrograms() {
    if (terrainProgram !== null && actorProgram !== null) {
        return;
    }

    terrainProgram = createTerrainProgram();
    actorProgram = createActorProgram();

    gl.enable(gl.DEPTH_TEST);
    gl.enable(gl.CULL_FACE);
    gl.cullFace(gl.BACK);
    gl.clearColor(0.18, 0.23, 0.20, 1);
}

function resizeCanvasToDisplaySize() {
    const devicePixelRatio = Math.max(1, window.devicePixelRatio || 1);
    const width = Math.max(1, Math.floor(canvas.clientWidth * devicePixelRatio));
    const height = Math.max(1, Math.floor(canvas.clientHeight * devicePixelRatio));

    if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width;
        canvas.height = height;
    }

    gl.viewport(0, 0, canvas.width, canvas.height);
}

function updateCameraMatrices() {
    const aspect = canvas.width / Math.max(1, canvas.height);
    const halfHeight = getOrthographicHalfHeight();
    const halfWidth = halfHeight * aspect;
    mat4Ortho(projectionMatrix, -halfWidth, halfWidth, -halfHeight, halfHeight, 0.1, 10000);

    const distance = getCameraDistance();
    const targetHeight = cameraLookHeight;
    const eyeX = viewCenterX;
    const eyeY = targetHeight + distance;
    const eyeZ = viewCenterY;

    mat4LookAt(
        viewMatrix,
        [eyeX, eyeY, eyeZ],
        [viewCenterX, targetHeight, viewCenterY],
        cameraUp);
    mat4Multiply(viewProjectionMatrix, projectionMatrix, viewMatrix);
}

function getVisibleTileRange() {
    if (terrainSize <= 0) {
        return { minTx: 0, maxTx: 0, minTy: 0, maxTy: 0 };
    }

    const terrainWorldSize = terrainSize * terrainRenderScale;
    const distance = getCameraDistance();
    const radius = Math.max(terrainWorldSize * 0.6, distance * 0.95);
    const leftWorld = viewCenterX - radius;
    const rightWorld = viewCenterX + radius;
    const topWorld = viewCenterY - radius;
    const bottomWorld = viewCenterY + radius;

    const minTx = Math.floor(leftWorld / terrainWorldSize);
    const maxTx = Math.floor(rightWorld / terrainWorldSize);
    const minTy = Math.floor(topWorld / terrainWorldSize);
    const maxTy = Math.floor(bottomWorld / terrainWorldSize);

    return { minTx, maxTx, minTy, maxTy };
}

function drawTerrainTiles() {
    if (terrainProgram === null || terrainVertexArray === null || terrainSize <= 0) {
        return;
    }

    gl.useProgram(terrainProgram.program);
    gl.bindVertexArray(terrainVertexArray);

    const modelLocation = gl.getUniformLocation(terrainProgram.program, terrainProgram.locations.model);
    const vpLocation = gl.getUniformLocation(terrainProgram.program, terrainProgram.locations.viewProjection);
    const normalLocation = gl.getUniformLocation(terrainProgram.program, terrainProgram.locations.normalMatrix);
    const lightDirectionLocation = gl.getUniformLocation(terrainProgram.program, terrainProgram.locations.lightDirection);
    const elevationScaleLocation = gl.getUniformLocation(terrainProgram.program, terrainProgram.locations.elevationScale);

    gl.uniformMatrix4fv(vpLocation, false, viewProjectionMatrix);
    gl.uniform3f(lightDirectionLocation, -0.5, 1.0, -0.25);
    gl.uniform1f(elevationScaleLocation, terrainElevationScale);

    const terrainWorldSize = terrainSize * terrainRenderScale;
    const tileRange = getVisibleTileRange();
    for (let ty = tileRange.minTy; ty <= tileRange.maxTy; ty += 1) {
        for (let tx = tileRange.minTx; tx <= tileRange.maxTx; tx += 1) {
            mat4IdentityInto(terrainModelMatrix);
            mat4Translate(terrainModelMatrix, tx * terrainWorldSize, 0, ty * terrainWorldSize);
            mat4Multiply(modelViewProjectionMatrix, viewMatrix, terrainModelMatrix);
            mat3FromMat4(normalMatrix, modelViewProjectionMatrix);

            gl.uniformMatrix4fv(modelLocation, false, terrainModelMatrix);
            gl.uniformMatrix3fv(normalLocation, false, normalMatrix);
            gl.drawElements(gl.TRIANGLES, terrainIndexCount, gl.UNSIGNED_INT, 0);
        }
    }

    gl.bindVertexArray(null);
}

function drawActors() {
    if (actorProgram === null || actorVertexArray === null || actorPointCount <= 0 || terrainSize <= 0) {
        return;
    }

    gl.useProgram(actorProgram.program);
    gl.bindVertexArray(actorVertexArray);

    const modelLocation = gl.getUniformLocation(actorProgram.program, actorProgram.locations.model);
    const vpLocation = gl.getUniformLocation(actorProgram.program, actorProgram.locations.viewProjection);
    const pointSizeLocation = gl.getUniformLocation(actorProgram.program, actorProgram.locations.pointSize);

    gl.uniformMatrix4fv(vpLocation, false, viewProjectionMatrix);
    gl.uniform1f(pointSizeLocation, Math.max(4, 10 * (zoom / maxZoom) + 6));

    const terrainWorldSize = terrainSize * terrainRenderScale;
    const tileRange = getVisibleTileRange();
    for (let ty = tileRange.minTy; ty <= tileRange.maxTy; ty += 1) {
        for (let tx = tileRange.minTx; tx <= tileRange.maxTx; tx += 1) {
            mat4IdentityInto(terrainModelMatrix);
            mat4Translate(terrainModelMatrix, tx * terrainWorldSize, 0, ty * terrainWorldSize);
            gl.uniformMatrix4fv(modelLocation, false, terrainModelMatrix);
            gl.drawArrays(gl.POINTS, 0, actorPointCount);
        }
    }

    gl.bindVertexArray(null);
}

function renderPlaceholder() {
    resizeCanvasToDisplaySize();
    gl.clearColor(0.20, 0.29, 0.25, 1);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
}

function renderTerrain() {
    resizeCanvasToDisplaySize();

    if (terrainSize <= 0 || terrainVertexArray === null) {
        renderPlaceholder();
        return;
    }

    gl.clearColor(0.13, 0.18, 0.15, 1);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
    updateCameraMatrices();
    drawTerrainTiles();
    drawActors();
}

function updateTerrainMapStatus(size, byteCount, decodeDurationMs, meshDurationMs) {
    if (terrainMapStatusElement === null) {
        return;
    }

    terrainMapStatusElement.textContent = JSON.stringify({
        size,
        tileCount: size * size,
        byteCount,
        decodeMs: Number(decodeDurationMs.toFixed(2)),
        meshUploadMs: Number(meshDurationMs.toFixed(2)),
        renderer: "webgl2",
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

    terrainSize = size;
    terrainHeightBytes = heightBytes;
    zoom = clampZoom(zoom);
    zoomTarget = zoom;
    zoomAnimationActive = false;
    viewCenterX = ((size - 1) * 0.5) * terrainRenderScale;
    viewCenterY = ((size - 1) * 0.5) * terrainRenderScale;
    const decodeEnd = performance.now();
    const meshStart = performance.now();
    uploadTerrainMesh(size, heightBytes);
    const meshEnd = performance.now();
    updateTerrainMapStatus(size, heightBytes.length, decodeEnd - decodeStart, meshEnd - meshStart);
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
    if (terrainSize <= 0) {
        return;
    }

    const distance = getCameraDistance();
    const viewportWorldWidth = (distance * 1.25) * (canvas.width / Math.max(1, canvas.height));
    const viewportWorldHeight = distance * 1.1;

    panAnimationStartTimeMs = performance.now();
    panAnimationStartX = viewCenterX;
    panAnimationStartY = viewCenterY;
    panAnimationTargetDeltaX = viewportWorldWidth * xRatio;
    panAnimationTargetDeltaY = viewportWorldHeight * yRatio;
}

function screenToGround(pointX, pointY) {
    const ndcX = ((pointX / canvas.width) * 2) - 1;
    const ndcY = 1 - ((pointY / canvas.height) * 2);
    const inverseViewProjection = mat4Identity();
    if (!mat4Invert(inverseViewProjection, viewProjectionMatrix)) {
        return { x: viewCenterX, y: viewCenterY };
    }

    const near = vec4TransformMat4([ndcX, ndcY, -1, 1], inverseViewProjection);
    const far = vec4TransformMat4([ndcX, ndcY, 1, 1], inverseViewProjection);
    if (near[3] === 0 || far[3] === 0) {
        return { x: viewCenterX, y: viewCenterY };
    }

    const nearW = [near[0] / near[3], near[1] / near[3], near[2] / near[3]];
    const farW = [far[0] / far[3], far[1] / far[3], far[2] / far[3]];
    const rayDir = [farW[0] - nearW[0], farW[1] - nearW[1], farW[2] - nearW[2]];

    if (Math.abs(rayDir[1]) < 0.00001) {
        return { x: viewCenterX, y: viewCenterY };
    }

    const t = (cameraLookHeight - nearW[1]) / rayDir[1];
    const worldX = nearW[0] + (rayDir[0] * t);
    const worldY = nearW[2] + (rayDir[2] * t);
    return { x: worldX, y: worldY };
}

function zoomAtCanvasPoint(nextZoom, anchorX, anchorY) {
    const clampedZoom = clampZoom(nextZoom);
    if (terrainSize <= 0 || Math.abs(clampedZoom - zoom) < 0.0001) {
        zoom = clampedZoom;
        zoomTarget = clampedZoom;
        zoomAnimationActive = false;
        return;
    }

    updateCameraMatrices();
    const before = screenToGround(anchorX, anchorY);
    zoomTarget = clampedZoom;
    zoomAnchorCanvasX = anchorX;
    zoomAnchorCanvasY = anchorY;
    zoomAnchorWorldX = before.x;
    zoomAnchorWorldY = before.y;
    zoomAnimationActive = true;
}

function renderFrame(nowMs) {
    updatePanAnimation(nowMs);
    updateZoomAnimation();
    updateInterpolatedActors(nowMs);
    updateActorBuffer();
    renderTerrain();
    animationFrameRequestId = requestAnimationFrame(renderFrame);
}

function startRenderLoop() {
    if (animationFrameRequestId !== null) {
        return;
    }

    animationFrameRequestId = requestAnimationFrame(renderFrame);
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
    if (!isDragging || lastDragPoint === null || terrainSize <= 0) {
        return;
    }

    const point = eventToCanvasPoint(event);
    updateCameraMatrices();
    const previousWorld = screenToGround(lastDragPoint.x, lastDragPoint.y);
    const currentWorld = screenToGround(point.x, point.y);

    viewCenterX -= currentWorld.x - previousWorld.x;
    viewCenterY -= currentWorld.y - previousWorld.y;
    lastDragPoint = point;
    normalizeViewCenter();
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

document.getElementById("reset-view").addEventListener("click", () => {
    if (terrainSize <= 0) {
        return;
    }

    zoom = 1;
    zoomTarget = 1;
    zoomAnimationActive = false;
    viewCenterX = ((terrainSize - 1) * 0.5) * terrainRenderScale;
    viewCenterY = ((terrainSize - 1) * 0.5) * terrainRenderScale;
});

window.addEventListener("resize", () => {
    resizeCanvasToDisplaySize();
});

canvas.addEventListener("webglcontextlost", (event) => {
    event.preventDefault();
    streamStatusElement.textContent = JSON.stringify({
        status: "renderer-context-lost",
        trackedActors: actors.size,
    }, null, 2);
});

canvas.addEventListener("webglcontextrestored", () => {
    terrainProgram = null;
    actorProgram = null;
    terrainVertexArray = null;
    actorVertexArray = null;
    actorBuffer = null;
    initializePrograms();

    if (terrainHeightBytes !== null && terrainSize > 0) {
        uploadTerrainMesh(terrainSize, terrainHeightBytes);
    }
});

function mat4Identity() {
    return new Float32Array([
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1,
    ]);
}

function mat3Identity() {
    return new Float32Array([
        1, 0, 0,
        0, 1, 0,
        0, 0, 1,
    ]);
}

function mat4IdentityInto(out) {
    out[0] = 1; out[1] = 0; out[2] = 0; out[3] = 0;
    out[4] = 0; out[5] = 1; out[6] = 0; out[7] = 0;
    out[8] = 0; out[9] = 0; out[10] = 1; out[11] = 0;
    out[12] = 0; out[13] = 0; out[14] = 0; out[15] = 1;
}

function mat4Multiply(out, a, b) {
    const a00 = a[0]; const a01 = a[1]; const a02 = a[2]; const a03 = a[3];
    const a10 = a[4]; const a11 = a[5]; const a12 = a[6]; const a13 = a[7];
    const a20 = a[8]; const a21 = a[9]; const a22 = a[10]; const a23 = a[11];
    const a30 = a[12]; const a31 = a[13]; const a32 = a[14]; const a33 = a[15];

    let b0 = b[0]; let b1 = b[1]; let b2 = b[2]; let b3 = b[3];
    out[0] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[1] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[2] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[3] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);

    b0 = b[4]; b1 = b[5]; b2 = b[6]; b3 = b[7];
    out[4] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[5] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[6] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[7] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);

    b0 = b[8]; b1 = b[9]; b2 = b[10]; b3 = b[11];
    out[8] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[9] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[10] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[11] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);

    b0 = b[12]; b1 = b[13]; b2 = b[14]; b3 = b[15];
    out[12] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[13] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[14] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[15] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);
}

function mat4Translate(matrix, x, y, z) {
    matrix[12] += x;
    matrix[13] += y;
    matrix[14] += z;
}

function mat4Ortho(out, left, right, bottom, top, near, far) {
    const lr = 1 / (left - right);
    const bt = 1 / (bottom - top);
    const nf = 1 / (near - far);

    out[0] = -2 * lr;
    out[1] = 0;
    out[2] = 0;
    out[3] = 0;

    out[4] = 0;
    out[5] = -2 * bt;
    out[6] = 0;
    out[7] = 0;

    out[8] = 0;
    out[9] = 0;
    out[10] = 2 * nf;
    out[11] = 0;

    out[12] = (left + right) * lr;
    out[13] = (top + bottom) * bt;
    out[14] = (far + near) * nf;
    out[15] = 1;
}

function mat4LookAt(out, eye, target, up) {
    let zx = eye[0] - target[0];
    let zy = eye[1] - target[1];
    let zz = eye[2] - target[2];
    let zLength = Math.hypot(zx, zy, zz);
    if (zLength === 0) {
        zz = 1;
        zLength = 1;
    }
    zx /= zLength;
    zy /= zLength;
    zz /= zLength;

    let xx = (up[1] * zz) - (up[2] * zy);
    let xy = (up[2] * zx) - (up[0] * zz);
    let xz = (up[0] * zy) - (up[1] * zx);
    let xLength = Math.hypot(xx, xy, xz);
    if (xLength === 0) {
        xx = 1;
        xLength = 1;
    }
    xx /= xLength;
    xy /= xLength;
    xz /= xLength;

    const yx = (zy * xz) - (zz * xy);
    const yy = (zz * xx) - (zx * xz);
    const yz = (zx * xy) - (zy * xx);

    out[0] = xx;
    out[1] = yx;
    out[2] = zx;
    out[3] = 0;
    out[4] = xy;
    out[5] = yy;
    out[6] = zy;
    out[7] = 0;
    out[8] = xz;
    out[9] = yz;
    out[10] = zz;
    out[11] = 0;
    out[12] = -((xx * eye[0]) + (xy * eye[1]) + (xz * eye[2]));
    out[13] = -((yx * eye[0]) + (yy * eye[1]) + (yz * eye[2]));
    out[14] = -((zx * eye[0]) + (zy * eye[1]) + (zz * eye[2]));
    out[15] = 1;
}

function mat3FromMat4(out, matrix) {
    out[0] = matrix[0];
    out[1] = matrix[1];
    out[2] = matrix[2];
    out[3] = matrix[4];
    out[4] = matrix[5];
    out[5] = matrix[6];
    out[6] = matrix[8];
    out[7] = matrix[9];
    out[8] = matrix[10];
}

function mat4Invert(out, matrix) {
    const a00 = matrix[0]; const a01 = matrix[1]; const a02 = matrix[2]; const a03 = matrix[3];
    const a10 = matrix[4]; const a11 = matrix[5]; const a12 = matrix[6]; const a13 = matrix[7];
    const a20 = matrix[8]; const a21 = matrix[9]; const a22 = matrix[10]; const a23 = matrix[11];
    const a30 = matrix[12]; const a31 = matrix[13]; const a32 = matrix[14]; const a33 = matrix[15];

    const b00 = (a00 * a11) - (a01 * a10);
    const b01 = (a00 * a12) - (a02 * a10);
    const b02 = (a00 * a13) - (a03 * a10);
    const b03 = (a01 * a12) - (a02 * a11);
    const b04 = (a01 * a13) - (a03 * a11);
    const b05 = (a02 * a13) - (a03 * a12);
    const b06 = (a20 * a31) - (a21 * a30);
    const b07 = (a20 * a32) - (a22 * a30);
    const b08 = (a20 * a33) - (a23 * a30);
    const b09 = (a21 * a32) - (a22 * a31);
    const b10 = (a21 * a33) - (a23 * a31);
    const b11 = (a22 * a33) - (a23 * a32);

    const determinant = (b00 * b11) - (b01 * b10) + (b02 * b09) + (b03 * b08) - (b04 * b07) + (b05 * b06);
    if (!determinant) {
        return false;
    }

    const inverseDet = 1 / determinant;
    out[0] = ((a11 * b11) - (a12 * b10) + (a13 * b09)) * inverseDet;
    out[1] = ((a02 * b10) - (a01 * b11) - (a03 * b09)) * inverseDet;
    out[2] = ((a31 * b05) - (a32 * b04) + (a33 * b03)) * inverseDet;
    out[3] = ((a22 * b04) - (a21 * b05) - (a23 * b03)) * inverseDet;
    out[4] = ((a12 * b08) - (a10 * b11) - (a13 * b07)) * inverseDet;
    out[5] = ((a00 * b11) - (a02 * b08) + (a03 * b07)) * inverseDet;
    out[6] = ((a32 * b02) - (a30 * b05) - (a33 * b01)) * inverseDet;
    out[7] = ((a20 * b05) - (a22 * b02) + (a23 * b01)) * inverseDet;
    out[8] = ((a10 * b10) - (a11 * b08) + (a13 * b06)) * inverseDet;
    out[9] = ((a01 * b08) - (a00 * b10) - (a03 * b06)) * inverseDet;
    out[10] = ((a30 * b04) - (a31 * b02) + (a33 * b00)) * inverseDet;
    out[11] = ((a21 * b02) - (a20 * b04) - (a23 * b00)) * inverseDet;
    out[12] = ((a11 * b07) - (a10 * b09) - (a12 * b06)) * inverseDet;
    out[13] = ((a00 * b09) - (a01 * b07) + (a02 * b06)) * inverseDet;
    out[14] = ((a31 * b01) - (a30 * b03) - (a32 * b00)) * inverseDet;
    out[15] = ((a20 * b03) - (a21 * b01) + (a22 * b00)) * inverseDet;
    return true;
}

function vec4TransformMat4(vector, matrix) {
    const x = vector[0];
    const y = vector[1];
    const z = vector[2];
    const w = vector[3];
    return [
        (matrix[0] * x) + (matrix[4] * y) + (matrix[8] * z) + (matrix[12] * w),
        (matrix[1] * x) + (matrix[5] * y) + (matrix[9] * z) + (matrix[13] * w),
        (matrix[2] * x) + (matrix[6] * y) + (matrix[10] * z) + (matrix[14] * w),
        (matrix[3] * x) + (matrix[7] * y) + (matrix[11] * z) + (matrix[15] * w),
    ];
}

initializePrograms();
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
