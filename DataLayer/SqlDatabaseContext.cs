﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using Shared;

namespace DataLayer {
	public class SqlDatabaseContext : DatabaseContext {
		public SqlConnection Connection { get; private set; }
		public string ConnectionString { get; private set; }

		public SqlDatabaseContext(string connection_string) {
			ConnectionString = connection_string;
		}

		private bool TrySetProperty(object obj, string property, object value) {
			var prop = obj.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
			if (prop != null && prop.CanWrite) {
				prop.SetValue(obj, value, null);
				return true;
			}
			return false;
		}

		public override List<T> GetAllFromTable<T>(string table) {
			using (Connection = new SqlConnection(ConnectionString)) {
				var command = new SqlCommand() {
					CommandText = $"SELECT * FROM {table}",
					Connection = Connection
				};

				Connection.Open();

				var command_result = command.ExecuteReader();

				var result = new List<T>();

				while (command_result.Read()) {
					var obj = (T)Activator.CreateInstance(typeof(T));
					var props = typeof(T).GetProperties();
					foreach(var prop in props) {
						var column_id = command_result.GetOrdinal(prop.Name);
						switch (prop.PropertyType.Name) {
							case "Int32":
								TrySetProperty(obj, prop.Name, command_result.GetInt32(column_id));
								break;
							case "String":
								TrySetProperty(obj, prop.Name, command_result.GetString(column_id));
								break;
							case "Decimal":
								TrySetProperty(obj, prop.Name, command_result.GetDecimal(column_id));
								break;
							case "Boolean":
								TrySetProperty(obj, prop.Name, command_result.GetBoolean(column_id));
								break;
							case "DateTime":
								TrySetProperty(obj, prop.Name, command_result.GetDateTime(column_id));
								break;
							case "Byte[]":
								var size = command_result.GetBytes(column_id, 0, null, 0, 0);
								var buffer = new byte[size];
								command_result.GetBytes(column_id, 0, buffer, 0, buffer.Length);
								TrySetProperty(obj, prop.Name, buffer);
								break;
						}
					}
					result.Add(obj);
				}

				return result;
			}
		}

		public override bool InsertIntoTable<T>(string table, T obj) {
			using (Connection = new SqlConnection(ConnectionString)) {
				var props = typeof(T).GetProperties();

				var prop_list = props.Select(prop => $"@{prop.Name}").ToList().Skip(1);

				var prop_arguments = string.Join(", ", prop_list);

				var command = new SqlCommand() {
					CommandText = $"INSERT INTO {table} VALUES ({prop_arguments})",
					Connection = Connection
				};

				foreach (var prop in prop_list) {
					var obj_prop_str = prop.Replace("@", "");
					var obj_prop = obj.GetType().GetProperty(obj_prop_str);
					command.Parameters.AddWithValue(prop, obj_prop.GetValue(obj));
				}

				Connection.Open();

				var command_result = command.ExecuteNonQuery();

				return command_result > 0;
			}
		}
	}
}