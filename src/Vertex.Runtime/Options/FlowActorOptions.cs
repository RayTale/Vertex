using Orleans;

namespace Vertex.Runtime.Options
{
    [GenerateSerializer]
    public class FlowActorOptions
    {
        /// <summary>
        /// Event Version interval of FlowActor saving snapshot
        /// </summary>
        [Id(0)]
        public int SnapshotVersionInterval { get; set; } = 500;

        /// <summary>
        /// The minimum event Version interval for saving snapshot when FlowActor is deactivated
        /// </summary>
        [Id(1)]
        public int MinSnapshotVersionInterval { get; set; } = 1;

        /// <summary>
        /// The amount of data read each time when reading events in batches
        /// </summary>
        [Id(2)]
        public int EventPageSize { get; set; } = 1000;

        [Id(3)]
        public FlowInitType InitType { get; set; } = FlowInitType.FirstReceive;

        /// <summary>
        /// Enable snapshot caching
        /// </summary>
        [Id(4)]
        public bool EnableSnapshotCache { get; set; }

        /// <summary>
        /// Event Version interval of FlowActor snapshot cache
        /// </summary>
        [Id(5)]
        public int SnapshotCacheVersionInterval { get; set; }

        /// <summary>
        /// The minimum event Version interval for saving snapshot cache when FlowActor is deactivated
        /// </summary>
        [Id(6)]
        public int MinSnapshotCacheVersionInterval { get; set; } = 1;
    }
}
