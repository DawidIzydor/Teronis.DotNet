﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Teronis.Extensions.NetStandard;
using Teronis.Reflection;
using Teronis.Libraries.NetStandard;

namespace Teronis.Tools.NetStandard
{
    public static class TypeTools
    {
        #region variable info

        /// <param name="originalVarInfo">Pass either <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>.</param>
        public static bool TryToVariableInfo(dynamic originalVarInfo, Func<string> getName, Func<Type> getValueType, out VariableInfo varInfo)
        {
            if (originalVarInfo == null)
                varInfo = null;
            else {
                varInfo = new VariableInfo((object)originalVarInfo, getName(),
                    Nullable.GetUnderlyingType(getValueType()) ?? getValueType(),
                    (Type)originalVarInfo.DeclaringType, (refObj) => originalVarInfo.GetValue(refObj),
                    (refObj, val) => originalVarInfo.SetValue(refObj, val),
                    (type, inherit) => originalVarInfo.IsDefined(type, inherit),
                    (attrType, inherit) => CustomAttributeExtensions.GetCustomAttributes(originalVarInfo, attrType, inherit));
            }

            return varInfo != null;
        }

        public static IEnumerable<VariableInfo> GetVariableInfos(Func<Type, VariableInfoSettings, IEnumerable<VariableInfo>> getVariableInfos, Type beginAt, VariableInfoSettings settings = null, Type interruptAt = null)
        {
            settings = settings ?? new VariableInfoSettings();

            if (settings.Flags.HasFlag(BindingFlags.DeclaredOnly))
                interruptAt = beginAt.BaseType;
            else {
                settings.Flags |= BindingFlags.DeclaredOnly;
                interruptAt = interruptAt ?? typeof(object);
            }

            foreach (var type in beginAt.GetBaseTypes(interruptAt))
                foreach (var varInfo in getVariableInfos(type, settings))
                    yield return varInfo;
        }

        /// <param name="originalVarInfo">Pass <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>.</param>
        public static bool TryToAttributeVariableInfo<T>(dynamic originalVarInfo, out AttributeVariableInfo<T> attrVarInfo, bool? getCustomAttributesInherit = null) where T : Attribute
        {
            if (originalVarInfo != null) {
                if (originalVarInfo.IsDefined(typeof(T), false)) {
                    VariableInfo varInfo;

                    var hasFound = originalVarInfo is FieldInfo
                        ? FieldInfoExtensions.TryToVariableInfo(originalVarInfo, out varInfo)
                        : (originalVarInfo is PropertyInfo
                            ? PropertyInfoExtensions.TryToVariableInfo(originalVarInfo, out varInfo)
                            : throw new ArgumentException($"The original variable info could not be converted neither to a field nor to a property."));

                    attrVarInfo = new AttributeVariableInfo<T>(varInfo, getCustomAttributesInherit);
                    return true;
                }
            }

            attrVarInfo = null;
            return false;
        }

        /// <param name="originalVarInfo">Pass <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>.</param>
        public static AttributeVariableInfo<T> TryToAttributeVariableInfo<T>(object originalVarInfo, bool? getCustomAttributesInherit = null) where T : Attribute
        {
            TryToAttributeVariableInfo(originalVarInfo, out AttributeVariableInfo<T> attrVarInfo, getCustomAttributesInherit);
            return attrVarInfo;
        }

        #endregion

        public static bool IsNullable(Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static object InstantiateUninitializedObject(this Type type)
            => typeof(Instantiator<>).MakeGenericType(type).GetMethod(nameof(Instantiator<object>.Instantiate), BindingFlags.Public | BindingFlags.Static).Invoke(null, null);

        public static object GetDefault(Type type) {
            if (type.IsValueType)
                return InstantiateUninitializedObject(type);
            else
                return null;
        }
    }
}
