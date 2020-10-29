using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vertex.Stream.Common
{
    public interface IStreamSubHandler
    {
        Task EventHandler(Type observerType, BytesBox bytes);

        Task EventHandler(Type observerType, List<BytesBox> list);
    }
}
