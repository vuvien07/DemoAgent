using HotelManagement.Util;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NAudio.CoreAudioApi;
using Python.Runtime;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Util;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        public Window currWindow;
        public Account? account;
        public bool _isAutoClosing = false;

        public App()
        {
            string baseDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo directoryInfo = new DirectoryInfo(baseDirectory);
            directoryInfo = directoryInfo.Parent?.Parent?.Parent;

            if (directoryInfo != null)
            {
                var iconPath = Path.Combine(directoryInfo.FullName, "Images", "fauget1.ico");
                if (File.Exists(iconPath))
                {
                    nIcon.Icon = new System.Drawing.Icon(iconPath);
                }
            }
            nIcon.Visible = true;
            nIcon.Click -= nIcon_Click;
            nIcon.Click += nIcon_Click;
        }

        void nIcon_Click(object sender, EventArgs e)
        {
            if (currWindow != null && currWindow is Window window)
            {
                window.Visibility = Visibility.Visible;
                window.WindowState = WindowState.Normal;
            }
        }

        public void SetAccount(Account account)
        {
            this.account = account;
        }

        public void SetCurrWindow(Window window)
        {
            currWindow = window;
        }

        public static async void Closing_Window(object sender, CancelEventArgs e)
        {
            var app = System.Windows.Application.Current as App;
            if (!app._isAutoClosing)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show(
                    "Have order Done?", "Question",
                    MessageBoxButton.YesNo, MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    if (sender is Window window)
                    {
                        var userContainer = (app.currWindow as UserContainer);
                        // Dừng ghi âm nếu đang ghi
                        if (app?.account != null && userContainer._recordInstance._isRecording)
                        {
                            userContainer._recordInstance.StopRecording(userContainer._recordInstance.finalPath, app.account);
                        }
                        app.Shutdown();
                    }
                }
                else
                {
                    if (sender is Window window)
                    {
                        e.Cancel = true;
                        window.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        private static async Task ShutdownPythonAndApp(App app)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => PythonEngine.Shutdown());

                app.Shutdown();
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                Console.WriteLine($"Lỗi khi tắt ứng dụng: {ex.Message}");
            }
        }


        public void SubscribeClosingEvent(Window window)
        {
            window.Closing -= Closing_Window;
            window.Closing += Closing_Window;
        }
    }

}
