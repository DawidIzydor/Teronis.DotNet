﻿using System.Diagnostics.CodeAnalysis;

namespace Teronis.Identity.Presenters.Generic
{
    public interface IServiceResult<out ContentType> : IServiceResult
    {
        [MaybeNull]
        new ContentType Content { get; }
    }
}