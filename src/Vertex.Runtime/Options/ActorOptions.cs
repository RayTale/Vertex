namespace Vertex.Runtime.Options
{
    public class ActorOptions
    {
        /// <summary>
        /// Event Version interval of RayGrain saving snapshot
        /// </summary>
        public int SnapshotVersionInterval { get; set; } = 500;

        /// <summary>
        /// The minimum event Version interval for saving snapshots when RayGrain is deactivated
        /// </summary>
        public int MinSnapshotVersionInterval { get; set; } = 1;

        /// <summary>
        /// The amount of data read each time when reading events in batches
        /// </summary>
        public int EventPageSize { get; set; } = 2000;
    }
}
