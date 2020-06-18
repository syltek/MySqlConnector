using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class Transaction : IClassFixture<TransactionFixture>
	{
		public Transaction(TransactionFixture database)
		{
			m_database = database;
			m_connection = m_database.Connection;
		}

		[Fact]
		public void NestedTransactions()
		{
			using (m_connection.BeginTransaction())
			{
				Assert.Throws<InvalidOperationException>(() => m_connection.BeginTransaction());
			}
		}

		[Fact]
		public void Commit()
		{
			m_connection.Execute("delete from transactions_test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Commit();
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new[] { 1, 2 }, results);
		}

		[Fact]
		public void DbConnectionCommit()
		{
			DbConnection connection = m_connection;
			connection.Execute("delete from transactions_test");
			using (var trans = connection.BeginTransaction())
			{
				connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Commit();
			}
			var results = connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new[] { 1, 2 }, results);
		}

#if !BASELINE
		[Fact]
		public async Task CommitAsync()
		{
			await m_connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			using (var trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.CommitAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new[] { 1, 2 }, results);
		}

		[Fact]
		public async Task CommitDisposeAsync()
		{
			await m_connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			MySqlTransaction trans = null;
			try
			{
				trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false);
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.CommitAsync().ConfigureAwait(false);
			}
			finally
			{
				await trans.DisposeAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new[] { 1, 2 }, results);
		}

#if !NET452 && !NET461 && !NET472 && !NETCOREAPP1_1_2 && !NETCOREAPP2_0 && !NETCOREAPP2_1
		[Fact]
		public async Task DbConnectionCommitAsync()
		{
			DbConnection connection = m_connection;
			await connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			using (var trans = await connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.CommitAsync().ConfigureAwait(false);
			}
			var results = await connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new[] { 1, 2 }, results);
		}
#endif
#endif

		[Fact]
		public void Rollback()
		{
			m_connection.Execute("delete from transactions_test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Rollback();
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

		[Fact]
		public void DbConnectionRollback()
		{
			DbConnection connection = m_connection;
			connection.Execute("delete from transactions_test");
			using (var trans = connection.BeginTransaction())
			{
				connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Rollback();
			}
			var results = connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

#if !BASELINE
		[Fact]
		public async Task RollbackAsync()
		{
			await m_connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			using (var trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.RollbackAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}

		[Fact]
		public async Task RollbackDisposeAsync()
		{
			await m_connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			MySqlTransaction trans = null;
			try
			{
				trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false);
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.RollbackAsync().ConfigureAwait(false);
			}
			finally
			{
				await trans.DisposeAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}

#if !NET452 && !NET461 && !NET472 && !NETCOREAPP1_1_2 && !NETCOREAPP2_0 && !NETCOREAPP2_1
		[Fact]
		public async Task DbConnectionRollbackAsync()
		{
			DbConnection connection = m_connection;
			await connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			using (var trans = await connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.RollbackAsync().ConfigureAwait(false);
			}
			var results = await connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}
#endif
#endif

		[Fact]
		public void NoCommit()
		{
			m_connection.Execute("delete from transactions_test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

#if !BASELINE
		[Fact]
		public async Task DisposeAsync()
		{
			await m_connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			MySqlTransaction trans = null;
			try
			{
				trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false);
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
			}
			finally
			{
				await trans.DisposeAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}
#endif

		readonly TransactionFixture m_database;
		readonly MySqlConnection m_connection;
	}
}
