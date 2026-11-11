using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    /// <summary>
    /// Minimal IQueryable wrapper that also implements IAsyncEnumerable so tests can
    /// exercise EF Core's async operators (FirstOrDefaultAsync, ToListAsync, …) on
    /// plain in-memory collections.
    /// </summary>
    internal sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var result = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])
                .MakeGenericMethod(resultType)
                .Invoke(this, [expression]);

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(resultType)
                .Invoke(null, [result]);
        }
    }

    internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression) : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner = inner;

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();

            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    }

    internal static class AsyncQueryableExtensions
    {
        public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
            => new TestAsyncEnumerable<T>(source);
    }
}
