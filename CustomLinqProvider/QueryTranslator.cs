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
    public class QueryTranslator : ExtendedExpressionVisitor
    {
        // TODO: Suport string operations like StartsWith, etc.

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

        protected override void OnBeginVisitExpression(Expression node)
        {
            mIsInnerCall = true;
            mText.Length = 0;
            mParameters.Clear();
            mIsSequence = true;
            mLimitingDataReaderMode = LimitingDataReaderMode.None;
            mElementType = null;
            mProperties.Clear();
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

                    var column = GetColumnName(property);

                    mText.Append(separator);
                    mText.Append(EncodeSqlName(column));

                    separator = @", ";
                }

                mText.Append(@" FROM ");
                mText.Append(EncodeSqlName(schema));
                mText.Append(@".");
                mText.Append(EncodeSqlName(table));
            }
            else
            {
                mText.Append(node.Value.ToSqlConstant());
            }

            return base.VisitConstant(node);
        }

        private static string GetColumnName(PropertyInfo property)
        {
            var column = property.Name;

            var columnAttributes = property.GetCustomAttributes(typeof(ColumnAttribute), true).Cast<ColumnAttribute>();
            foreach (var columnAttribute in columnAttributes)
            {
                if (!string.IsNullOrEmpty(columnAttribute.Name))
                    column = columnAttribute.Name;
            }
            return column;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Where")
            {
                mText.Append(@"SELECT * FROM (");
                base.Visit(node.Arguments[0]);
                mText.Append(@") AS temp WHERE ");
                var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
                base.Visit(lambda.Body);
                return node;
            }


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

            throw new InvalidOperationException(string.Format("Method [{0}] is unsupported.", node.Method.Name));
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                mText.Append("NOT");
                return base.Visit(node.Operand);
            }

            if (node.NodeType == ExpressionType.UnaryPlus)
            {
                mText.Append("+");
                return base.Visit(node.Operand);
            }

            if (node.NodeType == ExpressionType.Negate)
            {
                mText.Append("-");
                return base.Visit(node.Operand);
            }
            
            throw new InvalidOperationException(string.Format("Unary operator [{0}] is not supported.", node.NodeType));
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            // TODO: Support binary operators like left shift, right shift, power, etc.

            mText.Append(" (");

            Visit(node.Left);

            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    mText.Append(" + ");
                    break;

                case ExpressionType.And:
                    mText.Append(" & ");
                    break;

                case ExpressionType.AndAlso:
                    mText.Append(" AND ");
                    break;

                case ExpressionType.Divide:
                    mText.Append(" / ");
                    break;

                case ExpressionType.Equal:
                    mText.Append(" = ");
                    break;

                case ExpressionType.ExclusiveOr:
                    mText.Append(" ^ ");
                    break;

                case ExpressionType.GreaterThan:
                    mText.Append(" > ");
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    mText.Append(" >= ");
                    break;

                case ExpressionType.LessThan:
                    mText.Append(" < ");
                    break;

                case ExpressionType.LessThanOrEqual:
                    mText.Append(" <= ");
                    break;

                case ExpressionType.Modulo:
                    mText.Append(" % ");
                    break;

                case ExpressionType.Multiply:
                    mText.Append(" * ");
                    break;

                case ExpressionType.NotEqual:
                    mText.Append(" <> ");
                    break;

                case ExpressionType.Or:
                    mText.Append(" | ");
                    break;

                case ExpressionType.OrElse:
                    mText.Append(" OR ");
                    break;

                case ExpressionType.Subtract:
                    mText.Append(" - ");
                    break;
            }
            
            Visit(node.Right);

            mText.Append(")");

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter && node.Member is PropertyInfo)
            {
                mText.Append(EncodeSqlName(GetColumnName((PropertyInfo)node.Member)));
                return node;
            }

            throw new InvalidOperationException(string.Format("Unsupported member [{0}].", node.Member.Name));
        }

        private static string EncodeSqlName(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }
    }
}
