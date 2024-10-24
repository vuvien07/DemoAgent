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
        public dynamic? _processor;
        public dynamic? _model;
        public dynamic? _device;
        public bool _isAutoClosing = false;
        public dynamic? _speechRecogition;
        public dynamic? _punctuationModel;

        public App()
        {
            InitializePython();
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
                        // Ẩn cửa sổ trước khi tắt
                        window.Visibility = Visibility.Hidden;
                        app.nIcon.Visible = false;

                        // Thực hiện tắt Python đồng bộ trước khi shutdown ứng dụng
                        await ShutdownPythonAndApp(app);
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

        private void InitializePython()
        {
            //Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", @"C:\Users\boot.AI\AppData\Local\Programs\Python\Python39\Python39.dll");
            if (!PythonEngine.IsInitialized)
                PythonEngine.Initialize();
            try
            {
                using (Py.GIL())
                {
                    _speechRecogition = Py.Import("SpeechRecognition");
                    if (_model is null)
                        _model = _speechRecogition.load_model();
                    if (_processor is null)
                        _processor = _speechRecogition.load_processor();
                    if (_device is null)
                        _device = _speechRecogition.get_device();
                    if (_punctuationModel is null)
                        _punctuationModel = _speechRecogition.load_punctuation_model();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

}