using Xunit;

namespace Vertex.TxRuntime.Core
{
    [CollectionDefinition(Name)]
    public class ProviderCollection : ICollectionFixture<ProviderFixture>
    {
        public const string Name = "ProviderCollection";
    }
}
