using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Reflection;

namespace CustomLinqProvider
{
    public class QueryTranslator : ExpressionVisitor
    {
        private bool mIsInnerCall;
        private readonly StringBuilder mText = new StringBuilder();
        private readonly List<SqlParameter> mParameters = new List<SqlParameter>();
        private QueryOutputType mOutputType;
        private Type mElementType;
        private readonly List<PropertyInfo> mProperties = new List<PropertyInfo>();

        public string Text { get { return mText.ToString(); } }

        public IEnumerable<SqlParameter> Parameters { get { return mParameters; } }

        public object GenerateTranslator(SqlDataReader reader)
        {
            if (mOutputType == QueryOutputType.Sequence)
            {
                return Activator.CreateInstance(typeof(SqlReaderConverter<>).MakeGenericType(mElementType), reader, mProperties, SqlReaderConverterFlags.None);
            }
            
            var converter = (IEnumerable)Activator.CreateInstance(typeof(SqlReaderConverter<>).MakeGenericType(mElementType), reader, mProperties, SqlReaderConverterFlags.None);
            var enumerator = converter.GetEnumerator();
            var hasItem = enumerator.MoveNext();
            if (mOutputType == QueryOutputType.Single && !hasItem)
                throw new InvalidOperationException("Sequence contains no elements.");
            return enumerator.Current;
        }

        public override Expression Visit(Expression node)
        {
            var isOuterCall = !mIsInnerCall;
            if (isOuterCall)
            {
                mIsInnerCall = true;
                mText.Length = 0;
                mParameters.Clear();
                mOutputType = QueryOutputType.Sequence;
                mElementType = null;
                mProperties.Clear();
            }

            try
            {
                return base.Visit(node);
            }
            finally
            {
                if (isOuterCall)
                {
                    mIsInnerCall = false;
                }
            }
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (IsQueryable(node))
            {
                mOutputType = QueryOutputType.Sequence;
                mElementType = TypeSystem.GetElementType(node.Type);

                var schema = @"dbo";
                var table = mElementType.Name;

                var tableAttributes = mElementType.GetCustomAttributes(typeof(TableAttribute), true).Cast<TableAttribute>();
                foreach (var tableAttribute in tableAttributes)
                {
                    if (!string.IsNullOrEmpty(tableAttribute.SchemaName))
                        schema = tableAttribute.SchemaName;

                    if (!string.IsNullOrEmpty(tableAttribute.Name))
                        table = tableAttribute.Name;
                }

                mText.Append(@"SELECT");

                var separator = @" ";
                var properties = mElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(a => a.CanRead && a.CanWrite);
                foreach (var property in properties)
                {
                    mProperties.Add(property);

                    var column = property.Name;

                    var columnAttributes = property.GetCustomAttributes(typeof(ColumnAttribute), true).Cast<ColumnAttribute>();
                    foreach (var columnAttribute in columnAttributes)
                    {
                        if (!string.IsNullOrEmpty(columnAttribute.Name))
                            column = columnAttribute.Name;
                    }

                    mText.Append(separator);
                    mText.Append(EncodeSqlName(column));

                    separator = @", ";
                }

                mText.Append(@" FROM ");
                mText.Append(EncodeSqlName(schema));
                mText.Append(@".");
                mText.Append(EncodeSqlName(table));
            }

            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "First" || node.Method.Name == "FirstOrDefault")
            {
                mText.Append(@"SELECT TOP 1 * FROM (");
                var resultingNode = base.VisitMethodCall(node);
                mText.Append(@") AS temp");

                mOutputType = node.Method.Name == "First" ? QueryOutputType.Single : QueryOutputType.OptionalSingle;
                mElementType = TypeSystem.GetElementType(node.Type);
                return resultingNode;
            }
            else
            {
                return base.VisitMethodCall(node);
            }
        }

        private static bool IsQueryable(ConstantExpression node)
        {
            return node.Type.GetInterfaces().Any(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IQueryable<>));
        }

        private static string EncodeSqlName(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }
    }
}
