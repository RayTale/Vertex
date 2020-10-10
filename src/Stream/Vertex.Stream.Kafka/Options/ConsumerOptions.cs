namespace Vertex.Stream.Kafka.Options
{
    public class ConsumerOptions
    {

        /// <summary>
        /// 发生异常重试次数
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 重试间隔(ms)
        /// </summary>
        public int RetryIntervals { get; set; } = 1000;
    }
}
