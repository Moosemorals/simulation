import {
    panByViewportRatio,
    resetView,
    screenToGround,
    updateCameraMatrices,
    zoomAtCanvasPoint,
    normalizeViewCenter,
} from "./camera.js";
import { canvas, state } from "./state.js";
import { resizeCanvasToDisplaySize } from "./render.js";

function eventToCanvasPoint(event) {
    const rect = canvas.getBoundingClientRect();
    return {
        x: (event.clientX - rect.left) * (canvas.width / rect.width),
        y: (event.clientY - rect.top) * (canvas.height / rect.height),
    };
}

function endDrag() {
    state.isDragging = false;
    state.lastDragPoint = null;
    canvas.classList.remove("is-dragging");
}

export function registerInputHandlers() {
    canvas.addEventListener("wheel", (event) => {
        event.preventDefault();

        const point = eventToCanvasPoint(event);
        const zoomFactor = Math.exp(event.deltaY * -0.0015);
        zoomAtCanvasPoint(state.zoom * zoomFactor, point.x, point.y);
    }, { passive: false });

    canvas.addEventListener("mousedown", (event) => {
        if (event.button !== 0) {
            return;
        }

        state.isDragging = true;
        state.lastDragPoint = eventToCanvasPoint(event);
        canvas.classList.add("is-dragging");
    });

    canvas.addEventListener("mousemove", (event) => {
        if (!state.isDragging || state.lastDragPoint === null || state.terrainSize <= 0) {
            return;
        }

        const point = eventToCanvasPoint(event);
        updateCameraMatrices();
        const previousWorld = screenToGround(state.lastDragPoint.x, state.lastDragPoint.y);
        const currentWorld = screenToGround(point.x, point.y);

        state.viewCenterX -= currentWorld.x - previousWorld.x;
        state.viewCenterY -= currentWorld.y - previousWorld.y;
        state.lastDragPoint = point;
        normalizeViewCenter();
    });

    canvas.addEventListener("mouseup", endDrag);
    canvas.addEventListener("mouseleave", endDrag);
    canvas.addEventListener("blur", endDrag);

    document.getElementById("zoom-in").addEventListener("click", () => {
        zoomAtCanvasPoint(state.zoom * 1.1, canvas.width * 0.5, canvas.height * 0.5);
    });

    document.getElementById("zoom-out").addEventListener("click", () => {
        zoomAtCanvasPoint(state.zoom / 1.1, canvas.width * 0.5, canvas.height * 0.5);
    });

    document.getElementById("pan-up").addEventListener("click", () => panByViewportRatio(0, -0.1));
    document.getElementById("pan-down").addEventListener("click", () => panByViewportRatio(0, 0.1));
    document.getElementById("pan-left").addEventListener("click", () => panByViewportRatio(-0.1, 0));
    document.getElementById("pan-right").addEventListener("click", () => panByViewportRatio(0.1, 0));
    document.getElementById("reset-view").addEventListener("click", resetView);

    window.addEventListener("resize", () => {
        resizeCanvasToDisplaySize();
    });
}