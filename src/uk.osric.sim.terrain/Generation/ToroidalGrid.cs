// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

internal static class ToroidalGrid {
    public static int Wrap(int coordinate, int size) {
        int wrapped = coordinate % size;
        if (wrapped < 0) {
            wrapped += size;
        }

        return wrapped;
    }

    public static int Index(int x, int y, int size) {
        return Wrap(y, size) * size + Wrap(x, size);
    }

    public static float Get(float[] grid, int x, int y, int size) {
        return grid[Index(x, y, size)];
    }

    public static void Set(float[] grid, int x, int y, int size, float value) {
        grid[Index(x, y, size)] = value;
    }
}