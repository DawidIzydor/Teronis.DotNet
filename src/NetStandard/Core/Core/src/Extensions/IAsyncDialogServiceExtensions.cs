﻿using System;
using System.Threading.Tasks;
using Teronis.Localization;
using Teronis.Models;
using Teronis.Services;

namespace Teronis.Extensions
{
    public static class IAsyncDialogServiceExtensions
    {
        public static Task<bool?> ShowErrorDialogAsync(this IAsyncDialogService dialogService, Exception error)
        {
            var caption = StringResources.AnErrorOccuredExclamation;
            DialogModel dialogModel;
#if DEBUG
            dialogModel = new DialogModel(error.ToString(), caption);
#else
            dialogModel = new DialogModel(error.Message, caption);
#endif
            return dialogService.ShowDialogAsync(dialogModel);
        }
    }
}
