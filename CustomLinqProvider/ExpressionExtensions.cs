using System.Linq;
using System.Linq.Expressions;

namespace CustomLinqProvider
{
    public static class ExpressionExtensions
    {
        public static bool IsQueryable(this ConstantExpression node)
        {
            return node.Type.GetInterfaces().Any(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IQueryable<>));
        }
    }
}
