using FontAwesome.WPF;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using Python.Runtime;
using Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for SpeechLive.xaml
    /// </summary>
    public partial class SpeechLive : UserControl
    {
        private DispatcherTimer _timer;
        private TimeSpan _timeSpan;
        public static bool _isRecording;
        private System.Timers.Timer deviceTimer;
        private int _previousDeviceCount;
        private WaveInEvent waveIn;
        private static List<byte> audioBuffer = new List<byte>();

        //Quy dinh 1 giay am thanh co do dai 32000 bit
        private int bufferSize = 16000 * 2 * 1;
        private Task _transcriptionTask;
        private Task _recognitionTask;
        private App app = System.Windows.Application.Current as App;
        //private ButterworthHighPassFilterService _butterworthHighPassFilter;
        //private FilterSoundService _filterSound;

        public SpeechLive()
        {
            InitializeComponent();
            StartMonitoringDevices();
        }

        private void StartMonitoringDevices()
        {
            deviceTimer = new System.Timers.Timer(1000);
            deviceTimer.Elapsed += OnTimerElapsed;
            deviceTimer.Start();

            _previousDeviceCount = WaveInEvent.DeviceCount;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            int currentDeviceCount = WaveInEvent.DeviceCount;
            if (currentDeviceCount != _previousDeviceCount)
            {
                Dispatcher.Invoke(() => LoadDevice());
                _previousDeviceCount = currentDeviceCount;
            }
        }

        private void LoadDevice()
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
            DeviceCombobox.ItemsSource = deviceDynamics;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timeSpan = _timeSpan.Add(TimeSpan.FromSeconds(1));
            TimerLabel.Content = _timeSpan.ToString(@"hh\:mm\:ss");
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
                return;

            var iconImage = button.Template.FindName("IconImage", button) as ImageAwesome;
            if (iconImage == null)
                return;

            if (_isRecording)
            {
                _timer.Stop();
                iconImage.Icon = FontAwesomeIcon.Microphone;
                _isRecording = false;
                StopRecognition();
            }
            else
            {
                _timeSpan = TimeSpan.Zero;
                TimerLabel.Content = _timeSpan.ToString(@"hh\:mm\:ss");
                _timer.Start();
                iconImage.Icon = FontAwesomeIcon.Square;
                _isRecording = true;
                StartRecognition();
            }
        }

        private void StartRecognition()
        {
            var waveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono
            waveIn = new WaveInEvent
            {
                WaveFormat = waveFormat,
                BufferMilliseconds = 1000 // 1 giây mỗi buffer
            };

            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;

            //_filterSound = new FilterSoundService(3000, waveIn.WaveFormat.SampleRate);
            //_butterworthHighPassFilter = new ButterworthHighPassFilterService(100, waveIn.WaveFormat.SampleRate);

            waveIn.StartRecording();
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            audioBuffer.AddRange(e.Buffer[0..e.BytesRecorded]);
            while (audioBuffer.Count >= bufferSize)
            {
                byte[] chunk;
                chunk = audioBuffer.GetRange(0, bufferSize).ToArray();
                //ProcessBitAudio(chunk, e);
                audioBuffer.RemoveRange(0, bufferSize);

                // Thêm chunk vào hàng đợi để xử lý sau
            }
        }

        /*
         * Xu ly nhieu xung quanh cua khoi byte
         */
        //private void ProcessBitAudio(byte[] chunk, WaveInEventArgs e)
        //{
        //    for (int i = 0; i < e.BytesRecorded; i += 2)
        //    {
        //        short sample = BitConverter.ToInt16(e.Buffer, i);
        //        float sampleFloat = sample / 32768f;

        //        float filteredSample = _filterSound.ProcessSample(sampleFloat);
        //        float highPassFilteredSample = _butterworthHighPassFilter.Apply(filteredSample);

        //        short filteredSampleShort = (short)(highPassFilteredSample * 32768f);
        //        byte[] filteredSampleBytes = BitConverter.GetBytes(filteredSampleShort);

        //        chunk[i] = filteredSampleBytes[0];
        //        chunk[i + 1] = filteredSampleBytes[1];
        //    }
        //}

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (audioBuffer.Count > 0)
            {
                byte[] remaining;
                lock (audioBuffer)
                {
                    remaining = audioBuffer.ToArray();
                    audioBuffer.Clear();
                }
            }
        }

        private void StopRecognition()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timeSpan = TimeSpan.Zero;
            _isRecording = false;
            LoadDevice();
        }

        /*
         * Ham khoi tao moi truong python
         */
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