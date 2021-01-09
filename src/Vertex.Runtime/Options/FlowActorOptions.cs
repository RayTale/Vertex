namespace Vertex.Runtime.Options
{
    public class FlowActorOptions
    {
        /// <summary>
        /// Event Version interval of FlowActor saving snapshot
        /// </summary>
        public int SnapshotVersionInterval { get; set; } = 500;

        /// <summary>
        /// The minimum event Version interval for saving snapshot when FlowActor is deactivated
        /// </summary>
        public int MinSnapshotVersionInterval { get; set; } = 1;

        /// <summary>
        /// The amount of data read each time when reading events in batches
        /// </summary>
        public int EventPageSize { get; set; } = 1000;

        public FlowInitType InitType { get; set; } = FlowInitType.FirstReceive;

        /// <summary>
        /// Enable snapshot caching
        /// </summary>
        public bool EnableSnapshotCache { get; set; }

        /// <summary>
        /// Event Version interval of FlowActor snapshot cache
        /// </summary>
        public int SnapshotCacheVersionInterval { get; set; }

        /// <summary>
        /// The minimum event Version interval for saving snapshot cache when FlowActor is deactivated
        /// </summary>
        public int MinSnapshotCacheVersionInterval { get; set; } = 1;
    }
}
