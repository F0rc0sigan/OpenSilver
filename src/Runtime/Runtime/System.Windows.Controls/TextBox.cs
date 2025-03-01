
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
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;
using CSHTML5.Internal;
using OpenSilver.Internal.Controls;

#if MIGRATION
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Input;
#else
using Windows.Foundation;
using Windows.UI.Text;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using KeyEventArgs = Windows.UI.Xaml.Input.KeyRoutedEventArgs;
#endif

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Represents a control that can be used to display single-format, multi-line
    /// text.
    /// </summary>
    /// <example>
    /// <code lang="XAML" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    /// <TextBox x:Name="TextBoxName" Text="Some text"/>
    /// </code>
    /// <code lang="C#">
    ///     TextBoxName.Text = "Some text";
    /// </code>
    /// </example>
    [TemplatePart(Name = ContentElementName, Type = typeof(FrameworkElement))]
    [TemplateVisualState(Name = VisualStates.StateReadOnly, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateDisabled, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateMouseOver, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateFocused, GroupName = VisualStates.GroupFocus)]
    [TemplateVisualState(Name = VisualStates.StateUnfocused, GroupName = VisualStates.GroupFocus)]
    [TemplateVisualState(Name = VisualStates.StateValid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateInvalidUnfocused, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateInvalidFocused, GroupName = VisualStates.GroupValidation)]
    public partial class TextBox : Control
    {
        private const string ContentElementName = "ContentElement"; // Sl & UWP
        private const string ContentElementName_WPF = "PART_ContentHost"; // WPF

        private bool _isProcessingInput;
        private bool _isFocused;
        private int? _selectionIdxCache;
        private ScrollViewer _scrollViewer;
        private FrameworkElement _contentElement;
        private ITextBoxViewHost<TextBoxView> _textViewHost;

        public TextBox()
        {
            DefaultStyleKey = typeof(TextBox);
            IsEnabledChanged += (o, e) => UpdateVisualStates();
        }

        internal sealed override object GetFocusTarget() => _textViewHost?.View?.InputDiv;

        internal sealed override void UpdateTabIndexCore(bool isTabStop, int tabindex)
        {
            object focusTarget = GetFocusTarget();
            if (focusTarget == null)
            {
                return;
            }
            
            INTERNAL_HtmlDomManager.SetDomElementAttribute(
                focusTarget,
                "tabindex",
                (!isTabStop || !IsEnabled) ? "-1" : ConvertToHtmlTabIndex(tabindex).ToString());
        }

        /// <summary>
        /// Gets or sets the value that determines whether the text box allows and displays
        /// the newline or return characters.
        /// </summary>
        public bool AcceptsReturn
        {
            get { return (bool)GetValue(AcceptsReturnProperty); }
            set { SetValue(AcceptsReturnProperty, value); }
        }

        /// <summary>
        /// Identifies the AcceptsReturn dependency property.
        /// </summary>
        public static readonly DependencyProperty AcceptsReturnProperty =
            DependencyProperty.Register(
                nameof(AcceptsReturn), 
                typeof(bool), 
                typeof(TextBox), 
                new PropertyMetadata(false, OnAcceptsReturnChanged));

        private static void OnAcceptsReturnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnAcceptsReturnChanged((bool)e.NewValue);
            }
        }

        /// <summary>
        /// Gets or sets the value that determines whether pressing tab while the TextBox has the focus will add a tabulation in the text or set the focus to the next element.
        /// True to add a tabulation, false to set the focus to the next element.
        /// </summary>
        public bool AcceptsTab
        {
            get { return (bool)GetValue(AcceptsTabProperty); }
            set { SetValue(AcceptsTabProperty, value); }
        }

        /// <summary>
        /// Identifies the AcceptsReturn dependency property.
        /// </summary>
        public static readonly DependencyProperty AcceptsTabProperty =
            DependencyProperty.Register(
                nameof(AcceptsTab), 
                typeof(bool), 
                typeof(TextBox), 
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets the text that is displayed in the control until the value is changed by a user action or some other operation.
        /// </summary>
        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        /// <summary>
        /// Identifies the PlaceholderText dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText), 
                typeof(string), 
                typeof(TextBox), 
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the text displayed in the TextBox.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Identifies the Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text), 
                typeof(string), 
                typeof(TextBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure, OnTextChanged, CoerceText));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._isProcessingInput)
            {
                // Clear the flag to allow changing the Text during TextChanged
                tb._isProcessingInput = false;
            }
            else if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnTextChanged((string)e.NewValue);
            }

            if (tb._selectionIdxCache.HasValue)
            {
                tb._textViewHost.View.NEW_SET_SELECTION(tb._selectionIdxCache.Value, tb._selectionIdxCache.Value);
                tb._selectionIdxCache = null;
            }

            tb.OnTextChanged(new TextChangedEventArgs() { OriginalSource = tb });
        }

        private static object CoerceText(DependencyObject d, object value)
        {
            return value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets how the text should be aligned in the text box.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// Identifies the TextAlignment dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment), 
                typeof(TextAlignment), 
                typeof(TextBox), 
                new PropertyMetadata(TextAlignment.Left, OnTextAlignmentChanged));

        private static void OnTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnTextAlignmentChanged((TextAlignment)e.NewValue);
            }
        }

        /// <summary>
        /// Gets or sets the brush that is used to render the vertical bar that indicates
        /// the insertion point.
        /// </summary>
        public Brush CaretBrush
        {
            get { return (Brush)GetValue(CaretBrushProperty); }
            set { SetValue(CaretBrushProperty, value); }
        }

        /// <summary>
        /// Identify the CaretBrush dependency property
        /// </summary>
        public static readonly DependencyProperty CaretBrushProperty =
            DependencyProperty.Register(
                nameof(CaretBrush), 
                typeof(Brush), 
                typeof(TextBox), 
                new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        /// <summary>
        /// Gets or sets how the TextBow wraps text.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        /// <summary>
        /// Identifies the TextWrapping dependency property.
        /// </summary>
        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(
                nameof(TextWrapping),
                typeof(TextWrapping),
                typeof(TextBox),
                new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure, OnTextWrappingChanged));

        private static void OnTextWrappingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            tb.CoerceValue(HorizontalScrollBarVisibilityProperty);
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnTextWrappingChanged((TextWrapping)e.NewValue);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a horizontal ScrollBar should
        /// be displayed.
        /// 
        /// Returns a ScrollBarVisibility value that indicates whether a horizontal ScrollBar
        /// should be displayed. The default value is Hidden.
        /// </summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Identifies the HorizontalScrollBarVisibility dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(HorizontalScrollBarVisibility),
                typeof(ScrollBarVisibility),
                typeof(TextBox),
                new PropertyMetadata(ScrollBarVisibility.Hidden, OnHorizontalScrollBarVisibilityChanged, CoerceHorizontalScrollBarVisibility));

        private static void OnHorizontalScrollBarVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._scrollViewer != null)
            {
                tb._scrollViewer.HorizontalScrollBarVisibility = (ScrollBarVisibility)e.NewValue;
            }
        }

        private static object CoerceHorizontalScrollBarVisibility(DependencyObject d, object baseValue)
        {
            var tb = (TextBox)d;
            if (tb.TextWrapping == TextWrapping.Wrap)
            {
                return ScrollBarVisibility.Disabled;
            }
            return baseValue;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a vertical ScrollBar should be displayed.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Identifies the VerticalScrollBarVisibility dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(VerticalScrollBarVisibility),
                typeof(ScrollBarVisibility),
                typeof(TextBox),
                new PropertyMetadata(ScrollBarVisibility.Hidden, OnVerticalScrollBarVisibilityChanged));

        private static void OnVerticalScrollBarVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._scrollViewer != null)
            {
                tb._scrollViewer.VerticalScrollBarVisibility = (ScrollBarVisibility)e.NewValue;
            }
        }

        /// <summary>
        /// Gets or sets the value that determines the maximum number of characters allowed
        /// for user input.
        /// </summary>
        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        /// <summary>
        /// Identifies the MaxLength dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(TextBox),
                new PropertyMetadata(0, OnMaxLengthChanged));

        private static void OnMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnMaxLengthChanged((int)e.NewValue);
            }
        }

#if MIGRATION
        /// <summary>
        /// Gets or sets the text decorations (underline, strikethrough...).
        /// </summary>
        public new TextDecorationCollection TextDecorations
        {
            get { return (TextDecorationCollection)GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }

        /// <summary>
        /// Identifies the TextDecorations dependency property.
        /// </summary>
        public new static readonly DependencyProperty TextDecorationsProperty =
            DependencyProperty.Register(
                nameof(TextDecorations),
                typeof(TextDecorationCollection),
                typeof(TextBox),
                new PropertyMetadata(null, OnTextDecorationsChanged));

        private static void OnTextDecorationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnTextDecorationsChanged((TextDecorationCollection)e.NewValue);
            }
        }
#else
        /// <summary>
        /// Gets or sets the text decorations (underline, strikethrough...).
        /// </summary>
        public new TextDecorations? TextDecorations
        {
            get { return (TextDecorations?)GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }

        /// <summary>
        /// Identifies the TextDecorations dependency property.
        /// </summary>
        public new static readonly DependencyProperty TextDecorationsProperty =
            DependencyProperty.Register(
                nameof(TextDecorations), 
                typeof(TextDecorations?), 
                typeof(TextBox), 
                new PropertyMetadata(null, OnTextDecorationsChanged));

        private static void OnTextDecorationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnTextDecorationsChanged((TextDecorations?)e.NewValue);
            }
        }
#endif

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(TextBox),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnIsReadOnlyChanged((bool)e.NewValue);
            }

            tb.UpdateVisualStates();
        }

        /// <summary>
        /// Gets or sets a value that specifies whether the <see cref="TextBox"/> input 
        /// interacts with a spell check engine.
        /// </summary>
        /// <returns>
        /// true if the <see cref="TextBox"/> input interacts with a spell check engine; 
        /// otherwise, false. The default is false.
        /// </returns>
        public bool IsSpellCheckEnabled
        {
            get { return (bool)GetValue(IsSpellCheckEnabledProperty); }
            set { SetValue(IsSpellCheckEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSpellCheckEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSpellCheckEnabledProperty =
            DependencyProperty.Register(
                nameof(IsSpellCheckEnabled),
                typeof(bool),
                typeof(TextBox),
                new PropertyMetadata(false, OnIsSpellCheckEnabledChanged));

        private static void OnIsSpellCheckEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBox)d;
            if (tb._textViewHost != null)
            {
                tb._textViewHost.View.OnIsSpellCheckEnabledChanged((bool)e.NewValue);
            }
        }
        
        public string SelectedText
        {
            get
            {
                if (_textViewHost != null)
                {
                    _textViewHost.View.NEW_GET_SELECTION(out int selectionStartIndex, out int selectionLength, out _);
                    return Text.Substring(selectionStartIndex, selectionLength);
                }

                return string.Empty;
            }
            set
            {
                if (_textViewHost != null)
                {
                    _textViewHost.View.NEW_GET_SELECTION(out int selectionStartIndex, out int selectionLength, out _);
                    string text = Text.Substring(0, selectionStartIndex) + value + Text.Substring(selectionStartIndex + selectionLength);
                    _selectionIdxCache = selectionStartIndex + value.Length;
                    Text = text;
                    _selectionIdxCache = null;
                }
            }
        }


        public int SelectionStart
        {
            get
            {
                if (_textViewHost != null)
                {
                    _textViewHost.View.NEW_GET_SELECTION(out int selectionStartIndex, out _, out _);
                    return selectionStartIndex;
                }

                return 0;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("SelectionStart cannot be lower than 0");
                }

                _textViewHost?.View.NEW_SET_SELECTION(value, value + SelectionLength);
            }
        }

        public int SelectionLength
        {
            get
            {
                if (_textViewHost != null)
                {
                    _textViewHost.View.NEW_GET_SELECTION(out _, out int selectionLength, out _);
                    return selectionLength;
                }

                return 0;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("SelectionLength cannot be lower than 0");
                }

                _textViewHost?.View.NEW_SET_SELECTION(SelectionStart, SelectionStart + value);
            }
        }

        // It needs to get caret position for selection with Shift key
        internal int CaretPosition
        {
            get
            {
                if (_textViewHost != null)
                {
                    _textViewHost.View.NEW_GET_SELECTION(out _, out _, out int caretIndex);
                    return caretIndex;
                }

                return 0;
            }
        }

        /// <summary>
        /// Occurs when the text is changed.
        /// </summary>
        public event TextChangedEventHandler TextChanged;

        /// <summary>
        /// Raises the TextChanged event
        /// </summary>
        /// <param name="eventArgs">The arguments for the event.</param>
        protected virtual void OnTextChanged(TextChangedEventArgs eventArgs)
        {
            if (TextChanged != null)
            {
                TextChanged(this, eventArgs);
            }
        }

        /// <summary>
        /// Builds the visual tree for the
        /// <see cref="TextBox" /> control when a new
        /// template is applied.
        /// </summary>
#if MIGRATION
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            _scrollViewer = null;
            if (_contentElement != null)
            {
                ClearContentElement();
                _contentElement = null;
            }

            FrameworkElement contentElement = GetTemplateChild(ContentElementName) as FrameworkElement
                ?? GetTemplateChild(ContentElementName_WPF) as FrameworkElement;

            if (contentElement != null)
            {
                _scrollViewer = contentElement as ScrollViewer;
                InitializeScrollViewer();

                _contentElement = contentElement;
                InitializeContentElement();
            }

            UpdateVisualStates();
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

            if (e.Handled)
            {
                return;
            }

            e.Handled = true;
            Focus();
        }

        /// <summary>
        /// Returns a <see cref="TextBoxAutomationPeer"/> for use by the Silverlight automation 
        /// infrastructure.
        /// </summary>
        /// <returns>
        /// A <see cref="TextBoxAutomationPeer"/> for the <see cref="TextBox"/> object.
        /// </returns>
        protected override AutomationPeer OnCreateAutomationPeer()
            => new TextBoxAutomationPeer(this);

#if MIGRATION
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
#else
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
#endif
        {
#if MIGRATION
            base.OnMouseLeftButtonUp(e);
#else
            base.OnPointerReleased(e);
#endif

            e.Handled = true;
        }

#if MIGRATION
        protected override void OnMouseEnter(MouseEventArgs e)
#else
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
#endif
        {
#if MIGRATION
            base.OnMouseEnter(e);
#else
            base.OnPointerEntered(e);
#endif
            UpdateVisualStates();
        }

#if MIGRATION
        protected override void OnMouseLeave(MouseEventArgs e)
#else
        protected override void OnPointerExited(PointerRoutedEventArgs e)
#endif
        {
#if MIGRATION
            base.OnMouseLeave(e);
#else
            base.OnPointerExited(e);
#endif
            UpdateVisualStates();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
                return;

            base.OnKeyDown(e);

            HandleKeyDown(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (e.Handled)
            {
                e.PreventDefault = true;
                return;
            }

            if (IsReadOnly ||
                (!AcceptsReturn && (e.Text == "\r" || e.Text == "\n")) ||
                (MaxLength != 0 && Text.Length - SelectionLength >= MaxLength))
            {
                e.PreventDefault = true;
                return;
            }

            e.Handled = true;
        }

        internal sealed override void OnTextInputInternal()
        {
            if (_textViewHost != null)
            {
                _isProcessingInput = true;
                try
                {
                    _textViewHost.View.InvalidateMeasure();
                    SetCurrentValue(TextProperty, _textViewHost.View.GetText());
                }
                finally
                {
                    _isProcessingInput = false;
                }
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = true;
            UpdateVisualStates();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            UpdateVisualStates();
        }

        /// <summary>
        /// Selects all text in the text box.
        /// </summary>
        public void SelectAll()
        {
            Select(0, this.Text.Length);
        }

        public void Select(int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (start + length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            _textViewHost?.View.NEW_SET_SELECTION(start, start + length);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }
    
        internal override void UpdateVisualStates()
        {
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, VisualStates.StateDisabled, false);
            }
            else if (IsReadOnly)
            {
                VisualStateManager.GoToState(this, VisualStates.StateReadOnly, false);
            }
            else if (IsPointerOver)
            {
                VisualStateManager.GoToState(this, VisualStates.StateMouseOver, false);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, false);
            }

            if (_isFocused)
            {
                VisualStateManager.GoToState(this, VisualStates.StateFocused, false);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, false);
            }
        }

        private void InitializeScrollViewer()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
                _scrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
            }
        }

        private TextBoxView CreateView()
        {
            return new TextBoxView(this);
        }

        private void InitializeContentElement()
        {
            _textViewHost = GetContentHost<TextBoxView>(_contentElement);

            if (_textViewHost != null)
            {
                TextBoxView view = CreateView();
                view.Loaded += new RoutedEventHandler(OnViewLoaded);

                _textViewHost.AttachView(view);
            }
        }

        private void ClearContentElement()
        {
            if (_textViewHost != null)
            {
                _textViewHost.View.Loaded -= new RoutedEventHandler(OnViewLoaded);

                _textViewHost.DetachView();
                _textViewHost = null;
            }
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e) 
        {
            if (!IsLoaded)
            {
                return;
            }

            UpdateTabIndex(IsTabStop, TabIndex);
        }

        internal static ITextBoxViewHost<T> GetContentHost<T>(FrameworkElement contentElement) where T : FrameworkElement, ITextBoxView
        {
            if (contentElement is ContentControl cc)
            {
                return new TextBoxViewHost_ContentControl<T>(cc);
            }
            else if (contentElement is ContentPresenter cp)
            {
                return new TextBoxViewHost_ContentPresenter<T>(cp);
            }
            else if (contentElement is Border border)
            {
                return new TextBoxViewHost_Border<T>(border);
            }
            else if (contentElement is UserControl uc)
            {
                return new TextBoxViewHost_UserControl<T>(uc);
            }
            else if (contentElement is Panel panel)
            {
                return new TextBoxViewHost_Panel<T>(panel);
            }
            else if (contentElement is ItemsControl ic)
            {
                return new TextBoxViewHost_ItemsControl<T>(ic);
            }
            else if (IsContentPropertyHost(contentElement, out string contentPropertyName))
            {
                return new TextBoxViewHost_ContentProperty<T>(contentElement, contentPropertyName);
            }

            return null;
        }

        private static bool IsContentPropertyHost(FrameworkElement host, out string contentPropertyName)
        {
            ContentPropertyAttribute contentProp = (ContentPropertyAttribute)host
                .GetType()
                .GetCustomAttributes(typeof(ContentPropertyAttribute), true)
                .FirstOrDefault();

            if (contentProp != null)
            {
                contentPropertyName = contentProp.Name;
                return true;
            }

            contentPropertyName = null;
            return false;
        }

        [OpenSilver.NotImplemented]
        public event RoutedEventHandler SelectionChanged;

        [OpenSilver.NotImplemented]
        public static readonly DependencyProperty SelectionForegroundProperty =
            DependencyProperty.Register(
                nameof(SelectionForeground),
                typeof(Brush),
                typeof(TextBox),
                null);

        [OpenSilver.NotImplemented]
        public Brush SelectionForeground
        {
            get { return (Brush)this.GetValue(TextBox.SelectionForegroundProperty); }
            set { this.SetValue(TextBox.SelectionForegroundProperty, value); }
        }

        [OpenSilver.NotImplemented]
        public static readonly DependencyProperty SelectionBackgroundProperty =
            DependencyProperty.Register(
                nameof(SelectionBackground),
                typeof(Brush),
                typeof(TextBox),
                null);

        [OpenSilver.NotImplemented]
        public Brush SelectionBackground
        {
            get { return (Brush)GetValue(SelectionBackgroundProperty); }
            set { SetValue(SelectionBackgroundProperty, value); }
        }

        [OpenSilver.NotImplemented]
        public double LineHeight { get; set; }

        /// <summary>
        /// Returns a rectangle for the leading edge of the character at the specified index.
        /// </summary>
        /// <param name="charIndex">
        /// A zero-based character index of the character for which to retrieve the rectangle.
        /// </param>
        /// <returns>
        /// A rectangle for the leading edge of the character at the specified character
        /// index, or <see cref="Rect.Empty"/> if a bounding rectangle cannot be determined.
        /// </returns>
        /// <exception cref="NotImplementedException">Not implemented</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Rect GetRectFromCharacterIndex(int charIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a rectangle for the leading or trailing edge of the character at the
        /// specified index.
        /// </summary>
        /// <param name="charIndex">
        /// A zero-based character index of the character for which to retrieve the rectangle.
        /// </param>
        /// <param name="trailingEdge">
        /// true to get the trailing edge of the character; false to get the leading edge
        /// of the character.
        /// </param>
        /// <returns>
        /// A rectangle for an edge of the character at the specified character index, or
        /// <see cref="Rect.Empty"/> if a bounding rectangle cannot be determined.
        /// </returns>
        /// <exception cref="NotImplementedException">Not implemented</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Rect GetRectFromCharacterIndex(int charIndex, bool trailingEdge)
        {
            throw new NotImplementedException();
        }
    }
}
