namespace Vertex.Runtime.Options
{
    public enum FlowInitType : byte
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
