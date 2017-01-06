using System.Text.RegularExpressions;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


namespace SmartMerchant
{
    public sealed partial class DigitPassbox : UserControl
    {
        private readonly string _passwordChar = "●";

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }
        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register("Password", typeof(string), typeof(DigitPassbox), new PropertyMetadata(null));
        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register("MaxLength", typeof(int), typeof(DigitPassbox), new PropertyMetadata(null));


        public DigitPassbox()
        {
            this.InitializeComponent();
            ((FrameworkElement)Content).DataContext = this;
        }

        private void PasswordBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(Password) && e.Key == VirtualKey.Back)
                FocusManager.TryMoveFocus(FocusNavigationDirection.Previous);
            
            Password = GetNewPasscode(Password, e);
            DigitPasswordBox.Text = string.IsNullOrEmpty(Password) ? string.Empty : Regex.Replace(Password, @".", _passwordChar);
            
            if (DigitPasswordBox.Text.Length > 0)
                FocusManager.TryMoveFocus(FocusNavigationDirection.Next);

            DigitPasswordBox.SelectionStart = !string.IsNullOrEmpty(DigitPasswordBox.Text) ? DigitPasswordBox.Text.Length : 0;
        }

        private string GetNewPasscode(string oldPasscode, KeyRoutedEventArgs keyEventArgs)
        {
            string newPasscode = string.Empty;
            switch (keyEventArgs.Key)
            {
                case VirtualKey.Number0:
                case VirtualKey.NumberPad0:
                    newPasscode = "0";
                    break;
                case VirtualKey.Number1:
                case VirtualKey.NumberPad1:
                    newPasscode = "1";
                    break;
                case VirtualKey.Number2:
                case VirtualKey.NumberPad2:
                    newPasscode = "2";
                    break;
                case VirtualKey.Number3:
                case VirtualKey.NumberPad3:
                    newPasscode = "3";
                    break;
                case VirtualKey.Number4:
                case VirtualKey.NumberPad4:
                    newPasscode =  "4";
                    break;
                case VirtualKey.Number5:
                case VirtualKey.NumberPad5:
                    newPasscode = "5";
                    break;
                case VirtualKey.Number6:
                case VirtualKey.NumberPad6:
                    newPasscode = "6";
                    break;
                case VirtualKey.Number7:
                case VirtualKey.NumberPad7:
                    newPasscode = "7";
                    break;
                case VirtualKey.Number8:
                case VirtualKey.NumberPad8:
                    newPasscode =  "8";
                    break;
                case VirtualKey.Number9:
                case VirtualKey.NumberPad9:
                    newPasscode = "9";
                    break;
                case VirtualKey.Back:
                    if (!string.IsNullOrEmpty(oldPasscode))
                    {
                        newPasscode = oldPasscode.Substring(0, oldPasscode.Length - 1);
                    }
                    break;
                default:
                    //others
                    newPasscode = oldPasscode;
                    break;
            }
            return newPasscode;
        }
    }
}
