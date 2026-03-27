import { updateInterpolatedActors } from "./actors.js";
import {
    getCameraDistance,
    updateCameraMatrices,
    updatePanAnimation,
    updateZoomAnimation,
} from "./camera.js";
import { mat3FromMat4, mat4IdentityInto, mat4Multiply, mat4Translate } from "./math.js";
import { gl, canvas, state, streamStatusElement, terrainRenderScale } from "./state.js";
import { resetProgramState, initializePrograms } from "./shaders.js";
import { getElevationAt, getTerrainNormalAt, uploadTerrainMesh } from "./terrain.js";

function ensureActorBuffers() {
    if (state.actorVertexArray !== null && state.actorBuffer !== null) {
        return;
    }

    state.actorVertexArray = gl.createVertexArray();
    state.actorBuffer = gl.createBuffer();

    if (state.actorVertexArray === null || state.actorBuffer === null) {
        throw new Error("Unable to allocate actor rendering buffers.");
    }

    gl.bindVertexArray(state.actorVertexArray);
    gl.bindBuffer(gl.ARRAY_BUFFER, state.actorBuffer);
    gl.enableVertexAttribArray(0);
    gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 11 * Float32Array.BYTES_PER_ELEMENT, 0);
    gl.enableVertexAttribArray(1);
    gl.vertexAttribPointer(1, 2, gl.FLOAT, false, 11 * Float32Array.BYTES_PER_ELEMENT, 3 * Float32Array.BYTES_PER_ELEMENT);
    gl.enableVertexAttribArray(2);
    gl.vertexAttribPointer(2, 3, gl.FLOAT, false, 11 * Float32Array.BYTES_PER_ELEMENT, 5 * Float32Array.BYTES_PER_ELEMENT);
    gl.enableVertexAttribArray(3);
    gl.vertexAttribPointer(3, 3, gl.FLOAT, false, 11 * Float32Array.BYTES_PER_ELEMENT, 8 * Float32Array.BYTES_PER_ELEMENT);
    gl.bindVertexArray(null);
}

export function updateActorBuffer() {
    ensureActorBuffers();

    const offsets = [
        -1, -1,
        1, -1,
        1, 1,
        -1, -1,
        1, 1,
        -1, 1,
    ];

    const floatsPerVertex = 11;
    const verticesPerActor = 6;
    const data = new Float32Array(state.actors.size * verticesPerActor * floatsPerVertex);
    let write = 0;

    for (const actor of state.actors.values()) {
        const centerX = actor.drawX * terrainRenderScale;
        const centerY = getElevationAt(actor.drawX, actor.drawY) + 1.7;
        const centerZ = actor.drawY * terrainRenderScale;
        const normal = getTerrainNormalAt(actor.drawX, actor.drawY);

        let forwardX = actor.headingX;
        let forwardY = 0;
        let forwardZ = actor.headingY;
        const forwardDotNormal = (forwardX * normal[0]) + (forwardY * normal[1]) + (forwardZ * normal[2]);
        forwardX -= normal[0] * forwardDotNormal;
        forwardY -= normal[1] * forwardDotNormal;
        forwardZ -= normal[2] * forwardDotNormal;

        const forwardLength = Math.hypot(forwardX, forwardY, forwardZ);
        if (forwardLength > 0.0001) {
            forwardX /= forwardLength;
            forwardY /= forwardLength;
            forwardZ /= forwardLength;
        } else {
            forwardX = 1;
            forwardY = 0;
            forwardZ = 0;
        }

        let rightX = (normal[1] * forwardZ) - (normal[2] * forwardY);
        let rightY = (normal[2] * forwardX) - (normal[0] * forwardZ);
        let rightZ = (normal[0] * forwardY) - (normal[1] * forwardX);
        const rightLength = Math.hypot(rightX, rightY, rightZ);

        if (rightLength > 0.0001) {
            rightX /= rightLength;
            rightY /= rightLength;
            rightZ /= rightLength;
        } else {
            rightX = 0;
            rightY = 0;
            rightZ = 1;
        }

        for (let i = 0; i < verticesPerActor; i += 1) {
            const offsetIndex = i * 2;
            data[write] = centerX;
            data[write + 1] = centerY;
            data[write + 2] = centerZ;
            data[write + 3] = offsets[offsetIndex];
            data[write + 4] = offsets[offsetIndex + 1];
            data[write + 5] = forwardX;
            data[write + 6] = forwardY;
            data[write + 7] = forwardZ;
            data[write + 8] = rightX;
            data[write + 9] = rightY;
            data[write + 10] = rightZ;
            write += floatsPerVertex;
        }
    }

    gl.bindBuffer(gl.ARRAY_BUFFER, state.actorBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, data, gl.DYNAMIC_DRAW);
    state.actorVertexCount = state.actors.size * verticesPerActor;
}

export function resizeCanvasToDisplaySize() {
    const devicePixelRatio = Math.max(1, window.devicePixelRatio || 1);
    const width = Math.max(1, Math.floor(canvas.clientWidth * devicePixelRatio));
    const height = Math.max(1, Math.floor(canvas.clientHeight * devicePixelRatio));

    if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width;
        canvas.height = height;
    }

    gl.viewport(0, 0, canvas.width, canvas.height);
}

function getVisibleTileRange() {
    if (state.terrainSize <= 0) {
        return { minTx: 0, maxTx: 0, minTy: 0, maxTy: 0 };
    }

    const terrainWorldSize = state.terrainSize * terrainRenderScale;
    const distance = getCameraDistance();
    const radius = Math.max(terrainWorldSize * 0.6, distance * 0.95);
    const leftWorld = state.viewCenterX - radius;
    const rightWorld = state.viewCenterX + radius;
    const topWorld = state.viewCenterY - radius;
    const bottomWorld = state.viewCenterY + radius;

    const minTx = Math.floor(leftWorld / terrainWorldSize);
    const maxTx = Math.floor(rightWorld / terrainWorldSize);
    const minTy = Math.floor(topWorld / terrainWorldSize);
    const maxTy = Math.floor(bottomWorld / terrainWorldSize);

    return { minTx, maxTx, minTy, maxTy };
}

function drawTerrainTiles() {
    if (state.terrainProgram === null || state.terrainVertexArray === null || state.terrainSize <= 0) {
        return;
    }

    gl.useProgram(state.terrainProgram.program);
    gl.bindVertexArray(state.terrainVertexArray);

    gl.uniformMatrix4fv(state.terrainProgram.locations.viewProjection, false, state.viewProjectionMatrix);
    gl.uniform3f(state.terrainProgram.locations.lightDirection, -0.5, 1.0, -0.25);
    gl.uniform1f(state.terrainProgram.locations.elevationScale, state.terrainElevationScale);

    const terrainWorldSize = state.terrainSize * terrainRenderScale;
    const tileRange = getVisibleTileRange();
    for (let ty = tileRange.minTy; ty <= tileRange.maxTy; ty += 1) {
        for (let tx = tileRange.minTx; tx <= tileRange.maxTx; tx += 1) {
            mat4IdentityInto(state.terrainModelMatrix);
            mat4Translate(state.terrainModelMatrix, tx * terrainWorldSize, 0, ty * terrainWorldSize);
            mat4Multiply(state.modelViewProjectionMatrix, state.viewMatrix, state.terrainModelMatrix);
            mat3FromMat4(state.normalMatrix, state.modelViewProjectionMatrix);

            gl.uniformMatrix4fv(state.terrainProgram.locations.model, false, state.terrainModelMatrix);
            gl.uniformMatrix3fv(state.terrainProgram.locations.normalMatrix, false, state.normalMatrix);
            gl.drawElements(gl.TRIANGLES, state.terrainIndexCount, gl.UNSIGNED_INT, 0);
        }
    }

    gl.bindVertexArray(null);
}

function drawActors() {
    if (state.actorProgram === null || state.actorVertexArray === null || state.actorVertexCount <= 0 || state.terrainSize <= 0) {
        return;
    }

    gl.useProgram(state.actorProgram.program);
    gl.bindVertexArray(state.actorVertexArray);

    gl.uniformMatrix4fv(state.actorProgram.locations.viewProjection, false, state.viewProjectionMatrix);
    gl.uniform1f(state.actorProgram.locations.halfLength, state.sheepRadius * 2);
    gl.uniform1f(state.actorProgram.locations.halfWidth, state.sheepRadius);

    const terrainWorldSize = state.terrainSize * terrainRenderScale;
    const tileRange = getVisibleTileRange();
    for (let ty = tileRange.minTy; ty <= tileRange.maxTy; ty += 1) {
        for (let tx = tileRange.minTx; tx <= tileRange.maxTx; tx += 1) {
            mat4IdentityInto(state.terrainModelMatrix);
            mat4Translate(state.terrainModelMatrix, tx * terrainWorldSize, 0, ty * terrainWorldSize);
            gl.uniformMatrix4fv(state.actorProgram.locations.model, false, state.terrainModelMatrix);
            gl.drawArrays(gl.TRIANGLES, 0, state.actorVertexCount);
        }
    }

    gl.bindVertexArray(null);
}

export function renderPlaceholder() {
    resizeCanvasToDisplaySize();
    gl.clearColor(0.20, 0.29, 0.25, 1);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
}

export function renderTerrain() {
    resizeCanvasToDisplaySize();

    if (state.terrainSize <= 0 || state.terrainVertexArray === null) {
        renderPlaceholder();
        return;
    }

    gl.clearColor(0.13, 0.18, 0.15, 1);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
    updateCameraMatrices();
    drawTerrainTiles();
    drawActors();
}

function renderFrame(nowMs) {
    updatePanAnimation(nowMs);
    updateZoomAnimation();
    updateInterpolatedActors(nowMs);
    updateActorBuffer();
    renderTerrain();
    state.animationFrameRequestId = requestAnimationFrame(renderFrame);
}

export function startRenderLoop() {
    if (state.animationFrameRequestId !== null) {
        return;
    }

    state.animationFrameRequestId = requestAnimationFrame(renderFrame);
}

export function registerContextHandlers() {
    canvas.addEventListener("webglcontextlost", (event) => {
        event.preventDefault();
        streamStatusElement.textContent = JSON.stringify({
            status: "renderer-context-lost",
            trackedActors: state.actors.size,
        }, null, 2);
    });

    canvas.addEventListener("webglcontextrestored", () => {
        resetProgramState();
        initializePrograms();

        if (state.terrainHeightBytes !== null && state.terrainSize > 0) {
            uploadTerrainMesh(state.terrainSize, state.terrainHeightBytes);
        }
    });
}