import { gl, state } from "./state.js";

function compileShader(kind, source) {
    const shader = gl.createShader(kind);

    if (shader === null) {
        throw new Error("Failed to allocate shader object.");
    }

    gl.shaderSource(shader, source);
    gl.compileShader(shader);

    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        const infoLog = gl.getShaderInfoLog(shader);
        gl.deleteShader(shader);
        throw new Error(`Shader compilation failed: ${infoLog ?? "unknown error"}`);
    }

    return shader;
}

function createProgram(vertexSource, fragmentSource) {
    const vertexShader = compileShader(gl.VERTEX_SHADER, vertexSource);
    const fragmentShader = compileShader(gl.FRAGMENT_SHADER, fragmentSource);
    const program = gl.createProgram();

    if (program === null) {
        gl.deleteShader(vertexShader);
        gl.deleteShader(fragmentShader);
        throw new Error("Failed to allocate shader program.");
    }

    gl.attachShader(program, vertexShader);
    gl.attachShader(program, fragmentShader);
    gl.linkProgram(program);
    gl.deleteShader(vertexShader);
    gl.deleteShader(fragmentShader);

    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        const infoLog = gl.getProgramInfoLog(program);
        gl.deleteProgram(program);
        throw new Error(`Program linking failed: ${infoLog ?? "unknown error"}`);
    }

    return program;
}

function createTerrainProgram() {
    const vertexSource = `#version 300 es
precision highp float;

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uViewProjection;
uniform mat3 uNormalMatrix;

out vec3 vWorldPosition;
out vec3 vNormal;

void main() {
    vec4 worldPosition = uModel * vec4(aPosition, 1.0);
    vWorldPosition = worldPosition.xyz;
    vNormal = normalize(uNormalMatrix * aNormal);
    gl_Position = uViewProjection * worldPosition;
}
`;

    const fragmentSource = `#version 300 es
precision highp float;

in vec3 vWorldPosition;
in vec3 vNormal;

uniform vec3 uLightDirection;
uniform float uElevationScale;

out vec4 outColor;

vec3 terrainRamp(float h) {
    vec3 low = vec3(0.18, 0.30, 0.23);
    vec3 mid = vec3(0.42, 0.53, 0.37);
    vec3 high = vec3(0.67, 0.64, 0.54);
    vec3 peak = vec3(0.85, 0.84, 0.80);

    float t = clamp(h, 0.0, 1.0);
    if (t < 0.45) {
        return mix(low, mid, t / 0.45);
    }

    if (t < 0.78) {
        return mix(mid, high, (t - 0.45) / 0.33);
    }

    return mix(high, peak, (t - 0.78) / 0.22);
}

void main() {
    float height01 = clamp(vWorldPosition.y / uElevationScale, 0.0, 1.0);
    vec3 albedo = terrainRamp(height01);
    vec3 normal = normalize(vNormal);
    float diffuse = max(dot(normal, -normalize(uLightDirection)), 0.0);
    float ambient = 0.35;
    float lighting = ambient + (0.65 * diffuse);
    outColor = vec4(albedo * lighting, 1.0);
}
`;

    const program = createProgram(vertexSource, fragmentSource);
    return {
        program,
        locations: {
            model: gl.getUniformLocation(program, "uModel"),
            viewProjection: gl.getUniformLocation(program, "uViewProjection"),
            normalMatrix: gl.getUniformLocation(program, "uNormalMatrix"),
            lightDirection: gl.getUniformLocation(program, "uLightDirection"),
            elevationScale: gl.getUniformLocation(program, "uElevationScale"),
        },
    };
}

function createActorProgram() {
    const vertexSource = `#version 300 es
precision highp float;

layout(location = 0) in vec3 aCenter;
layout(location = 1) in vec2 aOffset;
layout(location = 2) in vec3 aForward;
layout(location = 3) in vec3 aRight;

uniform mat4 uModel;
uniform mat4 uViewProjection;

uniform float uHalfLength;
uniform float uHalfWidth;

out vec2 vOffset;

void main() {
    vec3 world = aCenter + (aForward * (aOffset.x * uHalfLength)) + (aRight * (aOffset.y * uHalfWidth));
    vec4 worldPosition = uModel * vec4(world, 1.0);
    gl_Position = uViewProjection * worldPosition;
    vOffset = aOffset;
}
`;

    const fragmentSource = `#version 300 es
precision highp float;

in vec2 vOffset;

out vec4 outColor;

void main() {
    float radiusSq = dot(vOffset, vOffset);
    if (radiusSq > 1.0) {
        discard;
    }

    float edge = smoothstep(0.78, 1.0, radiusSq);
    float nose = smoothstep(0.45, 1.0, vOffset.x) * (1.0 - smoothstep(0.35, 1.0, abs(vOffset.y)));
    vec3 fill = vec3(0.96, 0.94, 0.90);
    vec3 border = vec3(0.12, 0.10, 0.08);
    vec3 base = mix(fill, border, edge);
    outColor = vec4(mix(base, border, nose * 0.55), 1.0);
}
`;

    const program = createProgram(vertexSource, fragmentSource);
    return {
        program,
        locations: {
            model: gl.getUniformLocation(program, "uModel"),
            viewProjection: gl.getUniformLocation(program, "uViewProjection"),
            halfLength: gl.getUniformLocation(program, "uHalfLength"),
            halfWidth: gl.getUniformLocation(program, "uHalfWidth"),
        },
    };
}

export function initializePrograms() {
    if (state.terrainProgram !== null && state.actorProgram !== null) {
        return;
    }

    state.terrainProgram = createTerrainProgram();
    state.actorProgram = createActorProgram();

    gl.enable(gl.DEPTH_TEST);
    gl.enable(gl.CULL_FACE);
    gl.cullFace(gl.BACK);
    gl.clearColor(0.18, 0.23, 0.20, 1);
}

export function resetProgramState() {
    state.terrainProgram = null;
    state.actorProgram = null;
    state.terrainVertexArray = null;
    state.terrainVertexBuffer = null;
    state.terrainIndexBuffer = null;
    state.actorVertexArray = null;
    state.actorBuffer = null;
}