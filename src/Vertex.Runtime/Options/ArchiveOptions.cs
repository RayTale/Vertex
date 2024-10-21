using Orleans;

namespace Vertex.Runtime.Options
{
    [GenerateSerializer]
    public class ArchiveOptions
    {
        /// <summary>
        /// The minimum number of seconds between archiving (default 1 days)
        /// </summary>
        [Id(0)]
        public long MinIntervalSeconds { get; set; } = 24 * 60 * 60;

        [Id(1)]
        public int EventPageSize { get; set; } = 3000;

        /// <summary>
        /// The maximum number of seconds between archiving, as long as the interval is greater than this value, you can archive (default 7 days)
        /// </summary>
        [Id(2)]
        public long RetainSeconds { get; set; } = 24 * 60 * 60 * 7;
    }
}
