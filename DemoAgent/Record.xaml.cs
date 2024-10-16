using DemoAgent.Util;
using FontAwesome.WPF;
using Models;
using NAudio.Wave;
using Python.Runtime;
using Repositories;
using Services;
using System;
using System.Collections.Generic;
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
            if(recordService == null)
            {
                recordService = RecordService.Instance;
                recordService.InitializeService(account);
            }
            if (!recordService.IsRecording())
            {
                timeSpan = TimeSpan.Zero;
                recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
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
            if(!Directory.Exists(transcriptDirectory))
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
                timer = new DispatcherTimer(); // Kiểm tra mỗi giây
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
            switch (recordService.RecordMode)
            {
                case "Automatic":
                    if (recordService.CheckCurrentTimeBetweenStartTimeAndEndTime(account) && recordService.IsRecording())
                    {
                        timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
                        recordService.SaveTimeSpan(System.IO.Path.Combine(Environment.CurrentDirectory, "timeSpan.txt"), timeSpan.ToString(@"hh\:mm\:ss"));
                        TimerLabel.Content = $"Record time: {timeSpan.ToString(@"hh\:mm\:ss")}";
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
            TimerLabel.Content = $"Record time:";
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
            byte[] audioBytes = recordService.audioStream.ToArray();
            string transcript = performRecognizeText(audioBytes);
            using (StreamWriter sw = new StreamWriter(transPath))
            {
                sw.WriteLine(transcript);
            }
            recordService.audioStream?.Flush();
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
                TimerLabel.Content = $"Record time: {timeSpan.ToString(@"hh\:mm\:ss")}";
            }
            else
            {
                StopRecord();
            }

        }

        private List<WavFile> GetFiles()
        {
            return files;
        }

        private void LoadFiles()
        {
            string directory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            if(!File.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            files = recordService.GetAllWaveFilesInDirectory(directory);
            lvRecordings.ItemsSource = files;
        }

        private void RemoveFile(object sender, MouseButtonEventArgs e)
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

        private void DecryptWavFile(object sender, MouseButtonEventArgs e)
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

                            // Decrypt the .cnp file to a .wav file
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
            // Cuộn trong ScrollViewer
            if (scrollViewer != null)
            {
                // Điều chỉnh cuộn theo số lượng di chuyển
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = false; // Ngăn không cho sự kiện tiếp tục
            }
        }

        private string performRecognizeText(byte[] audioBytes)
        {
            string result = "";
            using (PyModule pyModule = Py.CreateScope())
            {
                // Định nghĩa biến trong phạm vi
                pyModule.Set("audio_bytes", audioBytes);
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


def audio_transcribe(audio_bytes, model, processor, device, sampling_rate=16000):
    try:
        # Read audio from bytes
        audio_data, sr = sf.read(
            io.BytesIO(audio_bytes),
            format='RAW',
            channels=1,
            samplerate=sampling_rate,
            subtype='PCM_16'
        )

        # Ensure that the audio has the correct sample rate
        if sr != sampling_rate:
            audio_data = librosa.resample(audio_data, orig_sr=sr, target_sr=sampling_rate)

        # Preprocess input data
        input_values = processor(audio_data, return_tensors=""pt"", padding=""longest"").input_values
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
                                pyModule.GetAttr("audio_bytes"),
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
