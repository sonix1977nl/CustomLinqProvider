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
            expression = SimplifyExpression(expression);

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
                    return translator.GenerateTranslator(reader);
                }
                catch
                {
                    reader.Dispose();
                    throw;
                }
            }
        }

        private Expression SimplifyExpression(Expression expression)
        {
            expression = EvaluateAppropriateSubExpressionsAtRuntime(expression);
            return expression;
        }

        private static Expression EvaluateAppropriateSubExpressionsAtRuntime(Expression expression)
        {
            var nominees = RuntimeEvaluationNominator.Nominate(expression);
            expression = RuntimeEvaluator.Evaluate(expression, nominees);
            return expression;
        }
    }
}
