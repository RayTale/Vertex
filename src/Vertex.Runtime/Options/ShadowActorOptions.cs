using Orleans;

namespace Vertex.Runtime.Options
{
    [GenerateSerializer]
    public class ShadowActorOptions
    {
        /// <summary>
        /// The amount of data read each time when reading events in batches
        /// </summary>
        [Id(0)]
        public int EventPageSize { get; set; } = 1000;
    }
}
