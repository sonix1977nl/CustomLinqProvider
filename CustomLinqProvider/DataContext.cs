using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace CustomLinqProvider
{
    public class DataContext : IDisposable
    {
        private readonly SqlConnection mConnection;

        public DataContext(SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            mConnection = connection;
        }

        public IQueryable<T> GetTable<T>()
        {
            return new Table<T>(mConnection);
        }

        public void Dispose()
        {
        }
    }
}
