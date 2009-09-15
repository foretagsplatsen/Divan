// adapted from sample code at http://linqinaction.net/files/folders/linqinaction/entry1952.aspx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Divan.Linq
{
    public class ExpressionVisitor
    {
        public virtual CouchQuery Query { get; protected set; }
        public virtual MethodCallExpression SelectExpression { get; protected set; }

        public ExpressionVisitor ProcessExpression(Expression expression, CouchDatabase db, string design, string view)
        {
            Query = db.Query(design, view);
            VisitExpression(expression);
            return this;
        }

        public ExpressionVisitor ProcessExpression(Expression expression, CouchDatabase db, CouchViewDefinition definition)
        {
            Query = db.Query(definition);
            VisitExpression(expression);
            return this;
        }

        private void VisitExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    VisitAndAlso((BinaryExpression)expression);
                    break;
                case ExpressionType.Equal:
                    CallIfPresent((BinaryExpression)expression, (val) => Query.Key(val));
                    break;
                case ExpressionType.LessThanOrEqual:
                    CallIfPresent((BinaryExpression)expression, (val) => Query.EndKey(val));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    CallIfPresent((BinaryExpression)expression, (val) => Query.StartKey(val));
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
            else if ((expression.Method.DeclaringType == typeof(Queryable)) &&
                (expression.Method.Name == "Select"))
                SelectExpression = expression;
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
            // since all things will have to translate to "key", and the only
            // place where we know what "key" really is is within the view,
            // just check that it's a member access expression
            if ((expression.Left.NodeType != ExpressionType.MemberAccess))
                return null;

            object key;
            if (expression.Right.NodeType == ExpressionType.Constant)
                key = ((ConstantExpression)expression.Right).Value;
            else if (expression.Right.NodeType == ExpressionType.MemberAccess)
                key = GetMemberValue((MemberExpression)expression.Right);
            else
                throw new NotSupportedException("Expression type not supported: " + expression.Right.NodeType.ToString());

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