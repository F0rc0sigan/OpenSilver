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


#if !BRIDGE
using JSIL.Meta;
using System.Dynamic;
#else
using Bridge;
#endif
using System;
using CSHTML5.Internal;
using System.Collections.Generic;

public static partial class CSharpXamlForHtml5
{
    public static partial class DomManagement
    {
        public static partial class Types
        {
            // Verify : this class is not supposed to be called by bridge.NET itself
            // Note: this class is intented to be used by the Simulator only, not when compiled to JavaScript.

#if !BRIDGE
            [JSIgnore]
            public class DynamicDomElement : DynamicObject
#else
            //class used for C# compilation
            [External]
            public class DynamicDomElement
#endif
            {
                Dictionary<Tuple<string, Action<object>>, HtmlEventProxy> _eventNameAndHandlerToHtmlEventProxy = null;

                INTERNAL_HtmlDomElementReference _domElementRef;

                internal DynamicDomElement(INTERNAL_HtmlDomElementReference domElementRef)
                {
                    _domElementRef = domElementRef;
                }

#if !BRIDGE
                public override bool TrySetMember(SetMemberBinder binder, object value)
                {
                    INTERNAL_HtmlDomManager.SetDomElementAttribute(_domElementRef, binder.Name, value, forceSimulatorExecuteImmediately: true);
                    return true;
                }

                public override bool TryGetMember(GetMemberBinder binder, out object result)
                {
                    string attributeName = binder.Name;
                    if (attributeName == "style")
                        result = INTERNAL_HtmlDomManager.GetDomElementStyleForModification(_domElementRef);
                    else
                        result = INTERNAL_HtmlDomManager.GetDomElementAttribute(_domElementRef, attributeName);
                    return true;
                }
#endif

                public void removeAttribute(string attributeName) //todo: generalize to all the methods (thanks to DynamicObject)
                {
                    INTERNAL_HtmlDomManager.RemoveDomElementAttribute(_domElementRef, attributeName, forceSimulatorExecuteImmediately: true);
                }

                public void addEventListener(string eventName, Action<object> handler)
                {
                    HtmlEventProxy proxy = INTERNAL_EventsHelper.AttachToDomEvents(eventName, _domElementRef, handler);
                    if (_eventNameAndHandlerToHtmlEventProxy == null)
                    {
                        _eventNameAndHandlerToHtmlEventProxy = new Dictionary<Tuple<string, Action<object>>, HtmlEventProxy>();
                    }
                    _eventNameAndHandlerToHtmlEventProxy.Add(new Tuple<string, Action<object>>(eventName, handler), proxy);
                }

                public void removeEventListener(string eventName, Action<object> handler)
                {
                    Tuple<string, Action<object>> key = new Tuple<string, Action<object>>(eventName, handler);
                    if (_eventNameAndHandlerToHtmlEventProxy !=  null && _eventNameAndHandlerToHtmlEventProxy.ContainsKey(key))
                    {
                        INTERNAL_EventsHelper.DetachEvent(eventName, _domElementRef, _eventNameAndHandlerToHtmlEventProxy[key], handler);
                    }
                    //else do nothing.
                }

                public void appendChild(object domElementRef)
                {
                    INTERNAL_HtmlDomManager.AppendChild_ForUseByPublicAPIOnly_SimulatorOnly(((DynamicDomElement)domElementRef)._domElementRef, this._domElementRef);
                }


                DynamicDomElementChildrenCollection _childNodes;
                public DynamicDomElementChildrenCollection childNodes
                {
                    get
                    {
                        if (_childNodes == null)
                        {
                            _childNodes = new DynamicDomElementChildrenCollection(this._domElementRef);
                        }
                        return _childNodes;
                    }
                }
            }
        }
    }
}
