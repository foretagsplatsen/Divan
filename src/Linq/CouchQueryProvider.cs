// adapted from sample code at http://linqinaction.net/files/folders/linqinaction/entry1952.aspx

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

        CouchViewDefinition definition;

        public CouchQueryProvider(CouchDatabase db, string design, string view)
        {
            this.db = db;
            this.view = view;
            this.design = design;
        }

        public CouchQueryProvider(CouchDatabase db, CouchViewDefinition definition)
        {
            this.db = db;
            this.definition = definition;
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

        public virtual ExpressionVisitor Prepare(Expression expression)
        {
            return 
                definition == null ?
                new ExpressionVisitor().ProcessExpression(expression, db, design, view) :
                new ExpressionVisitor().ProcessExpression(expression, db, definition);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            if (!typeof(CouchViewResult).IsAssignableFrom(typeof(TResult)))
                throw new InvalidCastException("Only subclasses of CouchViewResult are supported. " + typeof(TResult).ToString() + " is not a valid result type.");

            return (TResult)Execute(expression);
        }

        public object Execute(Expression expression)
        {
            var expVisitor = Prepare(expression);
            var result = expVisitor.Query.GetResult().RowDocuments().First();

            if (expVisitor.SelectExpression == null)
                return result;

            return ((MethodCallExpression)expVisitor.SelectExpression).Method.Invoke(result, null);
        }

        public string GetQueryText(Expression expression)
        {
            return Prepare(expression).ToString();
        }

        #endregion
    }
}
