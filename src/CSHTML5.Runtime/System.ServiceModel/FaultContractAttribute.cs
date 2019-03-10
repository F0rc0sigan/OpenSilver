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


#if WCF_STACK || BRIDGE

using System.Net.Security;

namespace System.ServiceModel
{
    //[AttributeUsage(ServiceModelAttributeTargets.OperationContract, AllowMultiple = true, Inherited = false)]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class FaultContractAttribute : Attribute
    {
        string action;
        string name;
        string ns;
        Type type;
        ProtectionLevel protectionLevel = ProtectionLevel.None;
        bool hasProtectionLevel = false;

        public FaultContractAttribute(Type detailType)
        {
            if (detailType == null)
                throw new ArgumentNullException("detailType");

            this.type = detailType;
        }

        public Type DetailType
        {
            get { return this.type; }
        }

        public string Action
        {
            get { return this.action; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                this.action = value;
            }
        }

        public string Name
        {
            get { return this.name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value == string.Empty)
                    throw new ArgumentOutOfRangeException("value");
                this.name = value;
            }
        }

        public string Namespace
        {
            get { return this.ns; }
            set
            {
                //if (!string.IsNullOrEmpty(value))
                //    NamingHelper.CheckUriProperty(value, "Namespace");
                this.ns = value;
            }
        }

        internal const string ProtectionLevelPropertyName = "ProtectionLevel";
        public ProtectionLevel ProtectionLevel
        {
            get
            {
                return this.protectionLevel;
            }
            set
            {
                //if (!ProtectionLevelHelper.IsDefined(value))
                //    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
                this.protectionLevel = value;
                this.hasProtectionLevel = true;
            }
        }

        public bool HasProtectionLevel
        {
            get { return this.hasProtectionLevel; }
        }
    }
}

#endif