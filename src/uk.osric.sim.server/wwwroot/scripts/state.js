import { mat3Identity, mat4Identity } from "./math.js";

export const terrainSeedElement = document.getElementById("terrain-seed");
export const terrainMapStatusElement = document.getElementById("terrain-map-status");
export const streamStatusElement = document.getElementById("stream-status");
export const canvas = document.getElementById("sim-canvas");
export const gl = canvas.getContext("webgl2", { antialias: true });

if (gl === null) {
    throw new Error("WebGL2 is required but was not available in this browser.");
}

export const minZoom = 0.35;
export const maxZoom = 3.5;
export const terrainRenderScale = 16;

export const state = {
    zoom: 1,
    zoomTarget: 1,
    terrainSize: 0,
    terrainHeightBytes: null,
    terrainElevationScale: 42,
    viewCenterX: 0,
    viewCenterY: 0,
    zoomAnchorCanvasX: 0,
    zoomAnchorCanvasY: 0,
    zoomAnchorWorldX: 0,
    zoomAnchorWorldY: 0,
    zoomAnimationActive: false,
    isDragging: false,
    lastDragPoint: null,
    actors: new Map(),
    lastTickSequence: null,
    lastTickAppliedAtMs: 0,
    tickIntervalMs: 100,
    animationFrameRequestId: null,
    panAnimationStartTimeMs: null,
    panAnimationDurationMs: 250,
    panAnimationStartX: 0,
    panAnimationStartY: 0,
    panAnimationTargetDeltaX: 0,
    panAnimationTargetDeltaY: 0,
    cameraUp: [0, 0, -1],
    projectionMatrix: mat4Identity(),
    viewMatrix: mat4Identity(),
    viewProjectionMatrix: mat4Identity(),
    terrainModelMatrix: mat4Identity(),
    modelViewProjectionMatrix: mat4Identity(),
    normalMatrix: mat3Identity(),
    terrainProgram: null,
    terrainVertexArray: null,
    terrainVertexBuffer: null,
    terrainIndexBuffer: null,
    terrainIndexCount: 0,
    actorProgram: null,
    actorVertexArray: null,
    actorBuffer: null,
    actorVertexCount: 0,
    sheepRadius: 4,
    simulationStream: null,
};