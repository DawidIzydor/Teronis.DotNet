﻿using System;
using System.Collections.Generic;
using Teronis.Collections.Generic;
using Teronis.Extensions.NetStandard;

namespace Teronis.Data.TreeColumn.Core
{
    public abstract class TreeColumnSeekerBase<TreeColumnKeyType, TreeColumnValueType>
        where TreeColumnKeyType : ITreeColumnKey
        where TreeColumnValueType : ITreeColumnValue<TreeColumnKeyType>
    {
        public IDictionary<TreeColumnKey, ITreeColumnValue<TreeColumnKeyType>> TreeColumnDefinitionByKey { get; private set; }
        public Type MightOwnTreeColumnsType { get; private set; }

        public TreeColumnSeekerBase(Type mightOwnTreeColumnsType)
        {
            TreeColumnDefinitionByKey = new Dictionary<TreeColumnKey, ITreeColumnValue<TreeColumnKeyType>>();
            MightOwnTreeColumnsType = mightOwnTreeColumnsType;
        }

        protected abstract TreeColumnValueType instantiateTreeColumnValue(TreeColumnKeyType key, string path, int index);

        public IDictionary<TreeColumnKeyType, TreeColumnValueType> SearchTreeColumnDefinitions(IList<TreeColumnKeyType> treeColumnOrdering)
        {
            /// Cache for declaration paths of children that are decorated with <see cref="MightOwnTreeColumnsAttribute"/>
            var columnDefinitionsByParent = new List<(Type DeclaringType, string Path)>(new[] { (MightOwnTreeColumnsType, default(string)) });
            var treeColumnDefinitions = new OrderedDictionary<TreeColumnKeyType, TreeColumnValueType>();

            while (columnDefinitionsByParent.Count > 0) {
                string combinePath(string left, string right)
                {
                    string combinedPath;

                    if (left == null)
                        combinedPath = right;
                    else
                        combinedPath = left + "." + right;

                    return combinedPath;
                }

                var parent = columnDefinitionsByParent[0];
                var declaringType = parent.DeclaringType;
                var parentPath = parent.Path;

                // We cache declaration path children that are existing in the current declaration path
                foreach (var varInfo in declaringType.GetPropertyAttributeVariableInfos<MightOwnTreeColumnsAttribute>()) {
                    var propertyName = varInfo.VariableInfo.Name;
                    string combinedPath = combinePath(parentPath, varInfo.VariableInfo.Name);
                    columnDefinitionsByParent.Add((varInfo.VariableInfo.ValueType, combinedPath));
                }

                for (int index = 0; index < treeColumnOrdering.Count; index++) {
                    var orderedTreeColumnKey = treeColumnOrdering[index];

                    if (orderedTreeColumnKey.DeclarationType == declaringType) {
                        string combinedPath = combinePath(parentPath, orderedTreeColumnKey.VariableName);
                        var treeColumnValue = instantiateTreeColumnValue(orderedTreeColumnKey, combinedPath, index);

                        if (index < treeColumnDefinitions.Count)
                            treeColumnDefinitions.Insert(index, orderedTreeColumnKey, treeColumnValue);
                        else
                            treeColumnDefinitions.Add(orderedTreeColumnKey, treeColumnValue);
                    }
                }

                columnDefinitionsByParent.RemoveAt(0);
            }

            return treeColumnDefinitions;
        }
    }
}