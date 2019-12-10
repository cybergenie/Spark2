using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Spark2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public  List<string> SelectedBleDeviceId = new List<string>();
        public  List<string> SelectedBleDeviceName = new List<string>();
        public MainWindow()
        {
            InitializeComponent();
            Navigate("Discovery");
        }

        private void btnNav_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem btn = sender as ListBoxItem;
            Navigate(btn.Tag.ToString());
        }

        private void Navigate(string path)
        {
            string uri = "Spark2.Views." + path;
            Type type = Type.GetType(uri);
            if (type != null)
            {
                object obj = type.Assembly.CreateInstance(uri);
                Page control = obj as Page;
                this.frmMain.Content = control;  
                PropertyInfo[] infos = type.GetProperties();
                foreach (PropertyInfo info in infos)
                {
                    if (info.Name == "ParentWindow")
                    {
                        info.SetValue(control, this);
                        break;
                    }
                }

            }
        }

        public void CallFromChild(string name)
        {
            MessageBox.Show("Hello," + name + "!");
        }

        public void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.CheckAccess())
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.InvokeAsync(() => UpdateStatus(strMessage, type));
            }

        }
        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Colors.Red);
                    break;
            }

            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }            
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };
    }
}
