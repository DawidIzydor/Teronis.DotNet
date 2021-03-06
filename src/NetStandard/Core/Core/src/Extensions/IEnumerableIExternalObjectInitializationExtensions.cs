﻿using System.Collections.Generic;

namespace Teronis.Extensions.tmp
{
    public static class IEnumerableIExternalObjectInitializationExtensions
    {
        public static void LetAttributesReceiveAttributeMemberInfo(this IEnumerable<IExternalObjectInitialization> ttbInitializedObjects)
        {
            var enumerator = ttbInitializedObjects.GetEnumerator();

            if (!enumerator.MoveNext()) {
                return;
            }

            do {
                enumerator.Current.TriggerExternalObjectInitialization();
            } while (enumerator.MoveNext());
        }
    }
}
