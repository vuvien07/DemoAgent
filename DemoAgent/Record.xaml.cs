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
        private DispatcherTimer timer;
        private TimeSpan timeSpan = TimeSpan.Zero;
        private Account account;
        private string finalePath;
        private string transPath;
        private List<WavFile> files;
        private bool isMeeting;
        private RecordService? recordService;
        private App app = System.Windows.Application.Current as App;
        private string wavFile = "";
        private string transFile = "";

        public Record(Account account, bool isMeeting)
        {
            InitializeComponent();
            this.account = account;
            this.isMeeting = isMeeting;
            if (recordService == null)
            {
                recordService = RecordService.Instance;
                recordService.InitializeService(account);
            }
            if (!recordService.IsRecording())
            {
                timeSpan = TimeSpan.Zero;
                recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
            }
            recordService.OnAudioDataAvailable += OnAudioDataAvailable;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceCombobox.ItemsSource = recordService.GetDevices();
            recordService.StartWatching(OnDeviceConnected, OnDeviceDisconnected);
            if (recordService.CheckCurrentTimeBetweenStartTimeAndEndTime(account) || recordService.IsRecording())
            {
                UpdateUIForRecording();
            }
            LoadFiles();
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            string directory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            if (!Directory.Exists(directory))
                if (recordService.IsRecording())
                {
                    StopRecord();
                    return;
                }
            updateIcon(FontAwesomeIcon.Square);
            string recordDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            string transcriptDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Transcription");
            if (!Directory.Exists(recordDirectory))
            {
                Directory.CreateDirectory(recordDirectory);
            }
            if (!recordService.IsRecording())
            {
                if (recordService.RecordMode == "Automatic")
                {
                    Meeting meet = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
                    wavFile = $"{meet.Name}";
                }
                else
                {
                    wavFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}";
                }
            }

            finalePath = System.IO.Path.Combine(directory, wavFile);
            if (recordService.IsRecording())
            {
                StopRecord();
                return;
            }
            updateIcon(FontAwesomeIcon.Square);
            if (!Directory.Exists(transcriptDirectory))
            {
                Directory.CreateDirectory(transcriptDirectory);
            }
            switch (recordService.RecordMode)
            {
                case "Manual":
                    wavFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}.wav";
                    transFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}.txt";
                    break;
                default:
                    Meeting meet = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
                    wavFile = $"{meet.Name}.wav";
                    transFile = $"{meet.Name}.txt";
                    break;
            }
            finalePath = System.IO.Path.Combine(recordDirectory, wavFile);
            transPath = System.IO.Path.Combine(transcriptDirectory, transFile);
            recordService.TranscriptionPath = System.IO.Path.Combine(transcriptDirectory, transFile);
            StartMonitoring();
            recordService.StartRecording(0, finalePath);
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
            Meeting meet = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
            String time = "";
            switch (recordService.RecordMode)
            {
                case "Automatic":
                    if (recordService.CheckCurrentTimeBetweenStartTimeAndEndTime(account) && recordService.IsRecording())
                    {
                        timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                        recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
                        TimerLabel.Content = $"Record time: {timeSpan.ToString(@"hh\:mm\:ss")}";
                        FileNameLable.Content = wavFile;
                        ;
                    }
                    else
                    {
                        StopRecord();
                    }
                    break;
                case "Manual":
                    if (recordService.IsRecording())
                    {
                        timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                        recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
                        TimerLabel.Content = $"Record time: {timeSpan.ToString(@"hh\:mm\:ss")}";
                        FileNameLable.Content = wavFile;
                    }
                    else
                    {
                        StopRecord();
                    }
                    break;
            }

        }

        private void StopRecord()
        {
            updateIcon(FontAwesomeIcon.Microphone);
            StopMonitoring();
            recordService.StopRecording(finalePath, account);
            recordService.StopWatching();
            TimerLabel.Content = $"00:00:00";
            timeSpan = TimeSpan.Zero;
            recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
            recordService.RecordMode = MessageUtil.RECORD_MANUAL;
            if (isMeeting)
            {
                var meeting = DemoAgentContext.INSTANCE.Meetings.FirstOrDefault(x => x.StatusId == 3);
                if (meeting != null)
                {
                    meeting.StatusId = 4;
                    DemoAgentContext.INSTANCE.Meetings.Update(meeting);
                    DemoAgentContext.INSTANCE.SaveChanges();
                }
            }
            string transcript = performRecognizeText(finalePath);
            using (StreamWriter sw = new StreamWriter(transPath))
            {
                sw.WriteLine(transcript);
            }
            File.Delete(finalePath);
            LoadFiles();
        }

        private void UpdateUIForRecording()
        {
            if (recordService.IsRecording()
                || recordService.CheckCurrentTimeBetweenStartTimeAndEndTime(account))
            {
                finalePath = recordService.FinalePath;
                timeSpan = (TimeSpan)recordService.GetTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"));
                StartMonitoring();
                updateIcon(FontAwesomeIcon.Square);
                TimerLabel.Content = $"{timeSpan.ToString(@"hh\:mm\:ss")}";
            }
            else
            {
                StopRecord();
            }

        }

        private void LoadFiles()
        {
            string directory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            if (!File.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            files = recordService.GetAllWaveFilesInDirectory(directory);
            lvRecordings.ItemsSource = files;
        }



        private void DecryptWavFile(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvRecordings.SelectedValue as WavFile;
            if (selectedFile != null)
            {
                string path = selectedFile.Path;
                if (path != null && File.Exists(path))
                {
                    using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            string folderPath = folderDialog.SelectedPath;
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                            string decryptedFilePath = System.IO.Path.Combine(folderPath, fileName + ".wav");

                            try
                            {
                                UtilHelper.DecryptFile(path, decryptedFilePath, account.PrivateKey);
                                EventUtil.printNotice($"The file has been successfully decrypted and saved at {folderPath}", MessageUtil.SUCCESS);
                            }
                            catch (Exception ex)
                            {
                                EventUtil.printNotice($"Error decrypting file: {ex.Message}", MessageUtil.ERROR);
                            }
                        }
                    }
                }
            }
        }

        public void updateIcon(FontAwesomeIcon icon)
        {
            var iconImage = RecordButton.Template.FindName("IconImage", RecordButton) as ImageAwesome;
            if (iconImage != null)
            {
                iconImage.Icon = icon;
            }
        }

        private void lvRecordings_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = false;
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


        private void OnAudioDataAvailable(float[] audioData)
        {
            Dispatcher.Invoke(() => UpdateWaveform(audioData));
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvRecordings.SelectedValue as WavFile;
            if (selectedFile != null)
            {
                try
                {
                    DialogResult dialogResult = System.Windows.Forms.MessageBox.Show($"Are you sure to delete this file at {selectedFile.Path}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dialogResult == DialogResult.Yes)
                    {
                        recordService.RemoveWavFile(files, selectedFile.Path);
                        LoadFiles();
                    }
                }
                catch (Exception) { }
            }
        }

        private void MenuItemOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvRecordings.SelectedValue as WavFile;
            if (selectedFile != null)
            {
                try
                {
                    // Mở thư mục chứa file đã chọn
                    string directoryPath = System.IO.Path.GetDirectoryName(selectedFile.Path);
                    if (Directory.Exists(directoryPath))
                    {
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = directoryPath,
                            UseShellExecute = true
                        };
                        Process.Start(processStartInfo);
                    }
                    else
                    {
                        EventUtil.printNotice("Thư mục không tồn tại!", MessageUtil.ERROR);
                    }
                }
                catch (Exception)
                {
                    EventUtil.printNotice("Đã xảy ra lỗi!", MessageUtil.ERROR);
                }
            }
        }
        private string performRecognizeText(string wavPath)
        {
            string result = "";
            using (PyModule pyModule = Py.CreateScope())
            {
                // Định nghĩa biến trong phạm vi
                pyModule.Set("wavPath", wavPath);
                pyModule.Set("model", app._model);
                pyModule.Set("processor", app._processor);
                pyModule.Set("device", app._device);

                // Chạy mã Python
                pyModule.Exec(@"
import io
import soundfile as sf
import librosa
import torch
import numpy as np


def audio_transcribe(wavPath, model, processor, device):
    try:
        # Read audio from bytes
        audio_input, sample_rate = sf.read(wavPath)

        # Ensure that the audio has the correct sample rate
        if sample_rate != 16000:
            audio_input = librosa.resample(audio_input, orig_sr=sample_rate, target_sr=16000)

        # Preprocess input data
        input_values = processor(audio_input, return_tensors=""pt"", padding=""longest"").input_values
        input_values = input_values.to(device)

        # Predict with the model
        with torch.no_grad():
            logits = model(input_values).logits

        predicted_ids = torch.argmax(logits, dim=-1)

        # Decode to text
        transcription = processor.decode(predicted_ids[0])

        return transcription
    
    except ValueError as ve:
        print(f""ValueError: {ve} - Ensure the audio bytes are valid and compatible."")
    except RuntimeError as re:
        print(f""RuntimeError: {re} - Check the model and processor compatibility with the input."")
    except Exception as e:
        print(f""An error occurred during audio transcription: {e} - Audio input type: {type(audio_input)}"")
                            ");
                PyObject[] pyObject = new PyObject[] {
                                pyModule.GetAttr("wavPath"),
                                pyModule.GetAttr("model"),
                                pyModule.GetAttr("processor"),
                                pyModule.GetAttr("device")
                            };
                var transcription = pyModule.InvokeMethod("audio_transcribe", pyObject);
                if (transcription != null && transcription is PyObject)
                {
                    result = transcription.As<string>();
                }
            }
            return result;
        }
    }
}
