﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Teronis.Collections.Generic
{
    public class EnumerableDataSource<DataType> : AsyncDataSource<DataType>
    {
        public static EnumerableDataSource<DataType> Create(IEnumerable<DataType> enumerable, ILogger logger)
        {
            enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
            var asyncEnumerable = enumerable.ToAsyncEnumerable();
            var dataSource = new EnumerableDataSource<DataType>(asyncEnumerable, logger);
            return dataSource;
        }

        private IAsyncEnumerable<DataType> asyncEnumerable;

        public EnumerableDataSource(IAsyncEnumerable<DataType> asyncEnumerable, ILogger logger)
            : base(logger)
            => this.asyncEnumerable = asyncEnumerable ?? throw new ArgumentNullException(nameof(asyncEnumerable));

        protected override IAsyncEnumerable<DataType> EnumerateAsync(CancellationToken cancellationToken = default)
            => asyncEnumerable;
    }
}