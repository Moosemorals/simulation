// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Collections.Concurrent;
using System.Threading.Channels;

using uk.osric.sim.contracts.Simulation;

namespace uk.osric.sim.server.Simulation;

public sealed class SimulationTickBroadcaster {
    private readonly ConcurrentDictionary<int, Channel<SimulationTickDto>> subscribers = [];
    private int nextSubscriberId;

    internal (ChannelReader<SimulationTickDto> Reader, IDisposable Subscription) Subscribe() {
        int subscriberId = Interlocked.Increment(ref nextSubscriberId);
        var channel = Channel.CreateUnbounded<SimulationTickDto>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
        });

        subscribers[subscriberId] = channel;
        IDisposable subscription = new Subscription(this, subscriberId, channel);
        return (channel.Reader, subscription);
    }

    internal void Publish(SimulationTickDto tick) {
        foreach (var (_, channel) in subscribers) {
            channel.Writer.TryWrite(tick);
        }
    }

    private void Unsubscribe(int subscriberId, Channel<SimulationTickDto> channel) {
        if (subscribers.TryRemove(subscriberId, out _)) {
            channel.Writer.TryComplete();
        }
    }

    private sealed class Subscription : IDisposable {
        private readonly SimulationTickBroadcaster broadcaster;
        private readonly int subscriberId;
        private readonly Channel<SimulationTickDto> channel;
        private int disposed;

        internal Subscription(SimulationTickBroadcaster broadcaster, int subscriberId, Channel<SimulationTickDto> channel) {
            this.broadcaster = broadcaster;
            this.subscriberId = subscriberId;
            this.channel = channel;
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref disposed, 1) != 0) {
                return;
            }

            broadcaster.Unsubscribe(subscriberId, channel);
        }
    }
}
