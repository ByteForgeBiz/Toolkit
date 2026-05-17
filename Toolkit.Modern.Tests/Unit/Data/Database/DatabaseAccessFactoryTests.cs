using AwesomeAssertions;
using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Tests.Helpers;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Data")]
    public class DatabaseAccessFactoryTests
    {
        /// <summary>
        /// Verifies that the factory returns DBAccess even when the legacy DBAccess2 flag is requested.
        /// </summary>
        [TestMethod]
        public void Create_WithUseDBAccess2Flag_ShouldReturnDBAccess()
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();

            var result = DatabaseAccessFactory.Create(options, useDBAccess2: true);

            result.Should().BeOfType<DBAccess>();
            result.Options.Should().BeSameAs(options);
        }

        /// <summary>
        /// Verifies that the obsolete modern factory shape remains a DBAccess compatibility shim.
        /// </summary>
        [TestMethod]
        public void CreateModern_ShouldReturnDBAccessCompatibilityShim()
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();

#pragma warning disable CS0618
            var result = DatabaseAccessFactory.CreateModern(options);
#pragma warning restore CS0618

            result.Should().BeOfType<DBAccess>();
            result.Options.Should().BeSameAs(options);
        }

        /// <summary>
        /// Verifies that null options are rejected consistently.
        /// </summary>
        [TestMethod]
        public void Create_WithNullOptions_ShouldThrowArgumentNullException()
        {
            Action action = () => DatabaseAccessFactory.Create((DatabaseOptions)null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("options");
        }

        /// <summary>
        /// Verifies that DBAccess can be consumed through the compatibility interface.
        /// </summary>
        [TestMethod]
        public void DBAccess_ShouldImplementIDatabaseAccess()
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();

            var result = new DBAccess(options);

            result.Should().BeAssignableTo<IDatabaseAccess>();
            ((IDatabaseAccess)result).ConnectionString.Should().Be(options.GetConnectionString());
        }

        /// <summary>
        /// Verifies that cancellation-token compatibility overloads honor pre-canceled tokens.
        /// </summary>
        [TestMethod]
        public void CompatibilityAsyncOverloads_WithCanceledToken_ShouldThrowOperationCanceledException()
        {
            var access = (IDatabaseAccess)new DBAccess(DatabaseTestHelper.CreateTestDatabaseOptions());
            using var source = new CancellationTokenSource();
            source.Cancel();

            Action action = () => access.ExecuteQueryAsync("SELECT 1", source.Token).GetAwaiter().GetResult();

            action.Should().Throw<OperationCanceledException>();
        }
    }
}
