// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.simulation.Ecs;

internal sealed class EntityStorage {
    private interface IComponentStore {
        void Remove(int entityId);
    }

    private sealed class ComponentStore<T> : IComponentStore where T : struct {
        private readonly Dictionary<int, T> data = [];

        internal void Set(int id, T value) => data[id] = value;
        internal bool TryGet(int id, out T value) => data.TryGetValue(id, out value);
        internal bool Contains(int id) => data.ContainsKey(id);
        public void Remove(int id) => data.Remove(id);
        internal Dictionary<int, T> All => data;
    }

    private int nextId;
    private readonly Dictionary<Type, IComponentStore> stores = [];

    private ComponentStore<T> StoreFor<T>() where T : struct {
        var type = typeof(T);
        if (!stores.TryGetValue(type, out var store)) {
            var newStore = new ComponentStore<T>();
            stores[type] = newStore;
            return newStore;
        }
        return (ComponentStore<T>)store;
    }

    private bool TryGetStore<T>(out ComponentStore<T> store) where T : struct {
        if (stores.TryGetValue(typeof(T), out var raw)) {
            store = (ComponentStore<T>)raw;
            return true;
        }
        store = null!;
        return false;
    }

    internal EntityId CreateEntity() => new(nextId++);

    internal void DestroyEntity(EntityId id) {
        foreach (var store in stores.Values) {
            store.Remove(id.Value);
        }
    }

    internal void Set<T>(EntityId id, T value) where T : struct {
        StoreFor<T>().Set(id.Value, value);
    }

    internal bool Has<T>(EntityId id) where T : struct {
        return TryGetStore<T>(out var store) && store.Contains(id.Value);
    }

    internal T Get<T>(EntityId id) where T : struct {
        if (TryGetStore<T>(out var store) && store.TryGet(id.Value, out var value)) {
            return value;
        }
        throw new InvalidOperationException($"Entity {id.Value} does not have component {typeof(T).Name}");
    }

    internal IEnumerable<(EntityId, T1, T2)> Query<T1, T2>()
        where T1 : struct
        where T2 : struct {
        if (!TryGetStore<T1>(out var s1) || !TryGetStore<T2>(out var s2)) {
            yield break;
        }
        foreach (var kvp in s1.All) {
            if (s2.TryGet(kvp.Key, out var v2)) {
                yield return (new EntityId(kvp.Key), kvp.Value, v2);
            }
        }
    }

    internal IEnumerable<(EntityId, T1, T2, T3)> Query<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct {
        if (!TryGetStore<T1>(out var s1) || !TryGetStore<T2>(out var s2) || !TryGetStore<T3>(out var s3)) {
            yield break;
        }
        foreach (var kvp in s1.All) {
            if (s2.TryGet(kvp.Key, out var v2) && s3.TryGet(kvp.Key, out var v3)) {
                yield return (new EntityId(kvp.Key), kvp.Value, v2, v3);
            }
        }
    }
}
