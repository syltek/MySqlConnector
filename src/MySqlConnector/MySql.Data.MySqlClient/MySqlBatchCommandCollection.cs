#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MySqlConnector.Core;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlBatchCommandCollection : Collection<MySqlBatchCommand>, IReadOnlyList<IMySqlCommand>
	{
		public new MySqlBatchCommand this[int index]
		{
			get => (MySqlBatchCommand) base[index];
			set => base[index] = value;
		}

		IMySqlCommand IReadOnlyList<IMySqlCommand>.this[int index] => (IMySqlCommand) this[index];

		IEnumerator<IMySqlCommand> IEnumerable<IMySqlCommand>.GetEnumerator()
		{
			foreach (MySqlBatchCommand command in this)
				yield return command;
		}
	}
}
