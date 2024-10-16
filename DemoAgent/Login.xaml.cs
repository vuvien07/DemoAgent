using DemoAgent.Util;
using FontAwesome.WPF;
using HotelManagement.Util;
using Models;
using Repositories;
using Services;
using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Util;

namespace DemoAgent
{
    public partial class Login : Window
    {
        private DispatcherTimer dispatcherTimer;
        private int progressValue;
        public bool isLoggingIn = false;
        public Account? authenticatedAccount;
        private ManagementEventWatcher? insertWatcher;
        private ManagementEventWatcher? deleteWatcher;


        public ManagementEventWatcher InsertWatcher { get => insertWatcher; set => insertWatcher = value; }


        public Login()
        {
            InitializeComponent();
            EventUtil.PrintNotice -= MessageBoxUtil.PrintMessageBox;
            EventUtil.PrintNotice += MessageBoxUtil.PrintMessageBox;
            StartUsbWatcher();
            (System.Windows.Application.Current as App).SetCurrWindow(this);
            (System.Windows.Application.Current as App).SubscribeClosingEvent(this);
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (CheckUsbForAccountFile() && !isLoggingIn)
                {
                    isLoggingIn = true;
                    InitializeTimer();
                    
                }
            });
        }

        private void InitializeTimer()
        {
            progressValue = 0;
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(10); // Update interval
            dispatcherTimer.Tick += Timer_Tick;
            dispatcherTimer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (progressValue < 100)
            {
                progressValue++;
                progressBar.Value = progressValue;
            }
            else
            {
                dispatcherTimer.Stop();
                AutoLogin();
            }
        }

        private void AutoLogin()
        {
            authenticatedAccount = GetAccount();
            if (authenticatedAccount != null)
            {
                isLoggingIn = true;
                if (authenticatedAccount.RoleId == 2)
                {
                    UserContainer user = new UserContainer(authenticatedAccount);
                    (System.Windows.Application.Current as App).SetCurrWindow(user);
                    (System.Windows.Application.Current as App).SetAccount(authenticatedAccount);
                    (System.Windows.Application.Current as App).SubscribeClosingEvent(user);
                    Window currentWindow = Window.GetWindow(this);
                    if (currentWindow != null)
                    {
                        currentWindow.Close();
                    }
                    user.Show();
                }
            }
            else
            {
                isLoggingIn = false;
            }
        }

        private Account? GetAccount()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    string fileName = "Account.txt";
                    string filePath = System.IO.Path.Combine(drive.Name, fileName);

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            string content = File.ReadAllText(filePath);
                            var account = AccountDBContext.Instance.getAccountByPrivateKey(content);
                            if (account != null)
                            {
                                return account;
                            }
                            else
                            {
                                EventUtil.printNotice("Account not found!", MessageUtil.ERROR);
                            }
                        }
                        catch (Exception ex)
                        {
                            EventUtil.printNotice("An error occured!", MessageUtil.ERROR);
                        }

                    }
                    else
                    {
                        EventUtil.printNotice("No valid USB drive with Account.txt found!", MessageUtil.ERROR);
                    }
                }
            }
            return null;
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
        private void StartUsb()
        {
            insertWatcher = new ManagementEventWatcher();
            insertWatcher.Query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
            InsertWatcher = insertWatcher;
        }

        private void StartUsbWatcher()
        {
            StartUsb();
            InsertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            InsertWatcher.Start();
        }

        public bool CheckUsbForAccountFile()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    string fileName = "Account.txt";
                    string filePath = System.IO.Path.Combine(drive.Name, fileName);
                    if (File.Exists(filePath))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}