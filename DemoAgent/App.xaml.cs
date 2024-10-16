using HotelManagement.Util;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NAudio.CoreAudioApi;
using Python.Runtime;
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
        public dynamic? _processor;
        public dynamic? _model;
        public dynamic? _device;


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
                    PythonEngine.Shutdown();
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

        private void InitializePython()
        {
           // Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", @"C:\Users\Admin\AppData\Local\Programs\Python\Python312\python312.dll");
            if (!PythonEngine.IsInitialized)
                PythonEngine.Initialize();
            try
            {
                using (Py.GIL())
                {
                    dynamic speechRecogitionScript = Py.Import("SpeechRecognition");
                    if (_model is null)
                        _model = speechRecogitionScript.load_model();
                    if (_processor is null)
                        _processor = speechRecogitionScript.load_processor();
                    if (_device is null)
                        _device = speechRecogitionScript.get_device();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

}
