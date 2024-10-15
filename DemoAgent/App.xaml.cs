using HotelManagement.Util;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Services;
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
        public ServiceProvider? serviceProvider;
        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        public Window currWindow;
        public Account? account;

        public App()
        {
            string baseDirectory = Directory.GetCurrentDirectory();
            string iconPath = @"D:\CMCProject1\DemoAgentV2\DemoAgent\fullscreen_arrow_icon_263604 (1).ico";
            nIcon.Icon = new System.Drawing.Icon(iconPath);
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

        public static void Closing_Window(object sender, CancelEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Have order Done?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (sender is Window window)
                {
                    var app = System.Windows.Application.Current as App;
                    RecordService recordService = RecordService.Instance;
                    if (app.account != null)
                    {
                        if (recordService.IsRecording())
                        {
                            recordService.StopRecording(recordService.FinalePath, (System.Windows.Application.Current as App)?.account);
                        }
                    }
                    Window userContainer = System.Windows.Application.Current.Windows.OfType<UserContainer>().FirstOrDefault();
                    if (userContainer != null)
                    {
                        var s = (Viewbox)userContainer.FindName("ContainerUser");
                        if (s != null && s.Child is SpeechLive speechLive)
                        {
                            CancellationTokenSource tokenSource = speechLive._cancellationTokenSource;
                            tokenSource?.Cancel();
                            tokenSource?.Dispose();
                        }
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

        public void SubscribeClosingEvent(Window window)
        {
            window.Closing -= Closing_Window;
            window.Closing += Closing_Window;
        }
    }

}
