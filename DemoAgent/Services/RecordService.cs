using DemoAgent.Util;
using Models;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Util;

namespace Services
{
    public class RecordService
    {
        public static RecordService Instance = new RecordService();

        private WaveFileWriter fileWriter;
        private bool isRecording;
        private WaveInEvent waveInEvent;
        private ManagementEventWatcher connectWatcher;
        private ManagementEventWatcher disconnectWatcher;
        private Account account;
        private string finalPath;
        private string _recordMode;
        public int _count = 0;
        public event Action<float[]> OnAudioDataAvailable;
        private string transcriptionPath;

        public string FinalePath { get => finalPath; set => finalPath = value; }
        public string RecordMode { get => _recordMode; set => _recordMode = value; }

        public string TranscriptionPath { get => transcriptionPath; set => transcriptionPath = value; }

        public void InitializeService(Account account)
        {
            this.account = account;
            _recordMode = MessageUtil.RECORD_MANUAL;
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

        public void StartRecording(int selectedDevice, string outputFilePath)
        {
            waveInEvent = new WaveInEvent
            {
                DeviceNumber = selectedDevice,
                WaveFormat = new WaveFormat(16000, 16, 1)
            };

            fileWriter = new WaveFileWriter(outputFilePath, waveInEvent.WaveFormat);
            float volumeFactor = 2.0f;  // Hệ số tăng âm lượng, tăng gấp đôi âm lượng

            waveInEvent.DataAvailable += (s, e) =>
            {
                // Ghi dữ liệu âm thanh vào file, nhưng sau khi điều chỉnh âm lượng
                for (int index = 0; index < e.BytesRecorded; index += 2)
                {
                    // Đọc mẫu âm thanh
                    short sample = BitConverter.ToInt16(e.Buffer, index);

                    // Điều chỉnh âm lượng
                    int adjustedSample = (int)(sample * volumeFactor);

                    // Đảm bảo giá trị không vượt quá giới hạn của short
                    adjustedSample = Math.Max(short.MinValue, Math.Min(short.MaxValue, adjustedSample));

                    // Ghi lại mẫu đã điều chỉnh vào buffer
                    byte[] adjustedBytes = BitConverter.GetBytes((short)adjustedSample);
                    e.Buffer[index] = adjustedBytes[0];
                    e.Buffer[index + 1] = adjustedBytes[1];
                }

                // Ghi dữ liệu đã điều chỉnh vào file
                fileWriter.Write(e.Buffer, 0, e.BytesRecorded);
                
                // Chuyển đổi mẫu thành giá trị float (-1 đến 1) để xử lý thêm
                float[] audioData = new float[e.BytesRecorded / 2];
                for (int index = 0; index < e.BytesRecorded; index += 2)
                {
                    short sample = BitConverter.ToInt16(e.Buffer, index);
                    audioData[index / 2] = sample / 32768f;
                }
                OnAudioDataAvailable?.Invoke(audioData);
            };

            waveInEvent.StartRecording();
            isRecording = true;
            finalPath = outputFilePath;
            FinalePath = finalPath;
        }

        public void StopRecording(string finalePath ,Account account)
        {
            if (isRecording)
            {
                try
                {
                    waveInEvent.StopRecording();
                    waveInEvent.Dispose();
                    fileWriter.Dispose();
                    isRecording = false;
                    if (File.Exists(finalePath))
                    {
                        string directoryPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        string encryptedFileName = $"{Path.GetFileName(finalePath)}.cnp";
                        string encryptedFilePath = Path.Combine(directoryPath, encryptedFileName);                        
                    }
                    var meeting = DemoAgentContext.INSTANCE.Meetings.FirstOrDefault(x => x.StatusId == 3);
                    if (meeting != null)
                    {
                        meeting.StatusId = 4;
                        DemoAgentContext.INSTANCE.Meetings.Update(meeting);
                        DemoAgentContext.INSTANCE.SaveChanges();
                    }
                    EventUtil.printNotice($"Save record successfully!", MessageUtil.SUCCESS);
                    _count = 0;
                }catch(Exception) {
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

        public bool IsRecording() => isRecording;

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

        public List<WavFile> GetAllWaveFilesInDirectory(string path)
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
    }
}
