using System;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace NuciWeb.Steam.ArchitectureTests
{
    [TestFixture]
    public sealed class ProductionArchitectureTests
    {
        static readonly Architecture Architecture = new ArchLoader()
            .LoadAssemblies(typeof(NuciWeb.Steam.ISteamProcessor).Assembly)
            .Build();

        [Test]
        public void ProductionAssembly_IsExplicitlyLoaded()
        {
            var assemblies = Architecture.Assemblies.ToArray();

            Assert.That(assemblies, Has.Length.EqualTo(1));
            Assert.That(assemblies[0].Name, Is.EqualTo("NuciWeb.Steam"));
        }

        [Test]
        public void PublicContracts_DoNotExposeAutomationTypes()
        {
            var contractTypes = Architecture.Types
                .Where(type => type.FullName.StartsWith("NuciWeb.Steam.ISteamProcessor", StringComparison.Ordinal)
                    || type.FullName.StartsWith("NuciWeb.Steam.Models", StringComparison.Ordinal)
                    || type.FullName.StartsWith("NuciWeb.Steam.KeyActivation", StringComparison.Ordinal))
                .ToArray();

            Assert.That(contractTypes, Is.Not.Empty);
            var automationTypes = contractTypes
                .Where(type => type.Dependencies.Any(dependency =>
                    dependency.Target.FullName.StartsWith("NuciWeb.Automation", StringComparison.Ordinal)))
                .ToArray();

            Assert.That(automationTypes, Is.Empty, "Public contracts must not depend on NuciWeb automation types.");
        }

        [Test]
        public void Utilities_DoNotDependOnExternalAssemblies()
        {
            var utilities = Types().That().ResideInNamespace("NuciWeb.Steam.Utils");

            Assert.That(utilities.GetObjects(Architecture), Is.Not.Empty);
            utilities.Should().NotDependOnAny(Types().That().ResideInNamespace("NuciWeb.Automation"));
        }

        [Test]
        public void Processors_DoNotDependOnPublicApiImplementations()
        {
            var processors = Types().That().ResideInNamespace("NuciWeb.Steam.Processors");

            Assert.That(processors.GetObjects(Architecture), Is.Not.Empty);
            processors.Should().NotDependOnAny(Types().That().ResideInNamespace("NuciWeb.Steam.Models"));
        }
    }
}
