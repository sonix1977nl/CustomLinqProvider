using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Collections;
using System.Data;

namespace CustomLinqProvider
{
    internal class CustomProvider : IQueryProvider
    {
        private readonly SqlConnection mConnection;

        internal CustomProvider(SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            mConnection = connection;
        }

        public IQueryable<T> CreateQuery<T>(Expression expression)
        {
            return new Table<T>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            return (IQueryable)Activator.CreateInstance(typeof(Table<>).MakeGenericType(elementType), this, expression);
        }

        public T Execute<T>(Expression expression)
        {
            return (T)Execute(expression);
        }

        public object Execute(Expression expression)
        {
            var translator = new QueryTranslator();
            translator.Visit(expression);

            if (mConnection.State == ConnectionState.Closed)
                mConnection.Open();

            using (var command = mConnection.CreateCommand())
            {
                command.CommandText = translator.Text;

                foreach (var parameter in translator.Parameters)
                {
                    command.Parameters.Add(parameter);
                }

                var reader = command.ExecuteReader();
                try
                {
                    if (translator.OutputType == QueryOutputType.Sequence)
                    {
                        return Activator.CreateInstance(typeof(SqlReaderConverter<>).MakeGenericType(translator.ElementType), reader, translator.Properties);
                    }
                    else if (translator.OutputType == QueryOutputType.Single || translator.OutputType == QueryOutputType.OptionalSingle)
                    {
                        var converter = (IEnumerable)Activator.CreateInstance(typeof(SqlReaderConverter<>).MakeGenericType(translator.ElementType), reader, translator.Properties);
                        var enumerator = converter.GetEnumerator();
                        var hasItem = enumerator.MoveNext();
                        if (translator.OutputType == QueryOutputType.Single && !hasItem)
                            throw new InvalidOperationException("Sequence contains no elements.");
                        return enumerator.Current;
                    }

                    throw new InvalidOperationException();
                }
                catch
                {
                    reader.Dispose();
                    throw;
                }
            }
        }
    }
}
