namespace Vertex.Runtime.Options
{
    public class SubActorOptions
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
        public int EventPageSize { get; set; } = 1000;
        public SubInitType InitType { get; set; } = SubInitType.FirstReceive;

    }
    public enum SubInitType : byte
    {
        None = 0,
        /// <summary>
        /// Recover from event with version 0
        /// </summary>
        ZeroVersion = 1,
        /// <summary>
        /// Recover from the first received version of the event
        /// </summary>
        FirstReceive = 2
    }
}
