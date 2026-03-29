// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain;

public sealed class SettlementCandidate {
    public required int X { get; init; }

    public required int Y { get; init; }

    public required float Score { get; init; }
}