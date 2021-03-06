﻿using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Vertex.Abstractions.InnerService
{
    public interface IDIDActor : IGrainWithStringKey
    {
        /// <summary>
        /// 通过utc时间生成分布式唯一id
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AlwaysInterleave]
        Task<long> NewID();
    }
}
