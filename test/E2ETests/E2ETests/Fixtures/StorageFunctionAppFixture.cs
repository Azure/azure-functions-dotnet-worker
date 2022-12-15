using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    public class StorageFunctionAppFixture : FunctionAppFixture
    {
        public StorageFunctionAppFixture(IMessageSink messageSink) : base(messageSink)
        {
        }

        #region Implementation of IAsyncLifetime

        public override async Task InitializeAsync()
        {
            await StorageHelpers.CreateBlobContainers();
            await StorageHelpers.CreateQueues();

            await base.InitializeAsync();
        }

        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();

            //NOTE: Comment this out if you want to keep during local testing.
            await StorageHelpers.DeleteQueues();
            await StorageHelpers.DeleteBlobContainers();
        }

        #endregion
    }

    [CollectionDefinition(Constants.StorageFunctionAppCollectionName)]
    public class StorageFunctionAppCollection : ICollectionFixture<StorageFunctionAppFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
