namespace Vertex.Runtime.Options
{
    public class ArchiveOptions
    {
        /// <summary>
        /// The minimum number of seconds between archiving (default 1 days)
        /// </summary>
        public long MinIntervalSeconds { get; set; } = 24 * 60 * 60;

        public int EventPageSize { get; set; } = 3000;

        /// <summary>
        /// The maximum number of seconds between archiving, as long as the interval is greater than this value, you can archive (default 7 days)
        /// </summary>
        public long RetainSeconds { get; set; } = 24 * 60 * 60 * 7;
    }
}
