using DemoAgent.Util;
using FontAwesome.WPF;
using Models;
using NAudio.Wave;
using Python.Runtime;
using Repositories;
using Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Util;
using static Azure.Core.HttpHeader;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for Record.xaml
    /// </summary>
    public partial class Record : System.Windows.Controls.UserControl
    {
        public DispatcherTimer timer;
        private TimeSpan timeSpan = TimeSpan.Zero;
        private Account account;
        private List<string> processWavFiles;
        private RecordService? recordService = RecordService.Instance;
        private App app = System.Windows.Application.Current as App;
        private bool _isManualRecord = false;

        public Record(Account account)
        {
            InitializeComponent();
            this.account = account;
            if (!recordService._isRecording)
            {
                recordService.InitializeService(account);
                timeSpan = TimeSpan.Zero;
                recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
            }
            recordService.OnAudioDataAvailable += OnAudioDataAvailable;
            if (processWavFiles == null)
            {
                recordService.OnProcessAudioTranscribe += OnProcessAudioTranscribe;
                recordService.OffNoticeLabel += OffNoticeLabel;
                processWavFiles = new List<string>();
            }
        }

        public async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceCombobox.ItemsSource = recordService.GetDevices();
            recordService.StartWatching(OnDeviceConnected, OnDeviceDisconnected);
            if (recordService._isRecording)
            {
                UpdateUIForRecording();
            }
            if(!_isManualRecord) {
                _isManualRecord = true;
                MessageBoxResult result = await Task.Run(() =>
                {
                    return System.Windows.MessageBox.Show(
                "Have order Done?", "Question",
                MessageBoxButton.YesNo, MessageBoxImage.Question
                    );
                });
                if (result == MessageBoxResult.Yes)
                {
                    StartRecord();
                }
                else
                {
                    (app.currWindow as UserContainer).BtStopRecord.IsEnabled = true;
                }
            }
        }

        public void StartRecord()
        {
            (app.currWindow as UserContainer).BtStopRecord.IsEnabled = true;
            string wavFile = "", transFile = "";
            string recordDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            string transcriptDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Transcription");
            if (!Directory.Exists(recordDirectory))
            {
                Directory.CreateDirectory(recordDirectory);
            }
            if (!recordService._isRecording)
            {
                wavFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}";
                transFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}.txt";
            }
            updateIcon(FontAwesomeIcon.Square, (app.currWindow as UserContainer).BtStopRecord, "IcoStopRecord");
            if (!Directory.Exists(transcriptDirectory))
            {
                Directory.CreateDirectory(transcriptDirectory);
            }
            recordService.finalPath = System.IO.Path.Combine(recordDirectory, wavFile);
            recordService.transcriptionPath = System.IO.Path.Combine(transcriptDirectory, transFile);
            StartMonitoring();
            recordService.StartRecording(0, recordService.finalPath);
            UpdateUIForRecording();
        }

        private void StartMonitoring()
        {
            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick -= Timer_Tick;
                timer.Tick += Timer_Tick;
            }
            else
            {
                timer.Tick -= Timer_Tick;
                timer.Tick += Timer_Tick;
            }
            timer.Start();
        }

        private void StopMonitoring()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;
                timer = null;
            }
        }


        private void OnDeviceConnected(EventArrivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DeviceCombobox.ItemsSource = recordService.GetDevices();
            });
        }

        private void OnDeviceDisconnected(EventArrivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {

                DeviceCombobox.ItemsSource = recordService.GetDevices();
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (recordService._isRecording)
            {
                timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
                (app.currWindow as UserContainer).RecordTime.Visibility = Visibility.Visible;
                (app.currWindow as UserContainer).RecordTime.Text = timeSpan.ToString(@"hh\:mm\:ss");
                (app.currWindow as UserContainer).BtPauseRecord.Visibility = Visibility.Visible;
            }
        }

        public void StopRecord()
        {
            Dispatcher.Invoke(() =>
            {
                FileNameLable.Content = "";
            });
            timeSpan = TimeSpan.Zero;
            recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
            StopMonitoring();
            string recordDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            string transcriptDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Transcription");
            recordService.StopRecording(recordService.finalPath, account);
            recordService.StopWatching();
            processWavFiles.Add(recordService.finalPath);
            recordService.fileWavProcess(recordService.finalPath);
            Task.Run(async () =>
           {
               try
               {
                   await recordService.processTranscribeAllWavFiles(processWavFiles, transcriptDirectory, app);
               }
               catch (Exception ex)
               {
                   // Xử lý ngoại lệ nếu cần
                   Console.WriteLine($"Error: {ex.Message}");
               }
           });
        }

        private void OffNoticeLabel()
        {
            Dispatcher.Invoke(() =>
            {
                NoticeLable.Visibility = Visibility.Collapsed;
            });
        }

        private void UpdateUIForRecording()
        {
            if (recordService._isRecording)
            {
                timeSpan = (TimeSpan)recordService.GetTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"));
                StartMonitoring();
                updateIcon(FontAwesomeIcon.Square, (app.currWindow as UserContainer).BtStopRecord, "IcoStopRecord");
            
                (app.currWindow as UserContainer).RecordTime.Visibility = Visibility.Visible;
                (app.currWindow as UserContainer).RecordTime.Text = timeSpan.ToString(@"hh\:mm\:ss");
                (app.currWindow as UserContainer).BtPauseRecord.Visibility = Visibility.Visible;

            }

        }

        public void updateIcon(FontAwesomeIcon icon, ToggleButton button, string target)
        {
            var iconImage = button.FindName(target) as ImageAwesome;
            if (iconImage != null)
            {
                iconImage.Icon = icon;
            }
        }

        private void UpdateWaveform(float[] audioData)
        {
            // Xóa canvas cũ
            WaveformCanvas.Children.Clear();

            // Tạo Polyline để vẽ đường sóng âm
            var polyline = new Polyline
            {
                Stroke = new LinearGradientBrush(
                    Colors.LightBlue, Colors.Blue, 90), // Tạo hiệu ứng chuyển màu
                StrokeThickness = 4 // Độ dày đường kẻ
            };

            double centerY = WaveformCanvas.ActualHeight / 2;
            double width = WaveformCanvas.ActualWidth;
            double pointSpacing = width / audioData.Length;

            for (int i = 0; i < audioData.Length; i++)
            {
                double x = i * pointSpacing;
                double y = centerY - (audioData[i] * centerY);

                polyline.Points.Add(new System.Windows.Point(x, y));
            }

            WaveformCanvas.Children.Add(polyline);

            AddWaveformBackground(WaveformCanvas);
        }

        private void AddWaveformBackground(Canvas canvas)
        {
            var background = new System.Windows.Shapes.Rectangle
            {
                Width = canvas.ActualWidth,
                Height = canvas.ActualHeight,
                Fill = new LinearGradientBrush(
                    Colors.White, Colors.LightGray, 90) // Hiệu ứng gradient cho nền
            };

            canvas.Children.Insert(0, background); // Đặt nền phía sau đường sóng âm
        }

        private void OnProcessAudioTranscribe(string fileName)
        {
            Dispatcher.Invoke(() =>
             {
                 NoticeLable.Visibility = Visibility.Visible;
                 ProcessWavLabel.Text = $"Processing transcribe audio for file {fileName}";
             });
        }


        private void OnAudioDataAvailable(float[] audioData)
        {
            Dispatcher.Invoke(() => UpdateWaveform(audioData));
        }
    }
}
