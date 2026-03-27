const terrainSeedElement = document.getElementById("terrain-seed");
const streamStatusElement = document.getElementById("stream-status");
const canvas = document.getElementById("sim-canvas");
const context = canvas.getContext("2d");

let zoom = 1;

function renderPlaceholder() {
    if (context === null) {
        return;
    }

    context.clearRect(0, 0, canvas.width, canvas.height);
    context.save();
    context.scale(zoom, zoom);

    const gradient = context.createLinearGradient(0, 0, 0, canvas.height);
    gradient.addColorStop(0, "#7f9f72");
    gradient.addColorStop(1, "#345244");
    context.fillStyle = gradient;
    context.fillRect(0, 0, canvas.width, canvas.height);

    context.fillStyle = "rgba(239, 225, 191, 0.65)";
    context.fillRect(40, 48, 180, 92);
    context.fillStyle = "#1c1a16";
    context.font = "28px Georgia";
    context.fillText("Viewport Scaffold", 56, 102);

    context.restore();
}

async function loadTerrainSeed() {
    const response = await fetch("/api/terrain/seed");
    const payload = await response.json();
    terrainSeedElement.textContent = JSON.stringify(payload, null, 2);
}

function connectSimulationStream() {
    const eventSource = new EventSource("/api/simulation/stream");

    eventSource.addEventListener("tick", (event) => {
        streamStatusElement.textContent = event.data;
    });

    eventSource.onerror = () => {
        streamStatusElement.textContent = "Simulation stream disconnected.";
    };
}

document.getElementById("zoom-in").addEventListener("click", () => {
    zoom = Math.min(zoom + 0.1, 2);
    renderPlaceholder();
});

document.getElementById("zoom-out").addEventListener("click", () => {
    zoom = Math.max(zoom - 0.1, 0.5);
    renderPlaceholder();
});

renderPlaceholder();
loadTerrainSeed().catch((error) => {
    terrainSeedElement.textContent = error instanceof Error ? error.message : String(error);
});
connectSimulationStream();
