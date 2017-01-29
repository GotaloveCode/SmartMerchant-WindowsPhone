using Windows.UI.Xaml.Input;

namespace SmartMerchant
{
    public sealed partial class EditCardPage : BindablePage
    {
        public EditCardPage()
        {
            this.InitializeComponent();
        }
        private void TextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (ExpiryMonth.Text.Length == 2)
                FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }
    }
}
