using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Linq;
using System.Reflection;

namespace Divan.Linq
{
    public class CouchQueryProvider: IQueryProvider
    {
        CouchDatabase db;
        string view;
        string design;

        public CouchQueryProvider(CouchDatabase db, string design, string view)
        {
            this.db = db;
            this.view = view;
            this.design = design;
        }

        #region IQueryProvider Members

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new CouchLinqQuery<TElement>(expression, this);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator
                    .CreateInstance(typeof(CouchLinqQuery<>)
                    .MakeGenericType(elementType), new object[] { expression, this });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        protected virtual CouchQuery Prepare(Expression expression)
        {
            return new ExpressionVisitor().ProcessExpression(expression, db, design, view);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);// return Prepare(expression).GetResult<TResult>();
        }

        public object Execute(Expression expression)
        {
            return Prepare(expression).GetResult();
        }

        #endregion
    }
}
