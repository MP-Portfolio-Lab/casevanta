namespace Casevanta.Api.Tests.Infrastructure;

/// <summary>All API tests share one in-memory host; each test uses its own cookie jar.</summary>
[CollectionDefinition(Name)]
public sealed class BffCollection : ICollectionFixture<BffFactory>
{
    public const string Name = "BFF";
}
