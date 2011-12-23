using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;
using System.Data.SqlClient;

namespace CustomLinqProvider
{
    public class Table<T> : IQueryable<T>
    {
        internal Table(SqlConnection connection)
        {
            Provider = new CustomProvider(connection);
            Expression = Expression.Constant(this);
        }

        internal Table(CustomProvider provider, Expression expression)
        {
            Provider = provider;
            Expression = expression;
        }

        public Type ElementType { get { return typeof(T); } }

        public Expression Expression { get; private set; }

        public IQueryProvider Provider { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }
    }
}
