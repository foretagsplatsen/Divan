// adapted from sample code at http://linqinaction.net/files/folders/linqinaction/entry1952.aspx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;
using System.Reflection;

namespace Divan.Linq
{
    /// <summary>
    /// IQueryable implementation for CouchDB Query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CouchLinqQuery<T>: IQueryable, IQueryable<T>
    {
        Expression expression;
        CouchQueryProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CouchLinqQuery&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="provider">The provider.</param>
        public CouchLinqQuery(Expression expression, CouchQueryProvider provider)
        {
            this.expression = expression;
            this.provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CouchLinqQuery&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public CouchLinqQuery(CouchQueryProvider provider)
        {
            this.expression = Expression.Constant(this);
            this.provider = provider;
        }

        /// <summary>
        /// Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The <see cref="T:System.Linq.Expressions.Expression"/> that is associated with this instance of <see cref="T:System.Linq.IQueryable"/>.
        /// </returns>
        Expression IQueryable.Expression
        {
            get { return this.expression; }
        }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="T:System.Linq.IQueryable"/> is executed.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// A <see cref="T:System.Type"/> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.
        /// </returns>
        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The <see cref="T:System.Linq.IQueryProvider"/> that is associated with this data source.
        /// </returns>
        IQueryProvider IQueryable.Provider
        {
            get { return this.provider; }
        }

        /// <summary>
        /// Enumerator facade that applies the select expression to all internal members
        /// </summary>
        /// <typeparam name="TReturn">The type of the return.</typeparam>
        class TransformingEnumerator<TReturn> : IEnumerator<TReturn>
        {
            private IEnumerator e;
            private Delegate transformer;

            public TransformingEnumerator(IEnumerator e, MethodCallExpression transformer)
            {
                this.e = e;

                var t = (UnaryExpression)transformer.Arguments[1];
                this.transformer = ((LambdaExpression)t.Operand).Compile();
            }

            public TReturn Current { get { return (TReturn)transformer.DynamicInvoke(e.Current); } }
            object IEnumerator.Current { get { return transformer.DynamicInvoke(e.Current); } }

            public void Dispose() { }
            public bool MoveNext() { return e.MoveNext(); }
            public void Reset() { e.Reset(); }
        }

        /// <summary>
        /// Executes the query and returns a properly-typed IEnumerator for the result
        /// </summary>
        /// <typeparam name="TReturn">The type of the return.</typeparam>
        /// <returns></returns>
        protected virtual IEnumerator<TReturn> DoGetEnumerator<TReturn>()
        {
            var expVisitor = this.provider.Prepare(expression);
            var viewResult = (CouchGenericViewResult)expVisitor.Query.GetResult();

            var typeParams = new Type[] { 
                expVisitor.SelectExpression == null ?
                // no expression, everything is fine. T will match
                typeof(T) :
                // if there is a select expression, then the type of the query 
                // will be defined by the first type parameter to the first
                // method parameter in the selection expression:
                // TOurReturnValue Select(IQueriable<TOurType>)
                expVisitor
                    .SelectExpression
                    .Method
                    .GetParameters()[0]
                    .ParameterType
                    .GetGenericArguments()[0]
            };

            // can't be avoided. the type parameter TResult, is required by the interface
            // and doesn't have the type restrictions that Divan requires. have to play games
            var dynamicResult =
                viewResult
                    .GetType()
                    .GetMethods()
                    .First(m => m.Name == "ValueDocuments" && m.IsGenericMethodDefinition)
                    .MakeGenericMethod(typeParams)
                    .Invoke(viewResult, null);

            if (expVisitor.SelectExpression == null)
                return ((IEnumerable<TReturn>)dynamicResult).GetEnumerator();

            return
                new TransformingEnumerator<TReturn>(
                    ((IEnumerable)dynamicResult).GetEnumerator(),
                    expVisitor.SelectExpression);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return DoGetEnumerator<T>();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return DoGetEnumerator<CouchDocument>();
        }

        public override string ToString() { return this.provider.GetQueryText(this.expression); }
        public override bool Equals(object obj) { return obj == null ? false : ToString().Equals(obj.ToString()); }
        public override int GetHashCode() { return ToString().GetHashCode(); }
    }
}
