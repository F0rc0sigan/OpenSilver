﻿
//===============================================================================
//
//  IMPORTANT NOTICE, PLEASE READ CAREFULLY:
//
//  ● This code is dual-licensed (GPLv3 + Commercial). Commercial licenses can be obtained from: http://cshtml5.com
//
//  ● You are NOT allowed to:
//       – Use this code in a proprietary or closed-source project (unless you have obtained a commercial license)
//       – Mix this code with non-GPL-licensed code (such as MIT-licensed code), or distribute it under a different license
//       – Remove or modify this notice
//
//  ● Copyright 2019 Userware/CSHTML5. This code is part of the CSHTML5 product.
//
//===============================================================================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if MIGRATION
#else
using Windows.UI.Xaml;
#endif

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Provides information for the System.Windows.Controls.Validation.Error attached
    /// event.
    /// </summary>
    public class ValidationErrorEventArgs : RoutedEventArgs
    {
        private ValidationErrorEventAction _action;

        /// <summary>
        /// Gets a value that indicates whether the error is a new error or an existing
        /// error that has now been cleared.
        /// </summary>
        public ValidationErrorEventAction Action
        {
            get { return _action; }
            internal set { _action = value; }
        }


        private ValidationError _error;

        /// <summary>
        /// Gets the error that caused this System.Windows.Controls.Validation.Error
        /// event.
        /// </summary>
        public ValidationError Error
        {
            get { return _error; }
            internal set { _error = value; }
        }

        ///// <summary>
        ///// Invokes the specified handler in a type-specific way on the specified object.
        ///// </summary>
        ///// <param name="genericHandler">The generic handler to call in a type-specific way.</param>
        ///// <param name="genericTarget">The object to invoke the handler on.</param>
        //protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget);
    }
}