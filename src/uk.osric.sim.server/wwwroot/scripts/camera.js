import {
    clamp,
    easeInOutCubic,
    mat4Identity,
    mat4Invert,
    mat4LookAt,
    mat4Multiply,
    mat4Ortho,
    vec4TransformMat4,
    wrapCoordinate,
} from "./math.js";
import { canvas, maxZoom, minZoom, state, terrainRenderScale } from "./state.js";

function getCameraLookHeight() {
    return state.terrainElevationScale * 0.35;
}

export function normalizeViewCenter() {
    if (state.terrainSize > 0) {
        const terrainWorldSize = state.terrainSize * terrainRenderScale;
        state.viewCenterX = wrapCoordinate(state.viewCenterX, terrainWorldSize);
        state.viewCenterY = wrapCoordinate(state.viewCenterY, terrainWorldSize);
    }
}

export function clampZoom(nextZoom) {
    return clamp(nextZoom, minZoom, maxZoom);
}

export function getCameraDistance() {
    if (state.terrainSize <= 0) {
        return 640;
    }

    const baseDistance = state.terrainSize * 1.15;
    return baseDistance / state.zoom;
}

export function getOrthographicHalfHeight() {
    if (state.terrainSize <= 0) {
        return 320 / state.zoom;
    }

    return (state.terrainSize * 0.62) / state.zoom;
}

export function updatePanAnimation(nowMs) {
    if (state.panAnimationStartTimeMs === null) {
        return;
    }

    const elapsedMs = nowMs - state.panAnimationStartTimeMs;
    const rawProgress = elapsedMs / state.panAnimationDurationMs;

    if (rawProgress >= 1.0) {
        state.viewCenterX = state.panAnimationStartX + state.panAnimationTargetDeltaX;
        state.viewCenterY = state.panAnimationStartY + state.panAnimationTargetDeltaY;
        normalizeViewCenter();
        state.panAnimationStartTimeMs = null;
        return;
    }

    const easeProgress = easeInOutCubic(rawProgress);
    state.viewCenterX = state.panAnimationStartX + (state.panAnimationTargetDeltaX * easeProgress);
    state.viewCenterY = state.panAnimationStartY + (state.panAnimationTargetDeltaY * easeProgress);
    normalizeViewCenter();
}

export function updateCameraMatrices() {
    const aspect = canvas.width / Math.max(1, canvas.height);
    const halfHeight = getOrthographicHalfHeight();
    const halfWidth = halfHeight * aspect;
    mat4Ortho(state.projectionMatrix, -halfWidth, halfWidth, -halfHeight, halfHeight, 0.1, 10000);

    const distance = getCameraDistance();
    const targetHeight = getCameraLookHeight();
    const eyeX = state.viewCenterX;
    const eyeY = targetHeight + distance;
    const eyeZ = state.viewCenterY;

    mat4LookAt(
        state.viewMatrix,
        [eyeX, eyeY, eyeZ],
        [state.viewCenterX, targetHeight, state.viewCenterY],
        state.cameraUp);
    mat4Multiply(state.viewProjectionMatrix, state.projectionMatrix, state.viewMatrix);
}

export function screenToGround(pointX, pointY) {
    const ndcX = ((pointX / canvas.width) * 2) - 1;
    const ndcY = 1 - ((pointY / canvas.height) * 2);
    const inverseViewProjection = mat4Identity();
    if (!mat4Invert(inverseViewProjection, state.viewProjectionMatrix)) {
        return { x: state.viewCenterX, y: state.viewCenterY };
    }

    const near = vec4TransformMat4([ndcX, ndcY, -1, 1], inverseViewProjection);
    const far = vec4TransformMat4([ndcX, ndcY, 1, 1], inverseViewProjection);
    if (near[3] === 0 || far[3] === 0) {
        return { x: state.viewCenterX, y: state.viewCenterY };
    }

    const nearW = [near[0] / near[3], near[1] / near[3], near[2] / near[3]];
    const farW = [far[0] / far[3], far[1] / far[3], far[2] / far[3]];
    const rayDir = [farW[0] - nearW[0], farW[1] - nearW[1], farW[2] - nearW[2]];

    if (Math.abs(rayDir[1]) < 0.00001) {
        return { x: state.viewCenterX, y: state.viewCenterY };
    }

    const t = (getCameraLookHeight() - nearW[1]) / rayDir[1];
    const worldX = nearW[0] + (rayDir[0] * t);
    const worldY = nearW[2] + (rayDir[2] * t);
    return { x: worldX, y: worldY };
}

export function updateZoomAnimation() {
    if (!state.zoomAnimationActive || state.terrainSize <= 0) {
        return;
    }

    const delta = state.zoomTarget - state.zoom;
    if (Math.abs(delta) < 0.00015) {
        state.zoom = state.zoomTarget;
        state.zoomAnimationActive = false;
        return;
    }

    state.zoom += delta * 0.22;
    updateCameraMatrices();
    const anchoredWorld = screenToGround(state.zoomAnchorCanvasX, state.zoomAnchorCanvasY);
    state.viewCenterX += state.zoomAnchorWorldX - anchoredWorld.x;
    state.viewCenterY += state.zoomAnchorWorldY - anchoredWorld.y;
    normalizeViewCenter();
}

export function zoomAtCanvasPoint(nextZoom, anchorX, anchorY) {
    const clampedZoom = clampZoom(nextZoom);
    if (state.terrainSize <= 0 || Math.abs(clampedZoom - state.zoom) < 0.0001) {
        state.zoom = clampedZoom;
        state.zoomTarget = clampedZoom;
        state.zoomAnimationActive = false;
        return;
    }

    updateCameraMatrices();
    const before = screenToGround(anchorX, anchorY);
    state.zoomTarget = clampedZoom;
    state.zoomAnchorCanvasX = anchorX;
    state.zoomAnchorCanvasY = anchorY;
    state.zoomAnchorWorldX = before.x;
    state.zoomAnchorWorldY = before.y;
    state.zoomAnimationActive = true;
}

export function panByViewportRatio(xRatio, yRatio) {
    if (state.terrainSize <= 0) {
        return;
    }

    const distance = getCameraDistance();
    const viewportWorldWidth = (distance * 1.25) * (canvas.width / Math.max(1, canvas.height));
    const viewportWorldHeight = distance * 1.1;

    state.panAnimationStartTimeMs = performance.now();
    state.panAnimationStartX = state.viewCenterX;
    state.panAnimationStartY = state.viewCenterY;
    state.panAnimationTargetDeltaX = viewportWorldWidth * xRatio;
    state.panAnimationTargetDeltaY = viewportWorldHeight * yRatio;
}

export function resetView() {
    if (state.terrainSize <= 0) {
        return;
    }

    state.zoom = 1;
    state.zoomTarget = 1;
    state.zoomAnimationActive = false;
    state.viewCenterX = ((state.terrainSize - 1) * 0.5) * terrainRenderScale;
    state.viewCenterY = ((state.terrainSize - 1) * 0.5) * terrainRenderScale;
}