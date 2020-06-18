using System.Data.Common;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ClientFactoryTests
	{
		[Fact]
		public void CreateCommand()
		{
			Assert.IsType<MySqlCommand>(MySqlClientFactory.Instance.CreateCommand());
		}

		[Fact]
		public void CreateConnection()
		{
			Assert.IsType<MySqlConnection>(MySqlClientFactory.Instance.CreateConnection());
		}

		[Fact]
		public void CreateConnectionStringBuilder()
		{
			Assert.IsType<MySqlConnectionStringBuilder>(MySqlClientFactory.Instance.CreateConnectionStringBuilder());
		}


		[Fact]
		public void CreateParameter()
		{
			Assert.IsType<MySqlParameter>(MySqlClientFactory.Instance.CreateParameter());
		}

#if !NETCOREAPP1_1_2
		[Fact]
		public void CreateCommandBuilder()
		{
			Assert.IsType<MySqlCommandBuilder>(MySqlClientFactory.Instance.CreateCommandBuilder());
		}

		[Fact]
		public void CreateDataAdapter()
		{
			Assert.IsType<MySqlDataAdapter>(MySqlClientFactory.Instance.CreateDataAdapter());
		}
#endif

#if !NETCOREAPP1_1_2 && !NETCOREAPP2_0
		[Fact]
		public void DbProviderFactoriesGetFactory()
		{
#if !NET452 && !NET461 && !NET472
			DbProviderFactories.RegisterFactory("MySqlConnector", MySqlClientFactory.Instance);
#endif
#if BASELINE
			var providerInvariantName = "MySql.Data.MySqlClient";
#else
			var providerInvariantName = "MySqlConnector";
#endif
			var factory = DbProviderFactories.GetFactory(providerInvariantName);
			Assert.NotNull(factory);
			Assert.Same(MySqlClientFactory.Instance, factory);

			using (var connection = new MySqlConnection())
			{
				factory = System.Data.Common.DbProviderFactories.GetFactory(connection);
				Assert.NotNull(factory);
				Assert.Same(MySqlClientFactory.Instance, factory);
			}
		}
#endif
	}
}
