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
namespace System.Windows.Navigation
#else
namespace Windows.UI.Xaml.Navigation
#endif
{
    /// <summary>
    /// Provides data for the NavigationFailed event of the NavigationService class and the NavigationFailed event of the Frame class.</summary>
    public sealed class NavigationFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the NavigationFailedEventArgs.
        /// </summary>
        public NavigationFailedEventArgs(Exception exception, bool handled, Uri uri)
        {
            _exception = exception;
            _handled = handled;
            _uri = uri;
        }

        private Exception _exception;
        private bool _handled;
        private Uri _uri;

        /// <summary>
        /// Gets the error from the failed navigation.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the failure event has been handled.
        /// </summary>
        public bool Handled
        {
            get { return _handled; }
            set { _handled = value; }
        }


        /// <summary>
        /// Gets the uniform resource identifier (URI) for the content that could not be navigated to.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }
    }
}