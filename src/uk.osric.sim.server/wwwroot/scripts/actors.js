import { clamp, interpolateWrapped, shortestWrappedDelta } from "./math.js";
import { state } from "./state.js";

function updateActorState(actor, x, y, velocityX, velocityY) {
    actor.previousX = actor.currentX;
    actor.previousY = actor.currentY;
    actor.currentX = x;
    actor.currentY = y;

    let nextVelocityX = velocityX;
    let nextVelocityY = velocityY;

    if (!Number.isFinite(nextVelocityX) || !Number.isFinite(nextVelocityY)) {
        const dt = Math.max(0.001, state.tickIntervalMs / 1000);
        nextVelocityX = shortestWrappedDelta(actor.previousX, actor.currentX, state.terrainSize) / dt;
        nextVelocityY = shortestWrappedDelta(actor.previousY, actor.currentY, state.terrainSize) / dt;
    }

    actor.velocityX = nextVelocityX;
    actor.velocityY = nextVelocityY;

    const speed = Math.hypot(nextVelocityX, nextVelocityY);
    if (speed > 0.0001) {
        actor.headingX = nextVelocityX / speed;
        actor.headingY = nextVelocityY / speed;
    }
}

export function upsertActor(entityId, x, y, velocityX, velocityY) {
    const existingActor = state.actors.get(entityId);

    if (existingActor !== undefined) {
        updateActorState(existingActor, x, y, velocityX, velocityY);
        return;
    }

    const initialVelocityX = Number.isFinite(velocityX) ? velocityX : 0;
    const initialVelocityY = Number.isFinite(velocityY) ? velocityY : 0;
    const speed = Math.hypot(initialVelocityX, initialVelocityY);
    const headingX = speed > 0.0001 ? initialVelocityX / speed : 1;
    const headingY = speed > 0.0001 ? initialVelocityY / speed : 0;

    state.actors.set(entityId, {
        previousX: x,
        previousY: y,
        currentX: x,
        currentY: y,
        drawX: x,
        drawY: y,
        velocityX: initialVelocityX,
        velocityY: initialVelocityY,
        headingX,
        headingY,
    });
}

export function updateInterpolatedActors(nowMs) {
    const hasTickAnchor = state.lastTickAppliedAtMs > 0;
    const rawAlpha = hasTickAnchor ? ((nowMs - state.lastTickAppliedAtMs) / state.tickIntervalMs) : 1;
    const alpha = clamp(rawAlpha, 0, 1);

    for (const actor of state.actors.values()) {
        actor.drawX = interpolateWrapped(actor.previousX, actor.currentX, alpha, state.terrainSize);
        actor.drawY = interpolateWrapped(actor.previousY, actor.currentY, alpha, state.terrainSize);
    }
}

export function hydrateActorsSnapshot(payload) {
    state.actors.clear();

    const firstRadius = payload.length > 0 ? payload[0].radius : null;
    if (Number.isFinite(firstRadius) && firstRadius > 0) {
        state.sheepRadius = firstRadius;
    }

    for (const actor of payload) {
        const velocityX = Number.isFinite(actor.velocityX) ? actor.velocityX : 0;
        const velocityY = Number.isFinite(actor.velocityY) ? actor.velocityY : 0;
        upsertActor(actor.entityId, actor.x, actor.y, velocityX, velocityY);
    }
}

export function applyTickPayload(payload) {
    const sequence = Number.isInteger(payload.sequence) ? payload.sequence : null;

    if (sequence === null) {
        return false;
    }

    if (state.lastTickSequence !== null && sequence <= state.lastTickSequence) {
        return false;
    }

    if (Number.isFinite(payload.tickRateHz) && payload.tickRateHz > 0) {
        state.tickIntervalMs = 1000 / payload.tickRateHz;
    }

    if (Array.isArray(payload.locationChanges)) {
        for (const change of payload.locationChanges) {
            if (!Number.isInteger(change.entityId) || !Number.isFinite(change.x) || !Number.isFinite(change.y)) {
                continue;
            }

            const velocityX = Number.isFinite(change.velocityX) ? change.velocityX : Number.NaN;
            const velocityY = Number.isFinite(change.velocityY) ? change.velocityY : Number.NaN;
            upsertActor(change.entityId, change.x, change.y, velocityX, velocityY);
        }
    }

    state.lastTickAppliedAtMs = performance.now();
    state.lastTickSequence = sequence;
    return true;
}