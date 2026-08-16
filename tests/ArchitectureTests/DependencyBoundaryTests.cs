using System;
using System.Linq;
using TradeCopia.Contracts;
using TradeCopia.Domain.Engine;
using TradeCopia.Native.Adapter;
using TradeCopia.Protocol;

namespace TradeCopia.ArchitectureTests
{
    public class DependencyBoundaryTests
    {
        [Theory]
        [InlineData(typeof(CopyCoordinator))]
        [InlineData(typeof(EngineStatusDto))]
        [InlineData(typeof(ProtocolFraming))]
        [InlineData(typeof(DisabledOrderExecutor))]
        public void Shared_assemblies_do_not_reference_forbidden_stacks(Type type)
        {
            var names = type.Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(names, n => n.StartsWith("NinjaTrader", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.IndexOf("AspNetCore", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.DoesNotContain(names, n => n.IndexOf("Sqlite", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.DoesNotContain(names, n => n.IndexOf("EntityFramework", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
