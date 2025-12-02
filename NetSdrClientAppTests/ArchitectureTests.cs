using NetArchTest.Rules;
using NUnit.Framework;
using System.Reflection;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class ArchitectureTests
    {
        private const string NetSdrClientNamespace = "NetSdrClientApp";
        private Assembly _assembly;

        [SetUp]
        public void Setup()
        {
            // Завантажуємо assembly NetSdrClientApp
            _assembly = Assembly.Load("NetSdrClientApp");
        }

        [Test]
        public void Networking_Layer_Should_Not_Depend_On_UI()
        {
            // Arrange & Act
            var result = Types.InAssembly(_assembly)
                .That()
                .ResideInNamespace($"{NetSdrClientNamespace}.Networking")
                .ShouldNot()
                .HaveDependencyOn($"{NetSdrClientNamespace}.UI")
                .GetResult();

            // Assert
            Assert.That(result.IsSuccessful, Is.True, 
                "Networking layer should not depend on UI layer");
        }

        [Test]
        public void Messages_Should_Not_Have_Dependencies_On_Networking()
        {
            // Arrange & Act
            var result = Types.InAssembly(_assembly)
                .That()
                .ResideInNamespace($"{NetSdrClientNamespace}.Messages")
                .ShouldNot()
                .HaveDependencyOn($"{NetSdrClientNamespace}.Networking")
                .GetResult();

            // Assert
            Assert.That(result.IsSuccessful, Is.True, 
                "Messages should not depend on Networking layer");
        }

        [Test]
        public void Networking_Classes_Should_Follow_Naming_Convention()
        {
            // Arrange & Act
            var result = Types.InAssembly(_assembly)
                .That()
                .ResideInNamespace($"{NetSdrClientNamespace}.Networking")
                .And()
                .AreClasses()
                .Should()
                .HaveNameEndingWith("Wrapper")
                .Or()
                .HaveNameEndingWith("Client")
                .Or()
                .BeInterfaces()
                .GetResult();

            // Assert
            Assert.That(result.IsSuccessful, Is.True, 
                $"All classes in Networking should follow naming convention (end with 'Wrapper' or 'Client' or be interfaces). " +
                $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
        }

        [Test]
        public void Interfaces_Should_Start_With_I()
        {
            // Arrange & Act
            var result = Types.InAssembly(_assembly)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            // Assert
            Assert.That(result.IsSuccessful, Is.True,
                $"All interfaces should start with 'I'. " +
                $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
        }
    }
}