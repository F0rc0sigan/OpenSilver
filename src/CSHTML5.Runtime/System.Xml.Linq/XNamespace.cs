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

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents an XML namespace. This class cannot be inherited.
    /// </summary>
    public sealed class XNamespace
    {
        /// <summary>
        /// Combines an System.Xml.Linq.XNamespace object with a local name to create
        /// an System.Xml.Linq.XName.
        /// </summary>
        /// <param name="ns">An System.Xml.Linq.XNamespace that contains the namespace.</param>
        /// <param name="localName">A System.String that contains the local name.</param>
        /// <returns>The new System.Xml.Linq.XName constructed from the namespace and local name.</returns>
        public static XName operator +(XNamespace ns, string localName)
        {
            return ns.GetName(localName);
        }

        /// <summary>
        /// Converts a string containing a Uniform Resource Identifier (URI) to an System.Xml.Linq.XNamespace.
        /// </summary>
        /// <param name="namespaceName">A System.String that contains the namespace URI.</param>
        /// <returns>An System.Xml.Linq.XNamespace constructed from the URI string.</returns>
        public static implicit operator XNamespace(string namespaceName)
        {
            return XNamespace.Get(namespaceName);
        }

        string _namespaceName;
        /// <summary>
        /// Gets the Uniform Resource Identifier (URI) of this namespace.
        /// </summary>
        public string NamespaceName
        {
            get
            {
                return _namespaceName;
            }
            internal set
            {
                _namespaceName = value;
            }
        }

        /// <summary>
        /// Gets an System.Xml.Linq.XNamespace for the specified Uniform Resource Identifier
        /// (URI).
        /// </summary>
        /// <param name="namespaceName">A System.String that contains a namespace URI.</param>
        /// <returns>An System.Xml.Linq.XNamespace created from the specified URI.</returns>
        public static XNamespace Get(string namespaceName)
        {
            XNamespace xNamespace = new XNamespace();
            xNamespace.NamespaceName = namespaceName;
            return xNamespace;
        }
     
        /// <summary>
        /// Returns an System.Xml.Linq.XName object created from this System.Xml.Linq.XNamespace
        /// and the specified local name.
        /// </summary>
        /// <param name="localName">A System.String that contains a local name.</param>
        /// <returns>
        /// An System.Xml.Linq.XName created from this System.Xml.Linq.XNamespace and
        /// the specified local name.
        /// </returns>
        public XName GetName(string localName)
        {
            XName xName = new XName();
            xName.LocalName = localName;
            xName.Namespace = this;
            return xName;
        }
       
        /// <summary>
        /// Returns the URI of this System.Xml.Linq.XNamespace.
        /// </summary>
        /// <returns>The URI of this System.Xml.Linq.XNamespace.</returns>
        public override string ToString()
        {
            return NamespaceName;
        }


        /// <summary>
        /// Returns a value indicating whether two instances of System.Xml.Linq.XNamespace
        /// are not equal.
        /// </summary>
        /// <param name="left">The first System.Xml.Linq.XNamespace to compare.</param>
        /// <param name="right">The second System.Xml.Linq.XNamespace to compare.</param>
        /// <returns>A System.Boolean that indicates whether left and right are not equal.</returns>
        public static bool operator !=(XNamespace left, XNamespace right)
        {
            if (object.ReferenceEquals(left, null))
            {
                if (object.ReferenceEquals(right, null))
                    return false;
                else
                    return true;
            }
            else if (object.ReferenceEquals(right, null))
            {
                return true;
            }
            return left._namespaceName != right._namespaceName;
        }

        /// <summary>
        /// Returns a value indicating whether two instances of System.Xml.Linq.XNamespace
        /// are equal.
        /// </summary>
        /// <param name="left">The first System.Xml.Linq.XNamespace to compare.</param>
        /// <param name="right">The second System.Xml.Linq.XNamespace to compare.</param>
        /// <returns>A System.Boolean that indicates whether left and right are equal.</returns>
        public static bool operator ==(XNamespace left, XNamespace right)
        {
            
            return !(left != right);
        }

        #region not implemented


        ///// <summary>
        ///// Determines whether the specified System.Xml.Linq.XNamespace is equal to the
        ///// current System.Xml.Linq.XNamespace.
        ///// </summary>
        ///// <param name="obj">The System.Xml.Linq.XNamespace to compare to the current System.Xml.Linq.XNamespace.</param>
        ///// <returns>
        ///// A System.Boolean that indicates whether the specified System.Xml.Linq.XNamespace
        ///// is equal to the current System.Xml.Linq.XNamespace.
        ///// </returns>
        //public override bool Equals(object obj);

        ///// <summary>
        ///// Gets a hash code for this System.Xml.Linq.XNamespace.
        ///// </summary>
        ///// <returns>An System.Int32 that contains the hash code for the System.Xml.Linq.XNamespace.</returns>
        //public override int GetHashCode();

        ///// <summary>
        ///// Gets the System.Xml.Linq.XNamespace object that corresponds to no namespace.
        ///// </summary>
        //public static XNamespace None { get; }

        ///// <summary>
        ///// Gets the System.Xml.Linq.XNamespace object that corresponds to the XML URI
        ///// (http://www.w3.org/XML/1998/namespace).
        ///// </summary>
        //public static XNamespace Xml { get; }

        ///// <summary>
        ///// Gets the System.Xml.Linq.XNamespace object that corresponds to the xmlns
        ///// URI (http://www.w3.org/2000/xmlns/).
        ///// </summary>
        //public static XNamespace Xmlns { get; } //todo: if we add this, use this instead of DataContractSerializer_Helpers.XMLNS_NAMESPACE?

        #endregion
    }
}