﻿using System.Windows;
using System.Windows.Controls;
using Teronis.Wpf.ViewModels;

namespace Teronis.Windows.Templating
{
    public class DialogFooterTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OkTemplate { get; set; }
        public DataTemplate YesNoTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {

            if (item is DialogFooterOkViewModel) {
                return OkTemplate;
            }

            if (item is DialogFooterYesNoViewModel) {
                return YesNoTemplate;
            } else {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
