using System;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;
using Xunit.Sdk;

namespace SideBySide
{
	[Collection("BulkLoaderCollection")]
	public class BulkLoaderAsync : IClassFixture<DatabaseFixture>
	{
		public BulkLoaderAsync(DatabaseFixture database)
		{
			m_testTable = "BulkLoaderAsyncTest";
			var initializeTable = $@"
				drop table if exists {m_testTable};
				create table {m_testTable}
				(
					one int primary key
					, ignore_one int
					, two varchar(200)
					, ignore_two varchar(200)
					, three varchar(200)
					, four datetime
					, five blob
				) CHARACTER SET = UTF8;";
			database.Connection.Execute(initializeTable);

			m_memoryStreamBytes = System.Text.Encoding.UTF8.GetBytes(@"1,'two-1','three-1'
2,'two-2','three-2'
3,'two-3','three-3'
4,'two-4','three-4'
5,'two-5','three-5'
");
		}

		[SkippableFact(ConfigSettings.TsvFile)]
		public async Task BulkLoadTsvFile()
		{
			using (var connection = new MySqlConnection(GetConnectionString()))
			{
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = false;
				int rowCount = await bl.LoadAsync();
				Assert.Equal(20, rowCount);
			}
		}

		[SkippableFact(ConfigSettings.LocalTsvFile)]
		public async Task BulkLoadLocalTsvFile()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.FileName = AppConfig.MySqlBulkLoaderLocalTsvFile;
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = true;
				int rowCount = await bl.LoadAsync();
				Assert.Equal(20, rowCount);
			}
		}

		[SkippableFact(ConfigSettings.LocalTsvFile)]
		public async Task BulkLoadLocalTsvFileDoubleEscapedTerminators()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.FileName = AppConfig.MySqlBulkLoaderLocalTsvFile;
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.LineTerminator = "\\n";
				bl.FieldTerminator = "\\t";
				bl.Local = true;
				int rowCount = await bl.LoadAsync();
				Assert.Equal(20, rowCount);
			}
		}

		[SkippableFact(ConfigSettings.CsvFile)]
		public async Task BulkLoadCsvFile()
		{
			using (var connection = new MySqlConnection(GetConnectionString()))
			{
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.FileName = AppConfig.MySqlBulkLoaderCsvFile;
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.FieldTerminator = ",";
				bl.FieldQuotationCharacter = '"';
				bl.FieldQuotationOptional = true;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = false;
				int rowCount = await bl.LoadAsync();
				Assert.Equal(20, rowCount);
			}
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public async Task BulkLoadLocalCsvFile()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				await connection.OpenAsync();
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile;
				bl.TableName = m_testTable;
				bl.CharacterSet = "UTF8";
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.FieldTerminator = ",";
				bl.FieldQuotationCharacter = '"';
				bl.FieldQuotationOptional = true;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = true;
				int rowCount = await bl.LoadAsync();
				Assert.Equal(20, rowCount);
			}
		}

		[Fact]
		public async Task BulkLoadCsvFileNotFound()
		{
			using (var connection = new MySqlConnection(GetConnectionString()))
			{
				await connection.OpenAsync();

				var secureFilePath = await connection.ExecuteScalarAsync<string>(@"select @@global.secure_file_priv;");
				if (string.IsNullOrEmpty(secureFilePath) || secureFilePath == "NULL")
					return;

				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.FileName = Path.Combine(secureFilePath, AppConfig.MySqlBulkLoaderCsvFile + "-junk");
				bl.TableName = m_testTable;
				bl.CharacterSet = "UTF8";
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.FieldTerminator = ",";
				bl.FieldQuotationCharacter = '"';
				bl.FieldQuotationOptional = true;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = false;
				try
				{
					int rowCount = await bl.LoadAsync();
				}
				catch (Exception exception)
				{
					while (exception.InnerException is object)
						exception = exception.InnerException;

					if (!(exception is FileNotFoundException))
					{
						try
						{
							Assert.Contains("Errcode: 2 ", exception.Message, StringComparison.OrdinalIgnoreCase);
						}
						catch (ContainsException)
						{
							Assert.Contains("OS errno 2 ", exception.Message, StringComparison.OrdinalIgnoreCase);
						}
						Assert.Contains("No such file or directory", exception.Message);
					}
				}
			}
		}

		[Fact]
		public async Task BulkLoadLocalCsvFileNotFound()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				await connection.OpenAsync();
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.Timeout = 3; //Set a short timeout for this test because the file not found exception takes a long time otherwise, the timeout does not change the result
				bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile + "-junk";
				bl.TableName = m_testTable;
				bl.CharacterSet = "UTF8";
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.FieldTerminator = ",";
				bl.FieldQuotationCharacter = '"';
				bl.FieldQuotationOptional = true;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = true;
				try
				{
					int rowCount = await bl.LoadAsync();
				}
				catch (MySqlException mySqlException)
				{
					while (mySqlException.InnerException is object)
					{
						if (mySqlException.InnerException is MySqlException innerException)
						{
							mySqlException = innerException;
						}
						else
						{
							Assert.IsType<System.IO.FileNotFoundException>(mySqlException.InnerException);
							break;
						}
					}
					if (mySqlException.InnerException is null)
					{
						Assert.IsType<System.IO.FileNotFoundException>(mySqlException);
					}
				}
				catch (Exception exception)
				{
					//We know that the exception is not a MySqlException, just use the assertion to fail the test
					Assert.IsType<MySqlException>(exception);
				}
				;
			}
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public async Task BulkLoadLocalCsvFileInTransactionWithCommit()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				await connection.OpenAsync();
				using (var transaction = connection.BeginTransaction())
				{
					var bulkLoader = new MySqlBulkLoader(connection)
					{
						FileName = AppConfig.MySqlBulkLoaderLocalCsvFile,
						TableName = m_testTable,
						CharacterSet = "UTF8",
						NumberOfLinesToSkip = 1,
						FieldTerminator = ",",
						FieldQuotationCharacter = '"',
						FieldQuotationOptional = true,
						Local = true,
					};
					bulkLoader.Expressions.Add("five = UNHEX(five)");
					bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

					var rowCount = await bulkLoader.LoadAsync();
					Assert.Equal(20, rowCount);

					transaction.Commit();
				}

				Assert.Equal(20, await connection.ExecuteScalarAsync<int>($@"select count(*) from {m_testTable};"));
			}
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public async Task BulkLoadLocalCsvFileInTransactionWithRollback()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				await connection.OpenAsync();
				using (var transaction = connection.BeginTransaction())
				{
					var bulkLoader = new MySqlBulkLoader(connection)
					{
						FileName = AppConfig.MySqlBulkLoaderLocalCsvFile,
						TableName = m_testTable,
						CharacterSet = "UTF8",
						NumberOfLinesToSkip = 1,
						FieldTerminator = ",",
						FieldQuotationCharacter = '"',
						FieldQuotationOptional = true,
						Local = true,
					};
					bulkLoader.Expressions.Add("five = UNHEX(five)");
					bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

					var rowCount = await bulkLoader.LoadAsync();
					Assert.Equal(20, rowCount);

					transaction.Rollback();
				}

				Assert.Equal(0, await connection.ExecuteScalarAsync<int>($@"select count(*) from {m_testTable};"));
			}
		}

		[Fact]
		public async Task BulkLoadMissingFileName()
		{
			using (var connection = new MySqlConnection(GetConnectionString()))
			{
				await connection.OpenAsync();
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.FieldTerminator = ",";
				bl.FieldQuotationCharacter = '"';
				bl.FieldQuotationOptional = true;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = false;
#if BASELINE
				await Assert.ThrowsAsync<System.NullReferenceException>(async () =>
				{
					int rowCount = await bl.LoadAsync();
				});
#else
				await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
				{
					int rowCount = await bl.LoadAsync();
				});
#endif
			}
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public async Task BulkLoadMissingTableName()
		{
			using (var connection = new MySqlConnection(GetConnectionString()))
			{
				await connection.OpenAsync();
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.FieldTerminator = ",";
				bl.FieldQuotationCharacter = '"';
				bl.FieldQuotationOptional = true;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = false;
#if BASELINE
				await Assert.ThrowsAsync<MySqlException>(async () =>
				{
					int rowCount = await bl.LoadAsync();
				});
#else
				await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
				{
					int rowCount = await bl.LoadAsync();
				});
#endif
			}
		}

#if !BASELINE
		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public async Task BulkLoadFileStreamInvalidOperation()
		{
			using (var connection = new MySqlConnection(GetConnectionString()))
			{
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
				{
					bl.SourceStream = fileStream;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
					bl.NumberOfLinesToSkip = 1;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Expressions.Add("five = UNHEX(five)");
					bl.Local = false;
					await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
					{
						int rowCount = await bl.LoadAsync();
					});
				}
			}
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public async Task BulkLoadLocalFileStream()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				await connection.OpenAsync();
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
				{
					bl.SourceStream = fileStream;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
					bl.NumberOfLinesToSkip = 1;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Expressions.Add("five = UNHEX(five)");
					bl.Local = true;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(20, rowCount);
				}
			}
		}

		[Fact]
		public async Task BulkLoadMemoryStreamInvalidOperation()
		{
			using (var connection = new MySqlConnection(GetConnectionString()))
			{
				await connection.OpenAsync();
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
				{
					bl.SourceStream = memoryStream;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three" });
					bl.NumberOfLinesToSkip = 0;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Local = false;
					await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
					{
						int rowCount = await bl.LoadAsync();
					});
				}
			}
		}

		[Fact]
		public async Task BulkLoadLocalMemoryStream()
		{
			using (var connection = new MySqlConnection(GetLocalConnectionString()))
			{
				await connection.OpenAsync();
				MySqlBulkLoader bl = new MySqlBulkLoader(connection);
				using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
				{
					bl.SourceStream = memoryStream;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three" });
					bl.NumberOfLinesToSkip = 0;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Local = true;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(5, rowCount);
				}
			}
		}
#endif

		private static string GetConnectionString() => BulkLoaderSync.GetConnectionString();
		private static string GetLocalConnectionString() => BulkLoaderSync.GetLocalConnectionString();

		readonly string m_testTable;
		readonly byte[] m_memoryStreamBytes;
	}
}
