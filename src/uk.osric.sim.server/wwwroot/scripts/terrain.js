import { clampZoom } from "./camera.js";
import { wrapCoordinate } from "./math.js";
import {
    gl,
    state,
    terrainMapStatusElement,
    terrainRenderScale,
} from "./state.js";

export function decodeBase64(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);

    for (let i = 0; i < binary.length; i += 1) {
        bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
}

export function decodeBase64Float32(base64) {
    const bytes = decodeBase64(base64);
    if (bytes.byteLength % Float32Array.BYTES_PER_ELEMENT !== 0) {
        throw new Error("Float32 payload size was not a multiple of 4 bytes.");
    }

    return new Float32Array(bytes.buffer, bytes.byteOffset, bytes.byteLength / Float32Array.BYTES_PER_ELEMENT);
}

export function getNormalAt(heightValues, size, x, y) {
    const left = heightValues[(y * size) + wrapCoordinate(x - 1, size)];
    const right = heightValues[(y * size) + wrapCoordinate(x + 1, size)];
    const up = heightValues[(wrapCoordinate(y - 1, size) * size) + x];
    const down = heightValues[(wrapCoordinate(y + 1, size) * size) + x];

    const dx = ((right - left) * state.terrainElevationScale) / terrainRenderScale;
    const dz = ((down - up) * state.terrainElevationScale) / terrainRenderScale;
    const nx = -dx;
    const ny = 2;
    const nz = -dz;
    const invLength = 1 / Math.hypot(nx, ny, nz);

    return [nx * invLength, ny * invLength, nz * invLength];
}

export function getElevationAt(worldX, worldY) {
    if (state.terrainHeightValues === null || state.terrainSize <= 0) {
        return 0;
    }

    const x = wrapCoordinate(Math.round(worldX), state.terrainSize);
    const y = wrapCoordinate(Math.round(worldY), state.terrainSize);
    const sample = state.terrainHeightValues[(y * state.terrainSize) + x] ?? 0;
    return sample * state.terrainElevationScale;
}

export function getTerrainNormalAt(worldX, worldY) {
    if (state.terrainHeightValues === null || state.terrainSize <= 0) {
        return [0, 1, 0];
    }

    const x = wrapCoordinate(Math.round(worldX), state.terrainSize);
    const y = wrapCoordinate(Math.round(worldY), state.terrainSize);
    return getNormalAt(state.terrainHeightValues, state.terrainSize, x, y);
}

export function updateTerrainMapStatus(size, floatCount, decodeDurationMs, meshDurationMs) {
    if (terrainMapStatusElement === null) {
        return;
    }

    terrainMapStatusElement.textContent = JSON.stringify({
        size,
        tileCount: size * size,
        floatCount,
        decodeMs: Number(decodeDurationMs.toFixed(2)),
        meshUploadMs: Number(meshDurationMs.toFixed(2)),
        renderer: "webgl2",
    }, null, 2);
}

export function uploadTerrainMesh(size, heightValues) {
    if (state.terrainVertexArray !== null) {
        gl.deleteVertexArray(state.terrainVertexArray);
        state.terrainVertexArray = null;
    }

    if (state.terrainVertexBuffer !== null) {
        gl.deleteBuffer(state.terrainVertexBuffer);
        state.terrainVertexBuffer = null;
    }

    if (state.terrainIndexBuffer !== null) {
        gl.deleteBuffer(state.terrainIndexBuffer);
        state.terrainIndexBuffer = null;
    }

    const vertexCount = size * size;
    const stride = 6;
    const vertices = new Float32Array(vertexCount * stride);

    for (let y = 0; y < size; y += 1) {
        for (let x = 0; x < size; x += 1) {
            const index = (y * size) + x;
            const offset = index * stride;
            const height = heightValues[index] * state.terrainElevationScale;
            const normal = getNormalAt(heightValues, size, x, y);

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

    state.terrainVertexArray = gl.createVertexArray();

    if (state.terrainVertexArray === null) {
        throw new Error("Unable to allocate terrain vertex array.");
    }

    state.terrainVertexBuffer = gl.createBuffer();
    state.terrainIndexBuffer = gl.createBuffer();

    if (state.terrainVertexBuffer === null || state.terrainIndexBuffer === null) {
        throw new Error("Unable to allocate terrain buffers.");
    }

    gl.bindVertexArray(state.terrainVertexArray);
    gl.bindBuffer(gl.ARRAY_BUFFER, state.terrainVertexBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

    const byteStride = stride * Float32Array.BYTES_PER_ELEMENT;
    gl.enableVertexAttribArray(0);
    gl.vertexAttribPointer(0, 3, gl.FLOAT, false, byteStride, 0);
    gl.enableVertexAttribArray(1);
    gl.vertexAttribPointer(1, 3, gl.FLOAT, false, byteStride, 3 * Float32Array.BYTES_PER_ELEMENT);

    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, state.terrainIndexBuffer);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, indices, gl.STATIC_DRAW);
    gl.bindVertexArray(null);

    state.terrainIndexCount = indexCount;
}

export function hydrateTerrainMap(payload) {
    const decodeStart = performance.now();
    const size = payload.size;
    const heightValues = decodeBase64Float32(payload.heightFloatDataBase64);

    if (heightValues.length !== size * size) {
        throw new Error("Height map payload size does not match map dimensions.");
    }

    state.terrainSize = size;
    state.terrainHeightValues = heightValues;
    state.zoom = clampZoom(state.zoom);
    state.zoomTarget = state.zoom;
    state.zoomAnimationActive = false;
    state.viewCenterX = ((size - 1) * 0.5) * terrainRenderScale;
    state.viewCenterY = ((size - 1) * 0.5) * terrainRenderScale;
    const decodeEnd = performance.now();
    const meshStart = performance.now();
    uploadTerrainMesh(size, heightValues);
    const meshEnd = performance.now();
    updateTerrainMapStatus(size, heightValues.length, decodeEnd - decodeStart, meshEnd - meshStart);
}