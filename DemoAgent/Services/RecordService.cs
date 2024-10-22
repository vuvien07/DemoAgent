using DemoAgent;
using DemoAgent.Util;
using Models;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Python.Runtime;
using Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static RecordService instance;
        private static object _lock = new object();
        public static RecordService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new RecordService();
                    }
                    return instance;
                }
            }
        }

        private WaveFileWriter fileWriter;
        public bool _isRecording;
        private WaveInEvent waveInEvent;
        private ManagementEventWatcher connectWatcher;
        private ManagementEventWatcher disconnectWatcher;
        private Account account;
        public string finalPath;
        public string _recordMode;
        public int _count = 0;
        public event Action<float[]> OnAudioDataAvailable;
        public event Action<string> OnProcessAudioTranscribe;
        public event Action OffNoticeLabel;
        public string transcriptionPath;
        public bool _isCompleteTask = false;


        //gioi han so luong tac vu chay dong thoi tren CPU
        private SemaphoreSlim semaphore = new SemaphoreSlim(3);

        public void InitializeService(Account account)
        {
            this.account = account;
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
            this.finalPath = outputFilePath;
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
                OnAudioDataAvailable?.Invoke(audioData);
            };

            waveInEvent.StartRecording();
            _isRecording = true;
            finalPath = outputFilePath;
        }

        public void StopRecording(string finalePath, Account account)
        {
            if (_isRecording)
            {
                try
                {
                    waveInEvent.StopRecording();
                    waveInEvent.Dispose();
                    waveInEvent = null;
                    fileWriter.Dispose();
                    fileWriter = null;
                    _isRecording = false;
                    var meeting = DemoAgentContext.INSTANCE.Meetings.FirstOrDefault(x => x.StatusId == 3);
                    if (meeting != null)
                    {
                        meeting.StatusId = 4;
                        DemoAgentContext.INSTANCE.Meetings.Update(meeting);
                        DemoAgentContext.INSTANCE.SaveChanges();
                    }
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

        public async Task processTranscribeAllWavFiles(List<string> processWavFiles, string transDir, dynamic app)
        {
            List<Task> tasks = new List<Task>();
            foreach (string file in processWavFiles)
            {
                string transFile = $"{Path.GetFileNameWithoutExtension(file)}.txt";
                string transPath = Path.Combine(transDir, transFile);
                tasks.Add(ProcessSingleFileAsync(processWavFiles, file, transPath, app));
            }
            await Task.WhenAll(tasks);
            OffNoticeLabel?.Invoke();
        }
        public void fileWavProcess(string wavPath)
        {
            try
            {
                string encryptedWavName = $"{System.IO.Path.GetFileNameWithoutExtension(wavPath)}.cnp";
                string encryptedWavPath = System.IO.Path.Combine(Path.GetDirectoryName(wavPath), encryptedWavName);
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
                OnProcessAudioTranscribe?.Invoke(wavPath);
                string result = await performRecognizeText(wavPath, app._model, app._processor, app._device, app._punctuationModel);
                await Task.Run(() =>
                {
                    using (StreamWriter sw = new StreamWriter(transPath))
                    {
                        sw.WriteLine(result);
                    }
                    string encryptedTransName = $"{System.IO.Path.GetFileNameWithoutExtension(transPath)}.cnp";
                    string encryptedTransPath = System.IO.Path.Combine(Path.GetDirectoryName(transPath), encryptedTransName);
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