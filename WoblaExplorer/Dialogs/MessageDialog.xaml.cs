using System.Windows;

namespace WoblaExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for MessageDialog.xaml
    /// </summary>
    /// 
    /// 

    public enum MessageDialogResult
    {
        Cancel,
        Ok,
        Yes,
        No
    }

    public enum MessageDialogButtons
    {
        YesNo,
        YesNoCancel,
        Ok
    }

    public partial class MessageDialog
    {
        public MessageDialogResult MessageDialogResult { get; set; }

        public MessageDialog(string text, string caption, MessageDialogButtons buttons)
        {
            InitializeComponent();
            Title = caption;
            MainTextBlock.Text = text;
            switch (buttons)
            {
                case MessageDialogButtons.Ok:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    BtnOk.Focus();
                    break;
                case MessageDialogButtons.YesNo:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    BtnYes.Focus();
                    BtnNo.Visibility = Visibility.Visible;
                    break;
                case MessageDialogButtons.YesNoCancel:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    BtnYes.Focus();
                    BtnNo.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    break;
            }
            MessageDialogResult = MessageDialogResult.Cancel;
        }

        public MessageDialog(string text)
        {
            InitializeComponent();
            MainTextBlock.Text = text;
            BtnOk.Visibility = Visibility.Visible;
            BtnOk.IsDefault = true;
            BtnOk.Focus();
            MessageDialogResult = MessageDialogResult.Cancel;
        }

        private void BtnYes_OnClick(object sender, RoutedEventArgs e)
        {
            MessageDialogResult = MessageDialogResult.Yes;
            DialogResult = true;
        }

        private void BtnNo_OnClick(object sender, RoutedEventArgs e)
        {
            MessageDialogResult = MessageDialogResult.No;
            DialogResult = true;
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            MessageDialogResult = MessageDialogResult.Cancel;
            DialogResult = true;
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            MessageDialogResult = MessageDialogResult.Ok;
            DialogResult = true;
        }
    }
}
