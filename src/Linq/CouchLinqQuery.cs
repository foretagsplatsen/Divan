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
    public class CouchLinqQuery<T>: IQueryable, IQueryable<T>
    {
        Expression expression;
        CouchQueryProvider provider;

        public CouchLinqQuery(Expression expression, CouchQueryProvider provider)
        {
            this.expression = expression;
            this.provider = provider;
        }

        public CouchLinqQuery(CouchQueryProvider provider)
        {
            this.expression = Expression.Constant(this);
            this.provider = provider;
        }

        Expression IQueryable.Expression
        {
            get { return this.expression; }
        }

        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return this.provider; }
        }

        class TransformingEnumerator<T> : IEnumerator<T>
        {
            private IEnumerator<T> e;
            private MethodInfo transformer;

            public TransformingEnumerator(IEnumerator<T> e, MethodInfo transformer)
            {
                this.e = e;
                this.transformer = transformer;
            }

            public T Current { get { return (T)transformer.Invoke(e.Current, null); } }
            object IEnumerator.Current { get { return transformer.Invoke(e.Current, null); } }

            public void Dispose() { e.Dispose(); }
            public bool MoveNext() { return e.MoveNext(); }
            public void Reset() { e.Reset(); }
        }

        protected virtual IEnumerator<T> DoGetEnumerator<T>()
        {
            var expVisitor = this.provider.Prepare(expression);
            var viewResult = (CouchGenericViewResult)expVisitor.Query.GetResult();

            var dynamicResult =
                viewResult
                    .GetType()
                    .GetMethods()
                    .First(m => m.Name == "ValueDocuments" && m.IsGenericMethodDefinition)
                    .MakeGenericMethod(new Type[] { typeof(T) })
                    .Invoke(viewResult, null);

            if (expVisitor.SelectExpression == null)
                return ((IEnumerable<T>)dynamicResult).GetEnumerator();

            return
                new TransformingEnumerator<T>(
                    ((IEnumerable<T>)dynamicResult).GetEnumerator(),
                    ((MethodCallExpression)expVisitor.SelectExpression).Method);
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
