﻿using System;
using System.Collections.Specialized;
using Teronis.Collections.CollectionChanging;
using Teronis.Data;
using Teronis.Extensions;

namespace Teronis.Collections.Synchronization
{
    public class CollectionItemConversionParentBehaviour<OriginalItemType, OriginalContentType, ConvertedItemType>
        where OriginalItemType : IHaveKnownParents
    {
        public INotifyCollectionChangeConversionApplied<ConvertedItemType, OriginalItemType, OriginalItemType> CollectionChangeConversionNotifer { get; private set; }

        public CollectionItemConversionParentBehaviour(INotifyCollectionChangeConversionApplied<ConvertedItemType, OriginalItemType, OriginalItemType> collectionChangeConversionNotifer)
        {
            CollectionChangeConversionNotifer = collectionChangeConversionNotifer;
            CollectionChangeConversionNotifer.CollectionChangeConversionApplied += ConvertedCollectionChangeNotifer_CollectionChangeConversionApplied;
        }

        private void ConvertedCollectionChangeNotifer_CollectionChangeConversionApplied(object sender, CollectionChangeConversionAppliedEventArgs<ConvertedItemType, OriginalItemType, OriginalItemType> args)
        {
            var convertedContentContentChange = args.ConvertedCollectionChangeBundle.ContentContentChange;
            var convertedItemItemChange = args.ConvertedCollectionChangeBundle.ItemItemChange;

            if (convertedContentContentChange.Action != convertedItemItemChange.Action) {
                CollectionChangeConversionThrowHelper.ThrowChangeActionMismatchException();
            }

            var action = convertedContentContentChange.Action;

            switch (action) {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Add:
                    var newConvertedContentContentItems = convertedContentContentChange.NewItems ??
                        throw new ArgumentException("No new converted content-content-items were given that can be attached as wanted parents.");

                    var newConvertedItemItemItems = convertedItemItemChange.NewItems ??
                        throw new ArgumentException("No new converted item-item-items were given that can be attached as wanted parents.");

                    var originalItemsEnumerator = newConvertedContentContentItems.GetEnumerator();
                    var convertedItemsEnumerator = newConvertedItemItemItems.GetEnumerator();

                    while (originalItemsEnumerator.MoveNext() && convertedItemsEnumerator.MoveNext()) {
                        var originalItem = originalItemsEnumerator.Current;

                        switch (action) {
                            case NotifyCollectionChangedAction.Remove:
                                originalItem.DetachKnownWantParentsHandler(this);
                                break;
                            case NotifyCollectionChangedAction.Add:
                                var convertedItem = convertedItemsEnumerator.Current ??
                                    throw new ArgumentException("One item of the new converted item-item-items is null and cannot be attached as wanted parent.");

                                void OriginalItem_WantParents(object s, HavingParentsEventArgs e)
                                    => e.AttachParentParents(convertedItem);

                                originalItem.AttachKnownWantParentsHandler(this, OriginalItem_WantParents);
                                break;
                        }
                    }

                    break;
            }
        }
    }
}
