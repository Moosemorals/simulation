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

    public static bool Get(bool[] grid, int x, int y, int size) {
        return grid[Index(x, y, size)];
    }

    public static void Set(bool[] grid, int x, int y, int size, bool value) {
        grid[Index(x, y, size)] = value;
    }

    public static void EnforceToroidalSeams(float[] grid, int size) {
        for (int i = 0; i < size; i++) {
            Set(grid, i, size - 1, size, Get(grid, i, 0, size));
            Set(grid, size - 1, i, size, Get(grid, 0, i, size));
        }
    }

    public static void EnforceToroidalSeams(bool[] grid, int size) {
        for (int i = 0; i < size; i++) {
            Set(grid, i, size - 1, size, Get(grid, i, 0, size));
            Set(grid, size - 1, i, size, Get(grid, 0, i, size));
        }
    }
}