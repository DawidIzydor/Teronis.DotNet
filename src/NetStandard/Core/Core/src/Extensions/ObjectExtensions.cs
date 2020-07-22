﻿using System;
using System.Diagnostics.CodeAnalysis;
using Teronis.Utils;

namespace Teronis.Extensions
{
    public static class ObjectExtensions
    {
        public static bool IsNullable(this object obj)
        {
            if (obj == null) {
                return true; // obvious
            }

            var type = obj.GetType();

            if (!type.IsValueType) {
                return true; // ref-type
            }

            if (Nullable.GetUnderlyingType(type) != null) {
                return true; // Nullable<T>
            }

            return false; // value-type
        }

        public static bool HasInterface<T>(this object obj) =>
            obj != null && HasInterface<T>(obj.GetType());

        public static bool HasInterface<T>(this object obj, [MaybeNull] out T typedObj) =>
            obj.HasInterface<T>()
            ? TeronisUtils.ReturnValue((T)obj, out typedObj, true)
            : TeronisUtils.ReturnValue(default!, out typedObj, false);
    }
}
