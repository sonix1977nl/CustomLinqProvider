using System.Collections.Generic;
using System.Linq.Expressions;

namespace CustomLinqProvider
{
    /// <summary>
    /// Evaluates given sub-expressions at runtime and replaces them in the full expression with the evaluated result.
    /// </summary>
    internal class RuntimeEvaluator : ExtendedExpressionVisitor
    {
        private readonly HashSet<Expression> mNominees;

        public static Expression Evaluate(Expression expression, HashSet<Expression> nominees)
        {
            var instance = new RuntimeEvaluator(nominees);
            return instance.Visit(expression);
        }

        private RuntimeEvaluator(HashSet<Expression> nominees)
        {
            mNominees = nominees;
        }

        public override Expression Visit(Expression node)
        {
            if (mNominees.Contains(node))
            {
                var lambda = Expression.Lambda(node);
                var @delegate = lambda.Compile();
                var value = @delegate.DynamicInvoke(null);
                return Expression.Constant(value, node.Type);
            }
            return base.Visit(node);
        }
    }
}
