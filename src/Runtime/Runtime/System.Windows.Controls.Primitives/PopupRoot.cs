﻿

/*===================================================================================
* 
*   Copyright (c) Userware/OpenSilver.net
*      
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/

using System;
using System.Collections.Generic;
using CSHTML5.Internal;
using DotNetForHtml5.Core;

#if MIGRATION
using System.Windows.Input;
#else
using Windows.Foundation;
using Windows.UI.Xaml.Input;
#endif

#if MIGRATION
namespace System.Windows.Controls.Primitives
#else
namespace Windows.UI.Xaml.Controls.Primitives
#endif
{
    internal sealed class PopupRoot : FrameworkElement
    {
        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        internal override int VisualChildrenCount
        {
            get
            {
                if (Content == null)
                {
                    return 0;
                }

                return 1;
            }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        internal override UIElement GetVisualChild(int index)
        {
            if (Content == null || index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return Content;
        }

        internal string INTERNAL_UniqueIndentifier { get; set; }

        internal Popup INTERNAL_LinkedPopup { get; set; }

        internal PopupRoot(string uniqueIdentifier, Window parentWindow, Popup popup)
        {
            INTERNAL_UniqueIndentifier = uniqueIdentifier;
            INTERNAL_ParentWindow = parentWindow;
            INTERNAL_LinkedPopup = popup;
        }

        internal void SetLayoutSize()
        {
            if (!UseCustomLayout) return;

            InvalidateMeasure();
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Arrange(new Rect(new Point(), DesiredSize));
        }

        /// <summary>
        /// Gets or sets the visual root of a popup
        /// </summary>
        public UIElement Content
        {
            get { return (UIElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Identifies the PopupRoot.Content dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                nameof(Content),
                typeof(UIElement),
                typeof(PopupRoot),
                new PropertyMetadata(null, OnContentChanged));

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PopupRoot popupRoot = (PopupRoot)d;
            popupRoot.TemplateChild = null;
            if (popupRoot.IsConnectedToLiveTree)
            {
                popupRoot.InvalidateMeasureInternal();
            }
        }

#if MIGRATION
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
#else
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
#endif
        {
#if MIGRATION
            base.OnMouseLeftButtonDown(e);
#else
            base.OnPointerPressed(e);
#endif

            // Note: If a popup has StayOpen=True, the value of "StayOpen" of its parents is ignored.
            // In other words, the parents of a popup that has StayOpen=True will always stay open
            // regardless of the value of their "StayOpen" property.

            HashSet<Popup> listOfPopupThatMustBeClosed = new HashSet<Popup>();
            List<PopupRoot> popupRootList = new List<PopupRoot>();

            foreach (object obj in INTERNAL_PopupsManager.GetAllRootUIElements())
            {
                if (obj is PopupRoot)
                {
                    PopupRoot root = (PopupRoot)obj;
                    popupRootList.Add(root);

                    if (root.INTERNAL_LinkedPopup != null)
                        listOfPopupThatMustBeClosed.Add(root.INTERNAL_LinkedPopup);
                }
            }

            // We determine which popup needs to stay open after this click
            foreach (PopupRoot popupRoot in popupRootList)
            {
                if (popupRoot.INTERNAL_LinkedPopup != null)
                {
                    // We must prevent all the parents of a popup to be closed when:
                    // - this popup is set to StayOpen
                    // - or the click happend in this popup

                    Popup popup = popupRoot.INTERNAL_LinkedPopup;

                    if (popup.StayOpen)
                    {
                        do
                        {
                            if (!listOfPopupThatMustBeClosed.Contains(popup))
                                break;

                            listOfPopupThatMustBeClosed.Remove(popup);

                            popup = popup.ParentPopup;

                        } while (popup != null);
                    }
                }
            }

            foreach (Popup popup in listOfPopupThatMustBeClosed)
            {
                var args = new OutsideClickEventArgs();
                popup.OnOutsideClick(args);
                if (!args.Handled)
                {
                    popup.CloseFromAnOutsideClick();
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            int count = VisualChildrenCount;
            if (count > 0 && INTERNAL_ParentWindow != null)
            {
                UIElement child = GetVisualChild(0);
                if (child != null)
                {
                    child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    return child.DesiredSize;
                }
            }
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int count = VisualChildrenCount;
            if (count > 0 && INTERNAL_ParentWindow != null)
            {
                UIElement child = GetVisualChild(0);
                if (child != null)
                {
                    child.Arrange(new Rect(finalSize));
                }
            }
            return finalSize;
        }

        internal override FrameworkTemplate TemplateCache
        {
            get => DefaultTemplate;
            set { }
        }

        internal override FrameworkTemplate TemplateInternal => DefaultTemplate;

        private static UseContentTemplate DefaultTemplate { get; } = new UseContentTemplate();

        private sealed class UseContentTemplate : FrameworkTemplate
        {
            public UseContentTemplate()
            {
                Seal();
            }

            internal override bool BuildVisualTree(FrameworkElement container)
            {
                container.TemplateChild = ((PopupRoot)container).Content as FrameworkElement;
                return false;
            }
        }
    }
}
