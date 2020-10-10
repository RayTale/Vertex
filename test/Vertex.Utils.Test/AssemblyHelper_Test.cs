using Xunit;

namespace Vertex.Utils.Test
{
    public class AssemblyHelper_Test
    {
        [Fact]
        public void GetAssemblies()
        {
            var assemblies = AssemblyHelper.GetAssemblies();
            Assert.Contains(typeof(AssemblyHelper_Test).Assembly, assemblies);
            Assert.Contains(typeof(AssemblyHelper).Assembly, assemblies);
        }
    }
}
