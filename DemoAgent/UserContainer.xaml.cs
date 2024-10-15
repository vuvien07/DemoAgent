using HotelManagement.Util;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Services;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Util;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for UserContainer.xaml
    /// </summary>
    public partial class UserContainer : Window
    {
        private Account account;
        private readonly RecordService? recordService;
        private List<ToggleButton> lsButton;

        public UserContainer(Account account)
        {
            InitializeComponent();
            this.account = account;
            if(recordService == null)
            {
                recordService = RecordService.Instance;
                recordService.InitializeService(account);
            }
            if(lsButton == null)
            {
                lsButton = new List<ToggleButton>() { btRecord, btMeeting, btLive, btCryto};
            }
            lbAccount.Content = account.Username;
            lbId.Content = account.Id;
            ContainerUser.Child = new UserCrypto(account);
        }

        private void btExit_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }


        private void btLive_Click(object sender, RoutedEventArgs e)
        {
            ContainerUser.Child = new SpeechLive();
            UncheckOthers(btLive);
        }

        private void btRecord_Click(object sender, RoutedEventArgs e)
        {
            ContainerUser.Child = new Record(account, false);
            UncheckOthers(btRecord);
        }

        private void btCryto_Click(object sender, RoutedEventArgs e)
        {
            var userCrypto = new UserCrypto(account);
            ContainerUser.Child = userCrypto;
            UncheckOthers(btCryto);
        }

        private void btMeeting_Click(object sender, RoutedEventArgs e)
        {
            var meeting = new UserMeeting(account);
            ContainerUser.Child = meeting;
            UncheckOthers(btMeeting);
        }

        public void RaiseEvent()
        {
            var userContainer = new UserContainer(account);
            userContainer.ContainerUser.Child = new Record(account, true);
            recordService.RecordMode = MessageUtil.RECORD_AUTOMATIC;
            UncheckOthers(userContainer.btRecord);
            userContainer.Show();
            Record record = userContainer.ContainerUser.Child as Record;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RoutedEventArgs args = new RoutedEventArgs(Button.ClickEvent);
                record.RecordButton.RaiseEvent(args);
            }), DispatcherPriority.ApplicationIdle);
            (Application.Current as App).SetCurrWindow(userContainer);
            (Application.Current as App).SubscribeClosingEvent(userContainer);
        }

        private void DisableButton(ToggleButton button)
        {
            if (button.IsEnabled)
            {
                button.IsEnabled = false;
            }
        }

        private void EnableButton(List<ToggleButton> buttons)
        {
            foreach (var button in buttons)
            {
                if (!button.IsEnabled) { 
                    button.IsEnabled = true;
                    return;
                }
            }
        }

        private void MenuIconGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Toggle giữa Visible và Collapsed
            if (MenuPanel.Visibility == Visibility.Collapsed)
            {
                MenuPanel.Visibility = Visibility.Visible;
                BellOn.Visibility = Visibility.Visible;
                bellOff.Visibility = Visibility.Collapsed;
            }
            else
            {
                MenuPanel.Visibility = Visibility.Collapsed;
                BellOn.Visibility = Visibility.Collapsed;
                bellOff.Visibility = Visibility.Visible;
            }
        }
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            InfoStackPanel.Visibility = Visibility.Visible; // Hiển thị StackPanel khi di chuột vào Grid
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            InfoStackPanel.Visibility = Visibility.Collapsed; // Ẩn StackPanel khi di chuột ra ngoài Grid
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Đóng cửa sổ
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; // Thu nhỏ cửa sổ
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem chuột có phải là nút trái không
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                // Kéo cửa sổ
                this.DragMove();
            }
        }

        private void btInformationApp_Click(object sender, RoutedEventArgs e)
        {
            ContainerUser.Child = null;
            MenuPanel.Visibility = Visibility.Collapsed;
            BellOn.Visibility = Visibility.Collapsed;
            bellOff.Visibility = Visibility.Visible;
            UncheckOthers(null);
        }

        private void btProfileUser_Click(object sender, RoutedEventArgs e)
        {
            ContainerUser.Child = null;
            MenuPanel.Visibility = Visibility.Collapsed;
            BellOn.Visibility = Visibility.Collapsed;
            bellOff.Visibility = Visibility.Visible;
            UncheckOthers(null);

        }

        private void btContactApp_Click(object sender, RoutedEventArgs e)
        {
            ContainerUser.Child = null;
            MenuPanel.Visibility = Visibility.Collapsed;
            BellOn.Visibility = Visibility.Collapsed;
            bellOff.Visibility = Visibility.Visible;
            UncheckOthers(null);

        }
        private void UncheckOthers(ToggleButton selectedButton)
        {
            foreach (var child in spMenu.Children)
            {
                if (child is ToggleButton toggleButton && toggleButton != selectedButton)
                {
                    toggleButton.IsChecked = false;
                }
            }
        }
    }
}
