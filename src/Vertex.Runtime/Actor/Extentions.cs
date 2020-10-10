using Orleans.Runtime;
using System;
using System.Runtime.CompilerServices;
using Vertex.Abstractions.Snapshot;
using Vertex.Runtime.Exceptions;
using Vertext.Abstractions.Event;

namespace Vertex.Runtime.Actor
{
    public static class Extentions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateVersion<PrimaryKey>(this SnapshotMeta<PrimaryKey> snapshotMeta, EventMeta eventMeta, Type grainType)
        {
            if (snapshotMeta.Version + 1 != eventMeta.Version)
            {
                throw new EventVersionException(snapshotMeta.ActorId.ToString(), grainType, eventMeta.Version, snapshotMeta.Version);
            }

            snapshotMeta.Version = eventMeta.Version;

            if (snapshotMeta.MinEventVersion == 0)
                snapshotMeta.MinEventVersion = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForceUpdateVersion<PrimaryKey>(this SnapshotMeta<PrimaryKey> snapshotMeta, EventMeta eventMeta, Type grainType)
        {
            if (snapshotMeta.Version + 1 != eventMeta.Version)
            {
                throw new EventVersionException(snapshotMeta.ActorId.ToString(), grainType, eventMeta.Version, snapshotMeta.Version);
            }

            snapshotMeta.DoingVersion = eventMeta.Version;
            snapshotMeta.Version = eventMeta.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementDoingVersion<PrimaryKey>(this SnapshotMeta<PrimaryKey> snapshotMeta, Type grainType)
        {
            if (snapshotMeta.DoingVersion != snapshotMeta.Version)
            {
                throw new SnapshotException(snapshotMeta.ActorId.ToString(), grainType, snapshotMeta.DoingVersion, snapshotMeta.Version);
            }
            snapshotMeta.DoingVersion += 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecrementDoingVersion<PrimaryKey>(this SnapshotMeta<PrimaryKey> snapshotMeta) => snapshotMeta.DoingVersion -= 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetEventId<PrimaryKey>(this EventUnit<PrimaryKey> @event)
        {
            return $"{@event.ActorId}_{@event.Meta.Version}";
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsafeUpdateVersion<PrimaryKey>(this SubSnapshot<PrimaryKey> snapshot, EventMeta eventBase)
        {
            snapshot.DoingVersion = eventBase.Version;
            snapshot.Version = eventBase.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementDoingVersion<PrimaryKey>(this SubSnapshot<PrimaryKey> state, Type grainType)
        {
            if (state.DoingVersion != state.Version)
            {
                throw new SnapshotException(state.ActorId.ToString(), grainType, state.DoingVersion, state.Version);
            }

            state.DoingVersion++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateVersion<PrimaryKey>(this SubSnapshot<PrimaryKey> snapshot, EventMeta eventBase, Type grainType)
        {
            if (snapshot.Version + 1 != eventBase.Version)
            {
                throw new EventVersionException(snapshot.ActorId.ToString(), grainType, eventBase.Version, snapshot.Version);
            }

            snapshot.Version = eventBase.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FullUpdateVersion<PrimaryKey>(this SubSnapshot<PrimaryKey> snapshot, EventMeta eventBase, Type grainType)
        {
            if (snapshot.Version > 0 && snapshot.Version + 1 != eventBase.Version)
            {
                throw new EventVersionException(snapshot.ActorId.ToString(), grainType, eventBase.Version, snapshot.Version);
            }

            snapshot.DoingVersion = eventBase.Version;
            snapshot.Version = eventBase.Version;
        }
    }
}
