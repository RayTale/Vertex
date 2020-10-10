using Xunit;

namespace Vertex.Runtime.Core
{
    [CollectionDefinition(Name)]
    public class ProviderCollection : ICollectionFixture<ProviderFixture>
    {
        public const string Name = "ProviderCollection";
    }
}
