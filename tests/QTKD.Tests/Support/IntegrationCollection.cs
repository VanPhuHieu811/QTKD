namespace QTKD.Tests.Support;

// Mọi test tích hợp dùng chung một ApiFactory và chạy tuần tự (cùng một database kiểm thử).
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<ApiFactory>
{
}
