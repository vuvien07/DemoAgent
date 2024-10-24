﻿using FontAwesome.WPF;
using HotelManagement.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.ApplicationServices;
using Models;
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
using System.Xaml;
using Util;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for UserContainer.xaml
    /// </summary>
    public partial class UserContainer : Window
    {
        private Account account;
        public List<ToggleButton> lsButton;
        public Record? _recordInstance = null;
        private UserRecord? _userRecordInstance = null;
        private SpeechLive? _speechLiveInstance = null;
        private RoutedEventArgs? _args = null;

        public UserContainer(Account account)
        {
            InitializeComponent();
            (System.Windows.Application.Current as App).SetCurrWindow(this);
            this.account = account;
            if (lsButton == null)
            {
                lsButton = new List<ToggleButton>() { btRecord, btLive, btProfileUser, btInformationApp, btContactApp, btUserRecord };
            }
            lbAccount.Content = account.Username;
            lbId.Content = account.Id;
            if (_recordInstance == null)
            {
                DisableButton(BtStopRecord);
                _args = new RoutedEventArgs(Window.LoadedEvent);
                _recordInstance = new Record(account);
                ContainerUser.Child = _recordInstance;
            }
        }

        private void btLive_Click(object sender, RoutedEventArgs e)
        {
            EnableButton(lsButton);
            if (_speechLiveInstance == null)
            {
                _speechLiveInstance = new SpeechLive(account);
            }
            ContainerUser.Child = _speechLiveInstance;
            DisableButton((ToggleButton)sender);
        }

        private void btRecord_Click(object sender, RoutedEventArgs e)
        {
            EnableButton(lsButton);
            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableButton((ToggleButton)sender);
                ContainerUser.Child = _recordInstance;
                _recordInstance.Loaded -= _recordInstance.Window_Loaded;
                _recordInstance.Loaded += _recordInstance.Window_Loaded;
            });
        }

        public void DisableButton(ToggleButton button)
        {
            if (button.IsEnabled)
            {
                button.IsEnabled = false;
            }
            foreach (var child in spMenu.Children)
            {
                if (child is ToggleButton toggleButton && toggleButton != button)
                {
                    toggleButton.IsChecked = false;
                }
            }
        }

        public void EnableButton(List<ToggleButton> buttons)
        {
            foreach (var button in buttons)
            {
                if (!button.IsEnabled)
                {
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
            EnableButton(lsButton);
            ContainerUser.Child = null;
            MenuPanel.Visibility = Visibility.Collapsed;
            BellOn.Visibility = Visibility.Collapsed;
            bellOff.Visibility = Visibility.Visible;
            DisableButton((ToggleButton)sender);

        }

        private void btProfileUser_Click(object sender, RoutedEventArgs e)
        {
            EnableButton(lsButton);
            ContainerUser.Child = null;
            MenuPanel.Visibility = Visibility.Collapsed;
            BellOn.Visibility = Visibility.Collapsed;
            bellOff.Visibility = Visibility.Visible;
            DisableButton((ToggleButton)sender);

        }

        private void btContactApp_Click(object sender, RoutedEventArgs e)
        {
            EnableButton(lsButton);
            ContainerUser.Child = null;
            MenuPanel.Visibility = Visibility.Collapsed;
            BellOn.Visibility = Visibility.Collapsed;
            bellOff.Visibility = Visibility.Visible;
            DisableButton((ToggleButton)sender);
        }

        private void BtStopRecord_Clicked(object sender, RoutedEventArgs e)
        {
            var iconImage = BtPauseRecord.FindName("IcoStopRecord") as ImageAwesome;
            if (iconImage.Icon == FontAwesomeIcon.DotCircleOutline)
            {
                _recordInstance.StartRecord();
            }
            else
            {
                _recordInstance.updateIcon(FontAwesome.WPF.FontAwesomeIcon.Pause, BtPauseRecord, "IcoPauseRecord");
                _recordInstance.updateIcon(FontAwesome.WPF.FontAwesomeIcon.DotCircleOutline, BtStopRecord, "IcoStopRecord");
                BtPauseRecord.Visibility = Visibility.Collapsed;
                RecordTime.Visibility = Visibility.Collapsed;
                _recordInstance.StopRecord();
            }
        }

        private void BtPauseRecord_Checked(object sender, RoutedEventArgs e)
        {
            var iconImage = BtPauseRecord.FindName("IcoPauseRecord") as ImageAwesome;
            if (iconImage.Icon == FontAwesomeIcon.Pause)
            {
                _recordInstance.updateIcon(FontAwesome.WPF.FontAwesomeIcon.Play, BtPauseRecord, "IcoPauseRecord");
                _recordInstance.timer.Stop();
                _recordInstance.waveInEvent.DataAvailable -= _recordInstance.OnDataAvailable;
            }
            else
            {
                _recordInstance.updateIcon(FontAwesome.WPF.FontAwesomeIcon.Pause, BtPauseRecord, "IcoPauseRecord");
                _recordInstance.timer.Start();
                _recordInstance.waveInEvent.DataAvailable += _recordInstance.OnDataAvailable;
            }
        }

        private void btUserRecord_Click(object sender, RoutedEventArgs e)
        {
            EnableButton(lsButton);
            if (_userRecordInstance == null)
            {
               _userRecordInstance = new UserRecord(account);
            }
            ContainerUser.Child = _userRecordInstance;
            DisableButton((ToggleButton)sender);
        }
    }
}
