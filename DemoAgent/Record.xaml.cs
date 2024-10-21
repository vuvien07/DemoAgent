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
        private DispatcherTimer timer;
        private TimeSpan timeSpan = TimeSpan.Zero;
        private Account account;
        private List<WavFile> files;
        private List<string> processWavFiles;
        private bool isMeeting;
        private RecordService? recordService = RecordService.Instance;
        private App app = System.Windows.Application.Current as App;

        public Record(Account account, bool isMeeting)
        {
            InitializeComponent();
            this.account = account;
            this.isMeeting = isMeeting;
            if (!recordService.IsRecording())
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
            string wavFile, transFile = "";
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
                if (recordService._recordMode == "Automatic")
                {
                    Meeting meet = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
                    wavFile = $"{meet.Name}";
                }
                else
                {
                    wavFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}";
                }
            }
            updateIcon(FontAwesomeIcon.Square);
            if (!Directory.Exists(transcriptDirectory))
            {
                Directory.CreateDirectory(transcriptDirectory);
            }
            switch (recordService._recordMode)
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
            Meeting meet = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
            String time = "";
            if (recordService._recordMode == "Automatic")
            {
                if (recordService.CheckCurrentTimeBetweenStartTimeAndEndTime(account) && recordService.IsRecording())
                {
                    timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                    recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
                    TimerLabel.Content = $"Record time: {timeSpan.ToString(@"hh\:mm\:ss")}";
                    FileNameLable.Content = System.IO.Path.GetFileName(recordService.finalPath);
                }
                else
                {
                    if (isMeeting && timer != null)
                    {
                        StopRecord();
                    }
                }
            }
            else
            {
                if (recordService.IsRecording())
                {
                    timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                    recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
                    TimerLabel.Content = $"Record time: {timeSpan.ToString(@"hh\:mm\:ss")}";
                    FileNameLable.Content = System.IO.Path.GetFileName(recordService.finalPath);
                }
            }

        }

        private void StopRecord()
        {
            TimerLabel.Content = "Record time:";
            FileNameLable.Content = "";
            timeSpan = TimeSpan.Zero;
            recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
            StopMonitoring();
            string recordDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            string transcriptDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Transcription");
            updateIcon(FontAwesomeIcon.Microphone);
            recordService.StopRecording(recordService.finalPath, account);
            recordService.StopWatching();
            recordService._recordMode = MessageUtil.RECORD_MANUAL;
            if (isMeeting)
            {
                var meeting = DemoAgentContext.INSTANCE.Meetings.FirstOrDefault(x => x.StatusId == 3);
                if (meeting != null)
                {
                    meeting.StatusId = 4;
                    DemoAgentContext.INSTANCE.Meetings.Update(meeting);
                    DemoAgentContext.INSTANCE.SaveChanges();
                }
                isMeeting = false;
            }
            processWavFiles.Add(recordService.finalPath);
            recordService.fileWavProcess(recordService.finalPath);
            LoadFiles();
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
            //_ = Task.Run(() =>
            //{
            //    OnProcessAudioTranscribe(recordService.finalPath);
            //}).ConfigureAwait(false);

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
            if (recordService.IsRecording()
                || recordService.CheckCurrentTimeBetweenStartTimeAndEndTime(account))
            {
                timeSpan = (TimeSpan)recordService.GetTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"));
                StartMonitoring();
                updateIcon(FontAwesomeIcon.Square);
                TimerLabel.Content = $"Record time: {timeSpan.ToString(@"hh\:mm\:ss")}";
                FileNameLable.Content = System.IO.Path.GetFileName(recordService.finalPath);
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

        private string performRecognizeText(string wavPath, dynamic app)
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
                    result = app._punctuationModel.restore_punctuation(result).As<string>();
                }
            }
            return result;
        }
    }
}
