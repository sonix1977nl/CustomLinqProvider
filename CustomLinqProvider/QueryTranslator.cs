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
        private bool mIsSequence;
        private LimitingDataReaderMode mLimitingDataReaderMode;
        private Type mElementType;
        private readonly List<PropertyInfo> mProperties = new List<PropertyInfo>();

        public string Text { get { return mText.ToString(); } }

        public IEnumerable<SqlParameter> Parameters { get { return mParameters; } }

        public object GenerateTranslator(SqlDataReader reader)
        {
            var limitedReader = new LimitingDataReader(reader, mLimitingDataReaderMode);
            var converter = Activator.CreateInstance(typeof(SqlReaderConverter<>).MakeGenericType(mElementType), limitedReader, mProperties);
            return mIsSequence ? converter : GetLast((IEnumerable)converter);
        }

        public object GetLast(IEnumerable enumerable)
        {
            object lastItem = null;
            foreach (var item in enumerable)
            {
                lastItem = item;
            }
            return lastItem;
        }

        public override Expression Visit(Expression node)
        {
            var isOuterCall = !mIsInnerCall;
            if (isOuterCall)
            {
                mIsInnerCall = true;
                mText.Length = 0;
                mParameters.Clear();
                mIsSequence = true;
                mLimitingDataReaderMode = LimitingDataReaderMode.None;
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
            if (node.IsQueryable())
            {
                mLimitingDataReaderMode = LimitingDataReaderMode.None;
                mIsSequence = true;
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

                mIsSequence = false;
                mLimitingDataReaderMode = node.Method.Name == "First" ? LimitingDataReaderMode.AtLeastOneRow : LimitingDataReaderMode.None;
                mElementType = TypeSystem.GetElementType(node.Type);
                return resultingNode;
            }

            if (node.Method.Name == "Single" || node.Method.Name == "SingleOrDefault")
            {
                mText.Append(@"SELECT TOP 2 * FROM (");
                var resultingNode = base.VisitMethodCall(node);
                mText.Append(@") AS temp");

                mIsSequence = false;
                mLimitingDataReaderMode = (node.Method.Name == "Single" ? LimitingDataReaderMode.AtLeastOneRow : LimitingDataReaderMode.None) | LimitingDataReaderMode.AtMostOneRow;
                mElementType = TypeSystem.GetElementType(node.Type);
                return resultingNode;
            }

            return base.VisitMethodCall(node);
        }

        private static string EncodeSqlName(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }
    }
}
