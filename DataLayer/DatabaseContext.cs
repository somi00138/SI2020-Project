﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer {
	public abstract class DatabaseContext {
		public abstract List<T> GetAllFromTable<T>(string table);
		public abstract bool InsertIntoTable<T>(string table, T obj);
	}
}
