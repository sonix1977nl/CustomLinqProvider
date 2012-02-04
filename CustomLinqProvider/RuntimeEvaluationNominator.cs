using System.Collections.Generic;
using System.Linq.Expressions;

namespace CustomLinqProvider
{
    /// <summary>
    /// Searches for (sub-)expressions in an expression that can be evaluated at run-time (for example, references to local variables).
    /// </summary>
    internal class RuntimeEvaluationNominator : ExpressionVisitor
    {
        private readonly HashSet<Expression> mNominees = new HashSet<Expression>();
        private readonly HashSet<Expression> mSubTreeNominees = new HashSet<Expression>();
        private bool mCannotBeEvaluated;
        private bool mIsSubExpression;

        public static HashSet<Expression> Nominate(Expression expression)
        {
            var instance = new RuntimeEvaluationNominator();
            instance.Visit(expression);
            return instance.mNominees;
        }

        private RuntimeEvaluationNominator()
        {
        }

        public override Expression Visit(Expression node)
        {
            if (node != null)
            {
                var originalIsSubExpression = mIsSubExpression;
                mIsSubExpression = true;

                var originalCannotBeEvaluated = mCannotBeEvaluated;
                mCannotBeEvaluated = false;

                node = base.Visit(node);

                if (!mCannotBeEvaluated)
                {
                    mSubTreeNominees.Clear();
                    if (node.NodeType != ExpressionType.Constant)
                    {
                        mSubTreeNominees.Add(node);
                    }
                }
                else
                {
                    mNominees.AddRange(mSubTreeNominees);
                    mSubTreeNominees.Clear();
                }

                mCannotBeEvaluated |= originalCannotBeEvaluated;

                mIsSubExpression = originalIsSubExpression;

                if (!mIsSubExpression)
                    mNominees.AddRange(mSubTreeNominees);
            }
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.IsQueryable())
                mCannotBeEvaluated = true;
            return base.VisitConstant(node);
        }
        
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            mCannotBeEvaluated = true;
            return base.VisitLambda(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            mCannotBeEvaluated = true;
            return base.VisitParameter(node);
        }
    }
}
