---
agent: agent
description: "Use when working on the frontend module for uk.osric.sim.frontend, including HTML, CSS, vanilla JavaScript, WebGL rendering, camera controls, and client consumption of terrain and SSE updates."
---

Work only on the frontend module unless an API contract change is required from the server.

Context:
- Frontend source lives under `src/uk.osric.sim.frontend`.
- Use HTML, CSS, and vanilla JavaScript.
- Render through a WebGL canvas with a top-down camera.
- Consume terrain bootstrap data over HTTP and tick updates over SSE.

Priority order:
1. Establish the page shell and rendering surface.
2. Build input handling for pan and zoom.
3. Define a client-side terrain bootstrap flow.
4. Add a simulation overlay fed by SSE.
5. Keep the asset pipeline simple until the rendering model justifies more tooling.

Constraints:
- No framework-heavy client stack unless the team explicitly chooses one later.
- Keep assets easy for the server to host.
- Maintain clear separation between rendering code and server transport code.
