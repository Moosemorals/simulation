import {
    connectSimulationStream,
    loadActorsSnapshot,
    loadTerrainHeightMap,
    loadTerrainSeed,
    reportActorsLoadError,
    reportTerrainLoadError,
} from "./api.js";
import { registerInputHandlers } from "./input.js";
import { renderPlaceholder, registerContextHandlers, startRenderLoop } from "./render.js";
import { initializePrograms } from "./shaders.js";

function initializeApp() {
    initializePrograms();
    registerInputHandlers();
    registerContextHandlers();
    renderPlaceholder();
    startRenderLoop();

    Promise.all([loadTerrainSeed(), loadTerrainHeightMap()]).catch(reportTerrainLoadError);

    loadActorsSnapshot().catch(reportActorsLoadError);
    connectSimulationStream();
}

initializeApp();