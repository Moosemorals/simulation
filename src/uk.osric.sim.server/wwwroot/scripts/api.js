import { applyTickPayload, hydrateActorsSnapshot } from "./actors.js";
import { state, streamStatusElement, terrainMapStatusElement, terrainSeedElement } from "./state.js";
import { hydrateTerrainMap } from "./terrain.js";

export async function loadTerrainSeed() {
    const response = await fetch("/api/terrain/seed");

    if (!response.ok) {
        throw new Error(`Failed to load terrain seed (${response.status}).`);
    }

    const payload = await response.json();
    terrainSeedElement.textContent = JSON.stringify(payload, null, 2);
}

export async function loadTerrainHeightMap() {
    const response = await fetch("/api/terrain/heightmap");

    if (!response.ok) {
        throw new Error(`Failed to load terrain height map (${response.status}).`);
    }

    const payload = await response.json();
    hydrateTerrainMap(payload);
}

export async function loadActorsSnapshot() {
    const response = await fetch("/api/simulation/actors");

    if (!response.ok) {
        throw new Error(`Failed to load actors (${response.status}).`);
    }

    const payload = await response.json();
    hydrateActorsSnapshot(payload);
}

export function connectSimulationStream() {
    if (state.simulationStream !== null) {
        state.simulationStream.close();
    }

    const eventSource = new EventSource("/api/simulation/stream");
    state.simulationStream = eventSource;

    streamStatusElement.textContent = JSON.stringify({
        status: "connecting",
        trackedActors: state.actors.size,
    }, null, 2);

    eventSource.addEventListener("tick", (event) => {
        const payload = JSON.parse(event.data);
        const applied = applyTickPayload(payload);

        if (!applied) {
            return;
        }

        streamStatusElement.textContent = JSON.stringify({
            status: "connected",
            sequence: state.lastTickSequence,
            changes: Array.isArray(payload.locationChanges) ? payload.locationChanges.length : 0,
            trackedActors: state.actors.size,
        }, null, 2);
    });

    eventSource.onerror = () => {
        streamStatusElement.textContent = JSON.stringify({
            status: "disconnected",
            lastSequence: state.lastTickSequence,
            trackedActors: state.actors.size,
        }, null, 2);
    };
}

export function reportTerrainLoadError(error) {
    const message = error instanceof Error ? error.message : String(error);
    terrainSeedElement.textContent = message;
    if (terrainMapStatusElement !== null) {
        terrainMapStatusElement.textContent = message;
    }
}

export function reportActorsLoadError(error) {
    streamStatusElement.textContent = JSON.stringify({
        status: "waiting-for-simulation",
        message: error instanceof Error ? error.message : String(error),
        trackedActors: state.actors.size,
    }, null, 2);
}