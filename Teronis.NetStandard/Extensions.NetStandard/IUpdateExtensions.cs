﻿using System;
using Teronis.Data;

namespace Teronis.Extensions.NetStandard
{
    public static class UpdateExtensions
    {
        public static ContentUpdate<InnerContentType> CreateUpdateFromContent<ContentType, InnerContentType>(this IContentUpdate<ContentType> update, Func<ContentType, InnerContentType> getInnerContent, object updateCreationSource)
            => new ContentUpdate<InnerContentType>(getInnerContent(update.Content), update.OriginalUpdateCreationSource, updateCreationSource);
    }
}
