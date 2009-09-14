using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Divan.Linq
{
    internal class ExpressionVisitor
    {
        CouchQuery query;

        public CouchQuery ProcessExpression(Expression expression, CouchDatabase db, string design, string view)
        {
            query = db.Query(design, view);
            VisitExpression(expression);
            return query;
        }

        private void VisitExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    VisitAndAlso((BinaryExpression)expression);
                    break;
                case ExpressionType.Equal:
                    CallIfPresent((BinaryExpression)expression, (val) => query.Key(val));
                    break;
                case ExpressionType.LessThanOrEqual:
                    CallIfPresent((BinaryExpression)expression, (val) => query.EndKey(val));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    CallIfPresent((BinaryExpression)expression, (val) => query.StartKey(val));
                    break;
                default:
                    if (expression is MethodCallExpression)
                        VisitMethodCall((MethodCallExpression)expression);
                    else if (expression is LambdaExpression)
                        VisitExpression(((LambdaExpression)expression).Body);
                    break;
            }
        }

        private void VisitAndAlso(BinaryExpression andAlso)
        {
            VisitExpression(andAlso.Left);
            VisitExpression(andAlso.Right);
        }

        private void VisitMethodCall(MethodCallExpression expression)
        {
            if ((expression.Method.DeclaringType == typeof(Queryable)) && 
                (expression.Method.Name == "Where"))
                VisitExpression(((UnaryExpression)expression.Arguments[1]).Operand);
            else
                throw new NotSupportedException("Method not supported: " + expression.Method.Name);
        }

        private void CallIfPresent(Expression expression, Action<object> callback)
        {
            object val;
            if ((val = GetRightExpressionValue((BinaryExpression)expression)) == null)
                return;

            callback(val);
        }

        private object GetRightExpressionValue(BinaryExpression expression)
        {
            if ((expression.Left.NodeType != ExpressionType.MemberAccess) ||
              (((MemberExpression)expression.Left).Member.Name != "Key"))
                return null;

            object key;
            if (expression.Right.NodeType == ExpressionType.Constant)
                key = ((ConstantExpression)expression.Right).Value;
            else if (expression.Right.NodeType == ExpressionType.MemberAccess)
                key = GetMemberValue((MemberExpression)expression.Right);
            else
                throw new NotSupportedException("Expression type not supported for publisher: " + expression.Right.NodeType.ToString());

            return key;
        }

        private Object GetMemberValue(MemberExpression memberExpression)
        {
            MemberInfo memberInfo;
            Object obj;

            if (memberExpression == null)
                throw new ArgumentNullException("memberExpression");

            // Get object
            if (memberExpression.Expression is ConstantExpression)
                obj = ((ConstantExpression)memberExpression.Expression).Value;
            else if (memberExpression.Expression is MemberExpression)
                obj = GetMemberValue((MemberExpression)memberExpression.Expression);
            else
                throw new NotSupportedException("Expression type not supported: " + memberExpression.Expression.GetType().FullName);

            // Get value
            memberInfo = memberExpression.Member;
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo property = (PropertyInfo)memberInfo;
                return property.GetValue(obj, null);
            }
            else if (memberInfo is FieldInfo)
            {
                FieldInfo field = (FieldInfo)memberInfo;
                return field.GetValue(obj);
            }
            else
            {
                throw new NotSupportedException("MemberInfo type not supported: " + memberInfo.GetType().FullName);
            }
        }
    }
}