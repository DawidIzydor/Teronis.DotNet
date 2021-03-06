﻿

namespace Teronis.ObjectModel.Updates
{
    public interface IContentUpdatingEventArgs<out ContentType>
    {
        IContentUpdate<ContentType> Update { get; }
        /// <summary>
        /// If true, then the update process will be canceled.
        /// </summary>
        bool Handled { get; set; }
    }
}
