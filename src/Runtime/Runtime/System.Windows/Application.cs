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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Windows;
using System.Windows.Resources;
using CSHTML5;
using CSHTML5.Internal;
using DotNetForHtml5.Core;
using OpenSilver.Internal;
using OpenSilver.Internal.Xaml;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xaml.Markup;

#if MIGRATION
using System.ApplicationModel.Activation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
#else
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
#endif

#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    /// <summary>
    /// Encapsulates the app and its available services.
    /// </summary>
    public partial class Application
    {
        private static Dictionary<string, string> _resourcesCache = null;
        private ApplicationLifetimeObjectsCollection lifetime_objects;
        private Window _mainWindow;
        private ResourceDictionary _resources;
        private Dictionary<object, object> _implicitResourcesCache;

        // Says if App.Resources has any implicit styles
        internal bool HasImplicitStylesInResources { get; set; }        

        /// <summary>
        /// Gets the Application object for the current application.
        /// </summary>
        public static Application Current { get; private set; }

        internal INTERNAL_XamlResourcesHandler XamlResourcesHandler { get; } = new INTERNAL_XamlResourcesHandler();

        internal ITextMeasurementService TextMeasurementService { get; private set; }

        public Application()
        {
            // Keep a reference to the app:
            Application.Current = this;

            // Initialize Deployment
            _ = Deployment.Current;

            AppParams = GetAppParams();

            new DOMEventManager(GetWindow, "unload", ProcessOnExit).AttachToDomEvents();

            // In case of a redirection from Microsoft AAD, when running in the Simulator, we re-instantiate the application. We need to reload the JavaScript files because they are no longer in the HTML DOM due to the AAD redirection:
            INTERNAL_InteropImplementation.ResetLoadedFilesDictionaries();

            // we change the resource manager for every resource registered
            ClientSideResourceRegister.Startup();

            // Keep a reference to the startup assembly:
            StartupAssemblyInfo.StartupAssembly = this.GetType().Assembly;

            // Remember whether we are in "SL Migration" mode or not:
#if MIGRATION
            OpenSilver.Interop.ExecuteJavaScriptVoid(@"document.isSLMigration = true", false);
#else
            OpenSilver.Interop.ExecuteJavaScriptVoid(@"document.isSLMigration = false", false);
#endif

            // Get default font-family from css
            INTERNAL_FontsHelper.DefaultCssFontFamily = OpenSilver.Interop.ExecuteJavaScriptString(
                "window.getComputedStyle(document.getElementsByTagName('body')[0]).getPropertyValue(\"font-family\")");


            TextMeasurementService = new TextMeasurementService();

            // Initialize the window:
            if (_mainWindow == null) // Note: it could be != null if the user clicks "Restart" from the Simulator advanced options.
            {
                _mainWindow = new Window(true, true);
                Window.Current = _mainWindow;
                object applicationRootDomElement = INTERNAL_HtmlDomManager.GetApplicationRootDomElement();
                _mainWindow.AttachToDomElement(applicationRootDomElement);

#if !CSHTML5NETSTANDARD
                // Workaround an issue on Firefox where the UI disappears if the window is resized and on some other occasions:
                if (INTERNAL_HtmlDomManager.IsFirefox())
                {
                    _mainWindow.SizeChanged += MainWindow_SizeChanged;
                    _timerForWorkaroundFireFoxIssue.Interval = new TimeSpan(0, 0, 2);
                    _timerForWorkaroundFireFoxIssue.Tick += TimerForWorkaroundFireFoxIssue_Tick;
                }
#endif
            }

            // We call the "Startup" event and the "OnLaunched" method using the Dispatcher, because usually the user registers the "Startup" event in the constructor of the "App.cs" class, which is derived from "Application.cs", and therefore when we arrive here the event is not yet registered. Executing the code in the Dispatcher ensures that the constructor of the "App.cs" class has finished before running the code.
#if MIGRATION
            Dispatcher
#else
            CoreDispatcher
#endif
                .INTERNAL_GetCurrentDispatcher().BeginInvoke(() =>
            {
                StartAppServices();

                // Raise the "Startup" event:
                if (this.Startup != null)
                    Startup(this, new StartupEventArgs());

                // Call the "OnLaunched" method:
                this.OnLaunched(new LaunchActivatedEventArgs());
            });

        }

        public IList ApplicationLifetimeObjects
        {
            get
            {
                if (lifetime_objects == null)
                {
                    lifetime_objects = new ApplicationLifetimeObjectsCollection();
                }

                return lifetime_objects;
            }
        }

        private void StartAppServices()
        {
            foreach (IApplicationService appService in ApplicationLifetimeObjects)
            {
                if (appService != null)
                {
                    try
                    {
                        appService.StartService(new ApplicationServiceContext());
                    }
                    catch (Exception ex)
                    {
                        OnUnhandledException(ex, false);
                    }
                }
            }
        }

        internal IDictionary<string, string> AppParams { get; }

        private struct HTMLParam
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private static IDictionary<string, string> GetAppParams()
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            HTMLParam[] paramsArray;
            try
            {
                paramsArray = JsonSerializer.Deserialize<HTMLParam[]>(
                    OpenSilver.Interop.ExecuteJavaScriptString("document.getAppParams()"));
            }
            catch
            {
                paramsArray = Array.Empty<HTMLParam>();
            }

            foreach (HTMLParam p in paramsArray)
            {
                if (p.Name != null)
                {
                    parameters[p.Name] = p.Value;
                }
            }

            return new ReadOnlyDictionary<string, string>(parameters);
        }

        #region Work around an issue on Firefox where the UI disappears if the window is resized and on some other occasions:

#if !CSHTML5NETSTANDARD
        DispatcherTimer _timerForWorkaroundFireFoxIssue = new DispatcherTimer();
        ChildWindow _childWindowForWorkaroundFireFoxIssue;

        void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            INTERNAL_WorkAroundFirefoxDisappearingUI();
        }

        public void INTERNAL_WorkAroundFirefoxDisappearingUI() //todo: move this method to the "PublicAPI" folder?
        {
            // Work around an issue on Firefox where the UI disappears if the window is resized and on some other occasions:
            if (_childWindowForWorkaroundFireFoxIssue == null) // Avoids entering multiple times on a single resize.
            {
                _childWindowForWorkaroundFireFoxIssue = new ChildWindow() { Opacity = 0 };
                _childWindowForWorkaroundFireFoxIssue.Show();
                _timerForWorkaroundFireFoxIssue.Start();
            }
        }

#if MIGRATION
        void TimerForWorkaroundFireFoxIssue_Tick(object sender, EventArgs e)
#else
        void TimerForWorkaroundFireFoxIssue_Tick(object sender, object e)
#endif
        {
            // Work around an issue on Firefox where the UI disappears if the window is resized and on some other occasions:
            _childWindowForWorkaroundFireFoxIssue.Dispatcher.BeginInvoke(() =>
            {
                _timerForWorkaroundFireFoxIssue.Stop();
                _childWindowForWorkaroundFireFoxIssue.Close();
                _childWindowForWorkaroundFireFoxIssue = null;
            });
        }
#endif

        #endregion

        /// <summary>
        /// Injects the "DataContractSerializer" into the "XmlSerializer" (read note in the "XmlSerializer" implementation to understand why).
        /// </summary>
        static void InjectDataContractSerializerIntoXmlSerializer()
        {

#if !FOR_DESIGN_TIME && !CSHTML5NETSTANDARD && !BRIDGE //Note: in case of "Design Time", we reference the .NET Framework version of "System.xml.dll", so the code below would not compile because the specified members of XmlSerializer would be missing.
            XmlSerializer.MethodToSerializeUsingDataContractSerializer = (obj, type) => { return (new DataContractSerializer(type, useXmlSerializerFormat: true)).SerializeToString(obj); };
            XmlSerializer.MethodToDeserializeUsingDataContractSerializer = (str, type) => { return (new DataContractSerializer(type, useXmlSerializerFormat: true)).DeserializeFromString(str); };
#endif
        }

#if !CSHTML5NETSTANDARD
#if !BRIDGE
        [JSIL.Meta.JSIgnore]
#else
        [Bridge.External]
#endif
        static void RedirectAlertToMessageBox_SimulatorOnly()
        {
            // Fix the freezing of the Simulator when calling 'alert' using the "Interop.ExecuteJavaScript()" method by redirecting the JavaScript "alert" to the Simulator message box:
            CSHTML5.Interop.ExecuteJavaScript(@"window.alert =  function(msg){ $0(msg); }", (Action<object>)((msg) => { MessageBox.Show(msg.ToString()); }));
        }
#endif

        /// <summary>
        /// Gets a collection of application-scoped resources, such as styles, templates,
        /// and brushes.
        /// </summary>
        [Ambient]
        public ResourceDictionary Resources
        {
            get
            {
                if (_resources == null)
                {
                    _resources = new ResourceDictionary();
                    _resources.AddOwner(this);
                }
                return _resources;
            }
            set
            {
                ResourceDictionary oldValue = _resources;
                _resources = value;

                if (oldValue != null)
                {
                    // This app is no longer an owner for the old ResourceDictionary
                    oldValue.RemoveOwner(this);
                }

                if (value != null)
                {
                    if (!value.ContainsOwner(this))
                    {
                        // This app is an owner for the new ResourceDictionary
                        value.AddOwner(this);
                    }
                }

                if (oldValue != value)
                {
                    InvalidateStyleCache(new ResourcesChangeInfo(oldValue, value));

                    //// this notify all window in the app that Application resources changed
                    // InvalidateResourceReferences(new ResourcesChangeInfo(oldValue, value));
                }
            }
        }

        internal bool HasResources
        {
            get
            {
                ResourceDictionary resources = _resources;
                return (resources != null &&
                        ((resources.Count > 0) || (resources.MergedDictionaries.Count > 0)));
            }
        }

        internal object FindImplicitResourceInternal(object resourceKey)
        {
            if (_implicitResourcesCache?.TryGetValue(resourceKey, out object resource) ?? false)
            {
                return resource;
            }

            return null;
        }

        internal void InvalidateStyleCache(ResourcesChangeInfo info)
        {
            if (info.Key is not null)
            {
                switch (info.Key)
                {
                    case Type:
                    case DataTemplateKey:
                        object resource = Resources[info.Key];
                        if (resource is null)
                        {
                            _implicitResourcesCache?.Remove(info.Key);
                        }
                        else
                        {
                            _implicitResourcesCache ??= new();
                            _implicitResourcesCache[info.Key] = resource;
                        }
                        break;
                }
            }
            else if (info.IsCatastrophicDictionaryChange ||
                (info.NewDictionary != null && (info.NewDictionary.HasImplicitStyles || info.NewDictionary.HasImplicitDataTemplates)) ||
                (info.OldDictionary != null && (info.OldDictionary.HasImplicitStyles || info.OldDictionary.HasImplicitDataTemplates)))
            {
                _implicitResourcesCache = HasResources && (Resources.HasImplicitStyles || Resources.HasImplicitDataTemplates) ?
                    ResourceDictionary.Helpers.BuildImplicitResourcesCache(Resources) :
                    null;
            }
        }

        /// <summary>
        /// Invoked when the application is launched. Override this method to perform
        /// application initialization and to display initial content in the associated
        /// Window.
        /// </summary>
        /// <param name="args">Event data for the event.</param>
        protected virtual void OnLaunched(LaunchActivatedEventArgs args)
        {
        }

        /// <summary>
        /// Occurs when an application is started.
        /// </summary>
        public event StartupEventHandler Startup;

        //protected virtual void InitializeComponent()
        //{
        //}

        /// <summary>
        /// Gets or sets the main application UI. This is an alias for: Window.Current.Content
        /// </summary>
        public UIElement RootVisual
        {
            get
            {
                return Window.Current.Content;
            }
            set
            {
                Window.Current.Content = value as FrameworkElement;
            }
        }

        /// <summary>
        /// Occurs just before an application shuts down and cannot be canceled.
        /// </summary>
        //public event EventHandler Exit;

        //returns the html window element
        internal object GetWindow() => INTERNAL_HtmlDomManager.GetHtmlWindow();

        /// <summary>
        /// Gets the application main window.
        /// </summary>
        public Window MainWindow
        {
            get
            {
                return Window.Current;
            }
        }

        // Exceptions:
        //   System.ArgumentNullException:
        //     The System.Uri that is passed to System.Windows.Application.GetResourceStream(System.Uri)
        //     is null.
        //
        //   System.ArgumentException:
        //     The System.Uri.OriginalString property of the System.Uri that is passed to
        //     System.Windows.Application.GetResourceStream(System.Uri) is null.
        //
        //   System.ArgumentException:
        //     The System.Uri that is passed to System.Windows.Application.GetResourceStream(System.Uri)
        //     is either not relative, or is absolute but not in the pack://application:,,,/
        //     form.
        //
        //   System.IO.IOException:
        //     The System.Uri that is passed to System.Windows.Application.GetResourceStream(System.Uri)
        //     cannot be found.
        /// <summary>
        /// Returns a string that contains the content of the file that is located at the
        /// specified System.Uri.
        /// </summary>
        /// <param name="uriResource">The System.Uri that maps to an embedded resource.</param>
        /// <returns>
        /// A string that contains the content of the file that is located at the specified System.Uri.
        /// </returns>
        public static Task<string> GetResourceString(Uri uriResource)
        {
            if (_resourcesCache == null)
            {
                _resourcesCache = new Dictionary<string, string>();
            }
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            if (_resourcesCache.ContainsKey(uriResource.OriginalString.ToLower()))
            {
                tcs.SetResult(_resourcesCache[uriResource.OriginalString.ToLower()]);
                return tcs.Task;
            }
            HashSet<string> supportedExtensions = new HashSet<string>(new string[] { ".txt", ".xml", ".config", ".json", ".clientconfig" });

            string uriAsString = uriResource.OriginalString;
            string extension = uriAsString.Substring(uriAsString.LastIndexOf('.'));
            if (!supportedExtensions.Contains(extension.ToLower())) //todo: when we will be able to handle more extensions, add them to supportedExtensions and do not forget to update GetResourceStream as well.
            {
                throw new NotSupportedException("Application.GetResourceString is currently not supported for files with an extension different than .txt, .xml, .json, .config, or .clientconfig.");
            }
            List<string> uris = new List<string>();
            uris.Add(uriAsString + ".g.js");
            if (uriResource.OriginalString.ToLower() == "ms-appx://app.config")
            {
                CSHTML5.Interop.LoadJavaScriptFilesAsync(
                    uris,
                    (Action)(() =>
                    {
                        tcs.SetResult(OpenSilver.Interop.ExecuteJavaScriptString("window.AppConfig"));
                    })
                    );
            }
            else if (uriResource.OriginalString.ToLower() == "ms-appx://servicereferences.clientconfig")
            {
                CSHTML5.Interop.LoadJavaScriptFilesAsync(
                    uris,
                    (Action)(() =>
                    {
                        tcs.SetResult(OpenSilver.Interop.ExecuteJavaScriptString("window.ServiceReferencesClientConfig"));
                    })
                    );
            }
            else
            {
                CSHTML5.Interop.LoadJavaScriptFilesAsync(
                    uris,
                    (Action)(() =>
                    {
                        string result = OpenSilver.Interop.ExecuteJavaScriptString("window.FileContent");
                        _resourcesCache.Add(uriResource.OriginalString.ToLower(), result);
                        tcs.SetResult(result);
                    })
                    );
            }
            //return Convert.ToString(Interop.ExecuteJavaScript("window.FileContent"));
            return tcs.Task;
        }

        /// <summary>
        /// Searches for the specified resource.
        /// </summary>
        /// <param name="resourceKey">The name of the resource to find.</param>
        /// <returns>
        /// The requested resource object. If the requested resource is not found, a
        /// null reference is returned.
        /// </returns>
        public object TryFindResource(object resourceKey)
        {
            return XamlResourcesHandler.TryFindResource(resourceKey);
        }

        /// <summary>
        /// Create logic tree from given resource Locator, and associate this
        /// tree with the given component.
        /// </summary>
        /// <param name="component">Root Element</param>
        /// <param name="resourceLocator">Resource Locator</param>
        public static void LoadComponent(object component, Uri resourceLocator)
        {
            if (component is null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (resourceLocator is null)
            {
                throw new ArgumentNullException(nameof(resourceLocator));
            }

            if (resourceLocator.IsAbsoluteUri)
            {
                throw new ArgumentException("Uri must be relative.");
            }

            string resourceUri = resourceLocator.ToString();
            if (IsComponentUri(resourceUri))
            {
                IXamlComponentLoader factory = GetXamlComponentLoader(resourceUri);
                if (factory != null)
                {
                    factory.LoadComponent(component);
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void LoadComponent(object component, IXamlComponentLoader loader)
        {
            if (component is null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (loader is null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            loader.LoadComponent(component);
        }

        private static bool IsComponentUri(string uri)
        {
            int index = uri.IndexOf(';');
            if (index > -1)
            {
                return uri.Substring(index).StartsWith(";component/");
            }

            return false;
        }

        private static string ExtractAssemblyNameFromComponentUri(string uri)
        {
            int offset = uri[0] == '/' ? 1 : 0;
            return uri.Substring(offset, uri.IndexOf(';') - offset);
        }

        private static IXamlComponentLoader GetXamlComponentLoader(string componentUri)
        {
            string className = XamlResourcesHelper.GenerateClassNameFromComponentUri(componentUri);
            string assemblyName = ExtractAssemblyNameFromComponentUri(componentUri);

            Type loaderType = Type.GetType($"{className}, {assemblyName}");
            if (loaderType != null)
            {
                return Activator.CreateInstance(loaderType) as IXamlComponentLoader;
            }

            return null;
        }

        // Exceptions:
        //   System.ArgumentNullException:
        //     The System.Uri that is passed to System.Windows.Application.GetResourceStream(System.Uri)
        //     is null.
        //
        //   System.ArgumentException:
        //     The System.Uri.OriginalString property of the System.Uri that is passed to
        //     System.Windows.Application.GetResourceStream(System.Uri) is null.
        //
        //   System.ArgumentException:
        //     The System.Uri that is passed to System.Windows.Application.GetResourceStream(System.Uri)
        //     is either not relative, or is absolute but not in the pack://application:,,,/
        //     form.
        //
        //   System.IO.IOException:
        //     The System.Uri that is passed to System.Windows.Application.GetResourceStream(System.Uri)
        //     cannot be found.
        /// <summary>
        /// Returns a resource stream for a resource data file that is located at the
        /// specified System.Uri (see WPF Application Resource, Content, and Data Files).
        /// </summary>
        /// <param name="uriResource">The System.Uri that maps to an embedded resource.</param>
        /// <returns>
        /// A System.Windows.Resources.StreamResourceInfo that contains a resource stream
        /// for resource data file that is located at the specified System.Uri.
        /// </returns>
        public static async Task<StreamResourceInfo> GetResourceStream(Uri uriResource)
        {
            string resourceString = await GetResourceString(uriResource);
            string uriAsString = uriResource.OriginalString;
            string extensionLowercase = uriAsString.Substring(uriAsString.LastIndexOf('.')).ToLower();

            byte[] byteArray = Encoding.ASCII.GetBytes(resourceString);
            MemoryStream stream = new MemoryStream(byteArray);

            string mimeType = "text/plain";
            if (extensionLowercase == ".xml" || extensionLowercase == ".config" || extensionLowercase == ".clientconfig")
            {
                mimeType = "application/xml";
            }
            else if (extensionLowercase == ".json")
            {
                mimeType = "application/json";
            } //todo: update this when more extensions will be handled

            StreamResourceInfo resourceInfo = new StreamResourceInfo(stream, mimeType);
            return resourceInfo;
        }

        public static event EventHandler INTERNAL_Reloaded;

        /// <summary>
        /// Intended to be called by the Simulator when navigating back from an external page.
        /// </summary>
        public static void INTERNAL_RaiseReloadedEvent()
        {
            EventHandler handler = INTERNAL_Reloaded;
            if (handler != null)
            {
                handler(null, null);
            }
        }


        #region Exit event

        /// <summary>
        /// Occurs just before an application shuts down and cannot be canceled.
        /// </summary>
        public event EventHandler Exit;

        /// <summary>
        /// Raises the Exit event
        /// </summary>
        void ProcessOnExit(object jsEventArg)
        {
            OnExit(EventArgs.Empty);
        }

        /// <summary>
        /// Raises the Exit event
        /// </summary>
        /// <param name="eventArgs">The arguments for the event.</param>
        protected virtual void OnExit(EventArgs eventArgs)
        {
            Exit?.Invoke(this, eventArgs);
        }

        #endregion

        private Host _host = new Host(true);

        /// <summary>
        /// Gets various details about the application's host.
        /// </summary>
        public Host Host
        {
            get
            {
                if (_host == null)
                    _host = new Host(true);
                return _host;
            }
        }

#if CSHTML5NETSTANDARD
        /// <summary>
        /// The entry point of the application needs to be wrapped by this method
        /// to ensure correction functioning of the application.
        /// </summary>
        /// <param name="entryPoint"></param>
        public static void RunApplication(Action entryPoint)
        {
            entryPoint();
            INTERNAL_ExecuteJavaScript.ExecutePendingJavaScriptCode();
        }
#endif
    }
}
