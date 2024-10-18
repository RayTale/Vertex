using Orleans;

namespace Vertex.Runtime.Options
{
    [GenerateSerializer]
    public class ActorOptions
    {
        /// <summary>
        /// Event Version interval of RayGrain saving snapshot
        /// </summary>
        [Id(0)]
        public int SnapshotVersionInterval { get; set; } = 500;

        /// <summary>
        /// The minimum event Version interval for saving snapshots when RayGrain is deactivated
        /// </summary>
        [Id(1)]
        public int MinSnapshotVersionInterval { get; set; } = 1;

        /// <summary>
        /// The amount of data read each time when reading events in batches
        /// </summary>
        [Id(2)]
        public int EventPageSize { get; set; } = 2000;
    }
}
