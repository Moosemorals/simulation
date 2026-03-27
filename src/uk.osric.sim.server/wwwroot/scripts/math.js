export function clamp(value, minValue, maxValue) {
    return Math.min(maxValue, Math.max(minValue, value));
}

export function easeInOutCubic(t) {
    return t < 0.5
        ? 4 * t * t * t
        : 1 - Math.pow((-2 * t) + 2, 3) / 2;
}

export function wrapCoordinate(value, size) {
    if (size <= 0) {
        return value;
    }

    return ((value % size) + size) % size;
}

export function interpolateWrapped(previous, current, alpha, size) {
    if (size <= 0) {
        return previous + ((current - previous) * alpha);
    }

    let delta = current - previous;
    const halfSize = size * 0.5;

    if (delta > halfSize) {
        delta -= size;
    } else if (delta < -halfSize) {
        delta += size;
    }

    return wrapCoordinate(previous + (delta * alpha), size);
}

export function shortestWrappedDelta(previous, current, size) {
    if (size <= 0) {
        return current - previous;
    }

    let delta = current - previous;
    const halfSize = size * 0.5;

    if (delta > halfSize) {
        delta -= size;
    } else if (delta < -halfSize) {
        delta += size;
    }

    return delta;
}

export function mat4Identity() {
    return new Float32Array([
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1,
    ]);
}

export function mat3Identity() {
    return new Float32Array([
        1, 0, 0,
        0, 1, 0,
        0, 0, 1,
    ]);
}

export function mat4IdentityInto(out) {
    out[0] = 1; out[1] = 0; out[2] = 0; out[3] = 0;
    out[4] = 0; out[5] = 1; out[6] = 0; out[7] = 0;
    out[8] = 0; out[9] = 0; out[10] = 1; out[11] = 0;
    out[12] = 0; out[13] = 0; out[14] = 0; out[15] = 1;
}

export function mat4Multiply(out, a, b) {
    const a00 = a[0]; const a01 = a[1]; const a02 = a[2]; const a03 = a[3];
    const a10 = a[4]; const a11 = a[5]; const a12 = a[6]; const a13 = a[7];
    const a20 = a[8]; const a21 = a[9]; const a22 = a[10]; const a23 = a[11];
    const a30 = a[12]; const a31 = a[13]; const a32 = a[14]; const a33 = a[15];

    let b0 = b[0]; let b1 = b[1]; let b2 = b[2]; let b3 = b[3];
    out[0] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[1] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[2] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[3] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);

    b0 = b[4]; b1 = b[5]; b2 = b[6]; b3 = b[7];
    out[4] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[5] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[6] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[7] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);

    b0 = b[8]; b1 = b[9]; b2 = b[10]; b3 = b[11];
    out[8] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[9] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[10] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[11] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);

    b0 = b[12]; b1 = b[13]; b2 = b[14]; b3 = b[15];
    out[12] = (b0 * a00) + (b1 * a10) + (b2 * a20) + (b3 * a30);
    out[13] = (b0 * a01) + (b1 * a11) + (b2 * a21) + (b3 * a31);
    out[14] = (b0 * a02) + (b1 * a12) + (b2 * a22) + (b3 * a32);
    out[15] = (b0 * a03) + (b1 * a13) + (b2 * a23) + (b3 * a33);
}

export function mat4Translate(matrix, x, y, z) {
    matrix[12] += x;
    matrix[13] += y;
    matrix[14] += z;
}

export function mat4Ortho(out, left, right, bottom, top, near, far) {
    const lr = 1 / (left - right);
    const bt = 1 / (bottom - top);
    const nf = 1 / (near - far);

    out[0] = -2 * lr;
    out[1] = 0;
    out[2] = 0;
    out[3] = 0;
    out[4] = 0;
    out[5] = -2 * bt;
    out[6] = 0;
    out[7] = 0;
    out[8] = 0;
    out[9] = 0;
    out[10] = 2 * nf;
    out[11] = 0;
    out[12] = (left + right) * lr;
    out[13] = (top + bottom) * bt;
    out[14] = (far + near) * nf;
    out[15] = 1;
}

export function mat4LookAt(out, eye, target, up) {
    let zx = eye[0] - target[0];
    let zy = eye[1] - target[1];
    let zz = eye[2] - target[2];
    let zLength = Math.hypot(zx, zy, zz);
    if (zLength === 0) {
        zz = 1;
        zLength = 1;
    }

    zx /= zLength;
    zy /= zLength;
    zz /= zLength;

    let xx = (up[1] * zz) - (up[2] * zy);
    let xy = (up[2] * zx) - (up[0] * zz);
    let xz = (up[0] * zy) - (up[1] * zx);
    let xLength = Math.hypot(xx, xy, xz);
    if (xLength === 0) {
        xx = 1;
        xLength = 1;
    }

    xx /= xLength;
    xy /= xLength;
    xz /= xLength;

    const yx = (zy * xz) - (zz * xy);
    const yy = (zz * xx) - (zx * xz);
    const yz = (zx * xy) - (zy * xx);

    out[0] = xx;
    out[1] = yx;
    out[2] = zx;
    out[3] = 0;
    out[4] = xy;
    out[5] = yy;
    out[6] = zy;
    out[7] = 0;
    out[8] = xz;
    out[9] = yz;
    out[10] = zz;
    out[11] = 0;
    out[12] = -((xx * eye[0]) + (xy * eye[1]) + (xz * eye[2]));
    out[13] = -((yx * eye[0]) + (yy * eye[1]) + (yz * eye[2]));
    out[14] = -((zx * eye[0]) + (zy * eye[1]) + (zz * eye[2]));
    out[15] = 1;
}

export function mat3FromMat4(out, matrix) {
    out[0] = matrix[0];
    out[1] = matrix[1];
    out[2] = matrix[2];
    out[3] = matrix[4];
    out[4] = matrix[5];
    out[5] = matrix[6];
    out[6] = matrix[8];
    out[7] = matrix[9];
    out[8] = matrix[10];
}

export function mat4Invert(out, matrix) {
    const a00 = matrix[0]; const a01 = matrix[1]; const a02 = matrix[2]; const a03 = matrix[3];
    const a10 = matrix[4]; const a11 = matrix[5]; const a12 = matrix[6]; const a13 = matrix[7];
    const a20 = matrix[8]; const a21 = matrix[9]; const a22 = matrix[10]; const a23 = matrix[11];
    const a30 = matrix[12]; const a31 = matrix[13]; const a32 = matrix[14]; const a33 = matrix[15];

    const b00 = (a00 * a11) - (a01 * a10);
    const b01 = (a00 * a12) - (a02 * a10);
    const b02 = (a00 * a13) - (a03 * a10);
    const b03 = (a01 * a12) - (a02 * a11);
    const b04 = (a01 * a13) - (a03 * a11);
    const b05 = (a02 * a13) - (a03 * a12);
    const b06 = (a20 * a31) - (a21 * a30);
    const b07 = (a20 * a32) - (a22 * a30);
    const b08 = (a20 * a33) - (a23 * a30);
    const b09 = (a21 * a32) - (a22 * a31);
    const b10 = (a21 * a33) - (a23 * a31);
    const b11 = (a22 * a33) - (a23 * a32);

    const determinant = (b00 * b11) - (b01 * b10) + (b02 * b09) + (b03 * b08) - (b04 * b07) + (b05 * b06);
    if (!determinant) {
        return false;
    }

    const inverseDet = 1 / determinant;
    out[0] = ((a11 * b11) - (a12 * b10) + (a13 * b09)) * inverseDet;
    out[1] = ((a02 * b10) - (a01 * b11) - (a03 * b09)) * inverseDet;
    out[2] = ((a31 * b05) - (a32 * b04) + (a33 * b03)) * inverseDet;
    out[3] = ((a22 * b04) - (a21 * b05) - (a23 * b03)) * inverseDet;
    out[4] = ((a12 * b08) - (a10 * b11) - (a13 * b07)) * inverseDet;
    out[5] = ((a00 * b11) - (a02 * b08) + (a03 * b07)) * inverseDet;
    out[6] = ((a32 * b02) - (a30 * b05) - (a33 * b01)) * inverseDet;
    out[7] = ((a20 * b05) - (a22 * b02) + (a23 * b01)) * inverseDet;
    out[8] = ((a10 * b10) - (a11 * b08) + (a13 * b06)) * inverseDet;
    out[9] = ((a01 * b08) - (a00 * b10) - (a03 * b06)) * inverseDet;
    out[10] = ((a30 * b04) - (a31 * b02) + (a33 * b00)) * inverseDet;
    out[11] = ((a21 * b02) - (a20 * b04) - (a23 * b00)) * inverseDet;
    out[12] = ((a11 * b07) - (a10 * b09) - (a12 * b06)) * inverseDet;
    out[13] = ((a00 * b09) - (a01 * b07) + (a02 * b06)) * inverseDet;
    out[14] = ((a31 * b01) - (a30 * b03) - (a32 * b00)) * inverseDet;
    out[15] = ((a20 * b03) - (a21 * b01) + (a22 * b00)) * inverseDet;
    return true;
}

export function vec4TransformMat4(vector, matrix) {
    const x = vector[0];
    const y = vector[1];
    const z = vector[2];
    const w = vector[3];
    return [
        (matrix[0] * x) + (matrix[4] * y) + (matrix[8] * z) + (matrix[12] * w),
        (matrix[1] * x) + (matrix[5] * y) + (matrix[9] * z) + (matrix[13] * w),
        (matrix[2] * x) + (matrix[6] * y) + (matrix[10] * z) + (matrix[14] * w),
        (matrix[3] * x) + (matrix[7] * y) + (matrix[11] * z) + (matrix[15] * w),
    ];
}