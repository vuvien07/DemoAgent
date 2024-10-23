using DemoAgent.Util;
using FontAwesome.WPF;
using Microsoft.IdentityModel.Tokens;
using Models;
using NAudio.Wave;
using Python.Runtime;
using Repositories;
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
        private App app = System.Windows.Application.Current as App;
        private bool _isManualRecord = false;
        private WaveFileWriter fileWriter;
        public bool _isRecording;
        public WaveInEvent waveInEvent;
        private ManagementEventWatcher connectWatcher;
        private ManagementEventWatcher disconnectWatcher;
        public string finalPath;
        public string _recordMode;
        public int _count = 0;
        public string transcriptionPath;
        public bool _isCompleteTask = false;
        private long totalBytesRecorded = 0;
        //gioi han so luong tac vu chay dong thoi tren CPU
        private SemaphoreSlim semaphore = new SemaphoreSlim(3);

        private static object _lock = new object();

        public Record(Account account)
        {
            InitializeComponent();
            this.account = account;
            if (!_isRecording)
            {
                timeSpan = TimeSpan.Zero;
                SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
            }
            if (processWavFiles == null)
            {
                processWavFiles = new List<string>();
            }
        }

        public async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceCombobox.ItemsSource = GetDevices();
            StartWatching(OnDeviceConnected, OnDeviceDisconnected);
            if (_isRecording)
            {
                UpdateUIForRecording();
            }
            if (!_isManualRecord)
            {
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
            if (!_isRecording)
            {
                wavFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}";
                transFile = $"{DateTime.Now:yyyyMMdd_HHmmss}_{account.Username}.txt";
            }
            updateIcon(FontAwesomeIcon.Square, (app.currWindow as UserContainer).BtStopRecord, "IcoStopRecord");
            if (!Directory.Exists(transcriptDirectory))
            {
                Directory.CreateDirectory(transcriptDirectory);
            }
            finalPath = System.IO.Path.Combine(recordDirectory, wavFile);
            transcriptionPath = System.IO.Path.Combine(transcriptDirectory, transFile);
            StartMonitoring();
            StartRecordingAudio(0, finalPath);
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
                DeviceCombobox.ItemsSource = GetDevices();
            });
        }

        private void OnDeviceDisconnected(EventArrivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {

                DeviceCombobox.ItemsSource = GetDevices();
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_isRecording)
            {
                timeSpan = (TimeSpan)GetTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"));
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
            SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
            StopMonitoring();
            string recordDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            string transcriptDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Transcription");
            StopRecording(finalPath, account);
            StopWatching();
            processWavFiles.Add(finalPath);
            fileWavProcess(finalPath);
            Task.Run(async () =>
           {
               try
               {
                   await processTranscribeAllWavFiles(processWavFiles, transcriptDirectory, app);
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
            if (_isRecording)
            {
                timeSpan = (TimeSpan)GetTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"));
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

        private void ProcessAudioTranscribe(string fileName)
        {
            Dispatcher.Invoke(() =>
             {
                 NoticeLable.Visibility = Visibility.Visible;
                 ProcessWavLabel.Text = $"Processing transcribe audio for file {fileName}";
             });
        }

        public void StartRecording(int selectedDevice, string outputFilePath)
        {
            this.finalPath = outputFilePath;
            waveInEvent = new WaveInEvent
            {
                DeviceNumber = selectedDevice,
                WaveFormat = new WaveFormat(16000, 16, 1)
            };

            fileWriter = new WaveFileWriter(outputFilePath, waveInEvent.WaveFormat);

            waveInEvent.DataAvailable += OnDataAvailable;

            waveInEvent.StartRecording();
            _isRecording = true;
            finalPath = outputFilePath;
        }

        public List<dynamic> GetDevices()
        {
            List<dynamic> deviceDynamics = new List<dynamic>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                deviceDynamics.Add(new
                {
                    Id = i,
                    ProductName = WaveInEvent.GetCapabilities(i).ProductName
                });
            }
            return deviceDynamics;
        }

        public void StartRecordingAudio(int selectedDevice, string outputFilePath)
        {
            this.finalPath = outputFilePath;
            waveInEvent = new WaveInEvent
            {
                DeviceNumber = selectedDevice,
                WaveFormat = new WaveFormat(16000, 16, 1)
            };

            fileWriter = new WaveFileWriter(outputFilePath, waveInEvent.WaveFormat);

            waveInEvent.DataAvailable += OnDataAvailable;

            waveInEvent.StartRecording();
            _isRecording = true;
            finalPath = outputFilePath;
        }

        public void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            totalBytesRecorded += e.BytesRecorded;

            while (totalBytesRecorded >= 32000)
            {
                timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
                totalBytesRecorded -= 32000;
            }
            // Ghi dữ liệu âm thanh vào file, nhưng sau khi điều chỉnh âm lượng
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                // Đọc mẫu âm thanh
                short sample = BitConverter.ToInt16(e.Buffer, index);

                // Điều chỉnh âm lượng
                int adjustedSample = (int)(sample * 2.0f);

                // Đảm bảo giá trị không vượt quá giới hạn của short
                adjustedSample = Math.Max(short.MinValue, Math.Min(short.MaxValue, adjustedSample));

                // Ghi lại mẫu đã điều chỉnh vào buffer
                byte[] adjustedBytes = BitConverter.GetBytes((short)adjustedSample);
                e.Buffer[index] = adjustedBytes[0];
                e.Buffer[index + 1] = adjustedBytes[1];
            }

            // Ghi dữ liệu đã điều chỉnh vào file
            if (fileWriter != null) // Kiểm tra để tránh lỗi NullReferenceException
            {
                fileWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }

            // Chuyển đổi mẫu thành giá trị float (-1 đến 1) để xử lý thêm
            float[] audioData = new float[e.BytesRecorded / 2];
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, index);
                audioData[index / 2] = sample / 32768f;
            }
            Dispatcher.Invoke(() =>
            {
                UpdateWaveform(audioData);
            });
        }

        public void StopRecording(string finalePath, Account account)
        {
            if (_isRecording)
            {
                try
                {
                    timeSpan = TimeSpan.Zero;
                    waveInEvent.StopRecording();
                    waveInEvent.Dispose();
                    waveInEvent = null;
                    fileWriter.Dispose();
                    fileWriter = null;
                    _isRecording = false;
                    EventUtil.printNotice($"Save record successfully!", MessageUtil.SUCCESS);
                }
                catch (Exception)
                {
                    EventUtil.printNotice($"An error occured while save recording file!", MessageUtil.ERROR);
                }
            }
        }


        public void StartWatching(Action<EventArrivedEventArgs> onDeviceConnected, Action<EventArrivedEventArgs> onDeviceDisconnected)
        {
            connectWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"));
            connectWatcher.EventArrived += (sender, e) => onDeviceConnected(e);
            connectWatcher.Start();

            disconnectWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'"));
            disconnectWatcher.EventArrived += (sender, e) => onDeviceDisconnected(e);
            disconnectWatcher.Start();
        }

        public void StopWatching()
        {
            if (connectWatcher != null)
            {
                connectWatcher.Stop();
                connectWatcher.Dispose();
                connectWatcher = null;
            }

            if (disconnectWatcher != null)
            {
                disconnectWatcher.Stop();
                disconnectWatcher.Dispose();
                disconnectWatcher = null;
            }
        }

        public void SaveTimeSpan(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        public TimeSpan? GetTimeSpan(string path)
        {
            if (File.Exists(path))
            {
                string timeSpanString = File.ReadAllText(path);
                if (TimeSpan.TryParse(timeSpanString, out TimeSpan timeSpan))
                {
                    return timeSpan;
                }
            }
            return null;
        }

        public bool CheckCurrentTimeBetweenStartTimeAndEndTime(Account account)
        {
            if (account != null)
            {
                Meeting meeting = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
                if (meeting != null)
                {
                    return meeting.TimeStart <= DateTime.Now && DateTime.Now <= meeting.TimeEnd;
                }

            }
            return false;
        }

        public List<WavFile> GetAllWaveFilesInDirectory(string path, string? name)
        {
            List<WavFile> waveFiles = new List<WavFile>();
            string[] files = Directory.GetFiles(path);
            try
            {
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Exists && fileInfo.Extension.Equals(".cnp", StringComparison.OrdinalIgnoreCase))
                    {
                        if (name.IsNullOrEmpty() || fileInfo.Name.Contains(name))
                        {
                            waveFiles.Add(new WavFile
                            {
                                Name = fileInfo.Name,
                                Type = fileInfo.Extension,
                                Size = FormatFileSize(fileInfo.Length),
                                Description = fileInfo.CreationTime.ToString(),
                                Path = fileInfo.FullName
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.GetBaseException();
            }
            waveFiles.Reverse();
            return waveFiles;
        }

        public void RemoveWavFile(List<WavFile> files, string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    WavFile? wav = files.FirstOrDefault(f => f.Name == path);
                    if (wav != null)
                    {
                        files.Remove(wav);
                    }
                }
                EventUtil.printNotice($"Remove file with path {path} successfully!", MessageUtil.SUCCESS);
            }
            catch (Exception)
            {
                EventUtil.printNotice("An error occured!", MessageUtil.ERROR);
            }
        }


        private string FormatFileSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
            {
                return $"{(double)bytes / GB:F2} GB";
            }
            else if (bytes >= MB)
            {
                return $"{(double)bytes / MB:F2} MB";
            }
            else if (bytes >= KB)
            {
                return $"{(double)bytes / KB:F2} KB";
            }
            else
            {
                return $"{bytes} bytes";
            }
        }

        public async Task processTranscribeAllWavFiles(List<string> processWavFiles, string transDir, dynamic app)
        {
            List<Task> tasks = new List<Task>();
            foreach (string file in processWavFiles)
            {
                string transFile = $"{System.IO.Path.GetFileNameWithoutExtension(file)}.txt";
                string transPath = System.IO.Path.Combine(transDir, transFile);
                tasks.Add(ProcessSingleFileAsync(processWavFiles, file, transPath, app));
            }
            await Task.WhenAll(tasks);
            OffNoticeLabel();
        }
        public void fileWavProcess(string wavPath)
        {
            try
            {
                string encryptedWavName = $"{System.IO.Path.GetFileNameWithoutExtension(wavPath)}.cnp";
                string encryptedWavPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(wavPath), encryptedWavName);
                UtilHelper.EncryptFile(wavPath, encryptedWavPath, account.PublicKey);
            }
            catch (Exception ex)
            {
                ex.GetBaseException();
            }
        }
        // Hàm xử lý từng file WAV
        private async Task ProcessSingleFileAsync(List<string> processWavFiles, string wavPath, string transPath, dynamic app)
        {
            await semaphore.WaitAsync();
            try
            {
                ProcessAudioTranscribe(wavPath);
                string result = await performRecognizeText(wavPath, app._model, app._processor, app._device, app._punctuationModel);
                await Task.Run(() =>
                {
                    using (StreamWriter sw = new StreamWriter(transPath))
                    {
                        sw.WriteLine(result);
                    }
                    string encryptedTransName = $"{System.IO.Path.GetFileNameWithoutExtension(transPath)}.cnp";
                    string encryptedTransPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(transPath), encryptedTransName);
                    UtilHelper.EncryptFile(transPath, encryptedTransPath, account.PublicKey);
                    File.Delete(transPath);
                    File.Delete(wavPath);
                    processWavFiles.Remove(wavPath);
                });
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<string> performRecognizeText(string wavPath, dynamic model, dynamic processor, dynamic device, dynamic punctuationModel)
        {
            return await Task<string>.Run(() =>
            {
                lock (_lock)
                {
                    using (PyModule pyModule = Py.CreateScope())
                    {
                        // Định nghĩa biến trong phạm vi
                        pyModule.Set("wavPath", wavPath);
                        pyModule.Set("model", model);
                        pyModule.Set("processor", processor);
                        pyModule.Set("device", device);
                        pyModule.Set("punctuation_model", punctuationModel);
                        // Chạy mã Python
                        pyModule.Exec(@"
import io
import soundfile as sf
import librosa
import torch
import numpy as np


def audio_transcribe(wavPath, model, processor, device, punctuation_model):
    try:
        # Read audio from bytes
        audio_input, sample_rate = sf.read(wavPath)

        # Ensure that the audio has the correct sample rate
        if sample_rate != 16000:
            audio_input = librosa.resample(audio_input, orig_sr=sample_rate, target_sr=16000)

        chunk_size = 10 * 16000  # Kích thước đoạn (10 giây)
        overlap_size = 2 * 16000  # Kích thước chồng lắp (2 giây)
        transcripts = []

        for i in range(0, len(audio_input), chunk_size - overlap_size):
            # Lấy đoạn âm thanh hiện tại
            chunk = audio_input[i:i + chunk_size]

            # Kiểm tra nếu đoạn âm thanh không đủ dài
            if len(chunk) < chunk_size:
                break  # Dừng nếu không còn đủ dữ liệu

            input_values = processor(chunk, return_tensors=""pt"", padding=""longest"").input_values
            input_values = input_values.to(device)

            with torch.no_grad():
                logits = model(input_values).logits

            predicted_ids = torch.argmax(logits, dim=-1)

            transcription = processor.decode(predicted_ids[0])
            transcripts.append(transcription)
        
        full_transcription = ' '.join(transcripts)

        final_result = punctuation_model.restore_punctuation(full_transcription)
        return final_result
    
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
                                pyModule.GetAttr("device"),
                                pyModule.GetAttr("punctuation_model")
                            };
                        var transcription = pyModule.InvokeMethod("audio_transcribe", pyObject);
                        _isCompleteTask = true;
                        return transcription.As<string>();
                    }
                }
            });
        }
    }
}
