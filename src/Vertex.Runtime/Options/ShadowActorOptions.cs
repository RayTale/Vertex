namespace Vertex.Runtime.Options
{
    public class ShadowActorOptions
    {
        /// <summary>
        /// The amount of data read each time when reading events in batches
        /// </summary>
        public int EventPageSize { get; set; } = 1000;
    }
}
