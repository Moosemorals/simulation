// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Runtime.CompilerServices;

namespace uk.osric.sim.terrain.Generation;

internal sealed class HydraulicErosionLayer {

    public float[] Apply(float[] heightData, int size, int erosionPasses, HydraulicErosionTuning tuning) {
        int cellCount = heightData.Length;
        WrappedCoordinateLookup wrappedCoordinates = new(size);
        int[] downhillIndices = new int[cellCount];
        int[] upstreamCounts = new int[cellCount];
        int[] processingQueue = new int[cellCount];
        float[] finalAccumulation = new float[cellCount];
        float[] delta = new float[cellCount];

        bool includeDiagonalNeighbors = tuning.NeighborSampleCount == 8;

        ComputeDownhillIndices(heightData, size, wrappedCoordinates, downhillIndices, includeDiagonalNeighbors);
        BuildFlowAccumulation(downhillIndices, upstreamCounts, processingQueue, finalAccumulation, tuning.BaseFlow);

        for (int pass = 0; pass < erosionPasses; pass++) {
            ErodeAndDeposit(heightData, finalAccumulation, downhillIndices, delta, tuning);
            EnforceToroidalSeams(heightData, size);

            if ((pass + 1) % tuning.TopologyRefreshInterval == 0) {
                ComputeDownhillIndices(heightData, size, wrappedCoordinates, downhillIndices, includeDiagonalNeighbors);
                BuildFlowAccumulation(downhillIndices, upstreamCounts, processingQueue, finalAccumulation, tuning.BaseFlow);
            }
        }

        Normalize(heightData);
        NormalizeToMax(finalAccumulation);
        EnforceToroidalSeams(finalAccumulation, size);
        return finalAccumulation;
    }

    private static void ComputeDownhillIndices(float[] heightData, int size, WrappedCoordinateLookup wrappedCoordinates, int[] downhillIndices, bool includeDiagonalNeighbors) {
        int[] previous = wrappedCoordinates.Previous;
        int[] next = wrappedCoordinates.Next;

        for (int y = 0; y < size; y++) {
            int upRow = previous[y] * size;
            int currentRow = y * size;
            int downRow = next[y] * size;

            for (int x = 0; x < size; x++) {
                int currentIndex = currentRow + x;
                float currentHeight = heightData[currentIndex];
                int left = previous[x];
                int right = next[x];
                int bestIndex = -1;
                float bestDrop = 0.0f;

                // Cardinal directions only (N, W, E, S) to reduce neighbor checks from 8 to 4
                ConsiderNeighbor(heightData, upRow + x, currentHeight, ref bestDrop, ref bestIndex);
                ConsiderNeighbor(heightData, currentRow + left, currentHeight, ref bestDrop, ref bestIndex);
                ConsiderNeighbor(heightData, currentRow + right, currentHeight, ref bestDrop, ref bestIndex);
                ConsiderNeighbor(heightData, downRow + x, currentHeight, ref bestDrop, ref bestIndex);

                if (includeDiagonalNeighbors) {
                    ConsiderNeighbor(heightData, upRow + left, currentHeight, ref bestDrop, ref bestIndex);
                    ConsiderNeighbor(heightData, upRow + right, currentHeight, ref bestDrop, ref bestIndex);
                    ConsiderNeighbor(heightData, downRow + left, currentHeight, ref bestDrop, ref bestIndex);
                    ConsiderNeighbor(heightData, downRow + right, currentHeight, ref bestDrop, ref bestIndex);
                }

                downhillIndices[currentIndex] = bestIndex;
            }
        }
    }

    private static void BuildFlowAccumulation(int[] downhillIndices, int[] upstreamCounts, int[] processingQueue, float[] accumulation, float baseFlow) {
        Array.Fill(accumulation, baseFlow);
        Array.Clear(upstreamCounts);

        for (int i = 0; i < downhillIndices.Length; i++) {
            int downhillIndex = downhillIndices[i];
            if (downhillIndex >= 0) {
                upstreamCounts[downhillIndex]++;
            }
        }

        int queueLength = 0;
        for (int i = 0; i < downhillIndices.Length; i++) {
            if (upstreamCounts[i] == 0) {
                processingQueue[queueLength++] = i;
            }
        }

        for (int queueIndex = 0; queueIndex < queueLength; queueIndex++) {
            int currentIndex = processingQueue[queueIndex];
            int downhillIndex = downhillIndices[currentIndex];
            if (downhillIndex < 0) {
                continue;
            }

            accumulation[downhillIndex] += accumulation[currentIndex];
            upstreamCounts[downhillIndex]--;

            if (upstreamCounts[downhillIndex] == 0) {
                processingQueue[queueLength++] = downhillIndex;
            }
        }
    }

    private static void ErodeAndDeposit(float[] heightData, float[] accumulation, int[] downhillIndices, float[] delta, HydraulicErosionTuning tuning) {
        Array.Clear(delta);
        float inverseCellCount = 1.0f / heightData.Length;

        for (int i = 0; i < heightData.Length; i++) {
            int downhillIndex = downhillIndices[i];
            if (downhillIndex < 0) {
                continue;
            }

            float slope = heightData[i] - heightData[downhillIndex];
            if (slope <= 0.0f) {
                continue;
            }

            float flowFactor = accumulation[i] * inverseCellCount;
            float erosion = MathF.Min(heightData[i] * tuning.ErosionCapFactor, slope * flowFactor * tuning.SlopeFlowFactor);
            float deposition = erosion * tuning.DepositionRatio;

            delta[i] -= erosion;
            delta[downhillIndex] += deposition;
        }

        for (int i = 0; i < heightData.Length; i++) {
            heightData[i] += delta[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConsiderNeighbor(float[] heightData, int neighborIndex, float currentHeight, ref float bestDrop, ref int bestIndex) {
        float drop = currentHeight - heightData[neighborIndex];
        if (drop > bestDrop) {
            bestDrop = drop;
            bestIndex = neighborIndex;
        }
    }

    private sealed class WrappedCoordinateLookup {
        public WrappedCoordinateLookup(int size) {
            Previous = new int[size];
            Next = new int[size];

            for (int i = 0; i < size; i++) {
                Previous[i] = i == 0 ? size - 1 : i - 1;
                Next[i] = i == size - 1 ? 0 : i + 1;
            }
        }

        public int[] Previous { get; }

        public int[] Next { get; }
    }

    private static void EnforceToroidalSeams(float[] grid, int size) {
        int lastRowStart = (size - 1) * size;

        for (int i = 0; i < size; i++) {
            float topValue = grid[i];
            grid[lastRowStart + i] = topValue;

            float leftValue = grid[i * size];
            grid[i * size + (size - 1)] = leftValue;
        }
    }

    private static void Normalize(float[] values) {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < values.Length; i++) {
            float value = values[i];
            if (value < min) {
                min = value;
            }

            if (value > max) {
                max = value;
            }
        }

        float range = max - min;
        if (range <= 0.0f) {
            for (int i = 0; i < values.Length; i++) {
                values[i] = 0.5f;
            }

            return;
        }

        for (int i = 0; i < values.Length; i++) {
            values[i] = (values[i] - min) / range;
        }
    }

    private static void NormalizeToMax(float[] values) {
        float max = 0.0f;
        for (int i = 0; i < values.Length; i++) {
            if (values[i] > max) {
                max = values[i];
            }
        }

        if (max <= 0.0f) {
            return;
        }

        for (int i = 0; i < values.Length; i++) {
            values[i] /= max;
        }
    }
}
