using System.Linq.Expressions;

namespace CustomLinqProvider
{
    public abstract class ExtendedExpressionVisitor : ExpressionVisitor
    {

        private bool mIsSubExpression;

        public override Expression Visit(Expression node)
        {
            if (mIsSubExpression)
                OnBeginVisitSubexpression(node);
            else
                OnBeginVisitExpression(node);
            var originalIsSubExpression = mIsSubExpression;
            mIsSubExpression = true;
            node = base.Visit(node);
            mIsSubExpression = originalIsSubExpression;
            if (mIsSubExpression)
                OnEndVisitSubexpression(node);
            else
                OnEndVisitExpression(node);
            return node;
        }

        protected virtual void OnBeginVisitExpression(Expression node)
        {
        }

        protected virtual void OnEndVisitExpression(Expression node)
        {
        }

        protected virtual void OnBeginVisitSubexpression(Expression node)
        {
        }

        protected virtual void OnEndVisitSubexpression(Expression node)
        {
        }
    }
}
