using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SCME.UI.CustomControl
{
    /// <summary>
    /// Interaction logic for Keyboard.xaml
    /// </summary>
    public partial class VirtualKeyboard : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String Info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Info));
        }

        #endregion

        public event EventHandler<RoutedEventArgs> EnterPressed;

        public Key PressedKey
        {
            get { return KEY; }
        }

        public CKeyboardLayout KeyLayout { get; set; }

        private const Key KEY = Key.None;

        public VirtualKeyboard()
        {
            InitializeComponent();

            if (Cache.Keyboards == null)
                return;

            foreach (var keyboards in Cache.Keyboards.Collection)
                languageComboBox.Items.Add(keyboards.Language);

            languageComboBox.SelectedIndex = 0;
        }

        public void Show(bool IsAnimateEnabled)
        {
            if (IsAnimateEnabled)
                AnimateScrollViewer(270);
            else
                keyboardScrollViewer.ScrollToEnd();
        }

        public void Hide(bool IsAnimateEnabled)
        {
            if (IsAnimateEnabled)
                AnimateScrollViewer(0);
            else
                keyboardScrollViewer.ScrollToTop();
        }

        private void AnimateScrollViewer(double To)
        {
            var verticalAnimation = new DoubleAnimation
                {
                    To = To,
                    DecelerationRatio = .2,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                };

            var storyboard = new Storyboard();
            storyboard.Children.Add(verticalAnimation);
            Storyboard.SetTarget(verticalAnimation, keyboardScrollViewer);
            Storyboard.SetTargetProperty(verticalAnimation,
                                         new PropertyPath(ScrollViewerBehavior.VerticalOffsetProperty));
            storyboard.Begin();
        }

        private void BNormalClick(object Sender, RoutedEventArgs E)
        {
            var btn = Sender as Button;
            if (btn == null)
                return;

            var eventArgs = new TextCompositionEventArgs(Keyboard.PrimaryDevice,
                                                         new TextComposition(InputManager.Current,
                                                                             Keyboard.FocusedElement,
                                                                             btn.Content.ToString()))
                {
                    RoutedEvent = TextInputEvent
                };

            InputManager.Current.ProcessInput(eventArgs);
        }

        private void Backspase_Click(object Sender, RoutedEventArgs E)
        {
            if (Keyboard.PrimaryDevice.ActiveSource != null)
            {
                var args = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Back)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };

                InputManager.Current.ProcessInput(args);
            }
        }

        private void Right_Click(object Sender, RoutedEventArgs E)
        {
            if (Keyboard.PrimaryDevice.ActiveSource != null)
            {
                var args = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Right)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };

                InputManager.Current.ProcessInput(args);
            }
        }

        private void Left_Click(object Sender, RoutedEventArgs E)
        {
            if (Keyboard.PrimaryDevice.ActiveSource != null)
            {
                var args = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Left)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };

                InputManager.Current.ProcessInput(args);
            }
        }

        private void Enter_Click(object Sender, RoutedEventArgs E)
        {
            if (Keyboard.PrimaryDevice.ActiveSource != null)
            {
                var args = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };

                InputManager.Current.ProcessInput(args);
            }

            var handler = EnterPressed;
            if (handler != null)
                handler(this, E);
        }

        private void Selector_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            var cb = Sender as ComboBox;
            if (cb == null)
                return;

            KeyLayout = Cache.Keyboards.Collection[cb.SelectedIndex];
            NotifyPropertyChanged("KeyLayout");
        }

        private void UIElement_OnMouseDown(object Sender, MouseButtonEventArgs E)
        {
            OnPreviewMouseDown(E);
            E.Handled = true;
        }
    }
}