using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CircleBeat
{
    public partial class MainWindow : Window
    {
        private const int SLIDER_COUNT = 30;
        private const int CALIBRATION_COUNT = 30; // Number of recent intervals to consider for averaging

        private ulong _msPassedSinceStart = 0;
        private ulong _lastPressMS = 0;

        private float BPMS { get => 60000 / _bpm; set => _bpm = 60000 / value; }
        private float _bpm = 1;

        private int NowAtSlider { get => _nowAtSlider; set { _nowAtSlider = value; if (_nowAtSlider == SLIDER_COUNT) _nowAtSlider = 0; } }
        private int _nowAtSlider = 0;

        private bool _isPlaying = false;
        private Queue<float> _recentIntervals = new Queue<float>(); // Queue to store recent calibration intervals

        public MainWindow()
        {
            InitializeComponent();

            Media.LoadedBehavior = MediaState.Manual;
            Media.UnloadedBehavior = MediaState.Manual;

            new Thread(UpdateLoop).Start();
            KeyDown += OnKeyDown;
        }

        private TimeSpan elapsed;
        public void UpdateLoop()
        {
            while (true)
            {
                _msPassedSinceStart += 1;

                Dispatcher.Invoke(() =>
                {
                    BeatSldr.Value = _msPassedSinceStart % BPMS / (BPMS / 10f);
                    CountLbl.Content = _msPassedSinceStart;
                });

                Thread.Sleep(1);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (BPMLbl.IsFocused)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        {
                            Keyboard.ClearFocus();
                            return;
                        }
                    case Key.Enter:
                        {
                            BPMLbl_Completed();
                            Keyboard.ClearFocus();
                            return;
                        }
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        {
                            Environment.Exit(0);
                            return;
                        }
                    case Key.P:
                        {
                            PausePlay();
                            return;
                        }
                    case Key.A:
                        {
                            Calibrate();
                        }
                        break;
                }

                CreateNewSlider();
            }
        }

        private void CreateNewSlider()
        {
            Slider slider = new() { Value = _msPassedSinceStart % BPMS / (BPMS / 10f) };

            SliderStack.Children.Insert(NowAtSlider, slider);

            if (SliderStack.Children.Count > SLIDER_COUNT)
            {
                if (NowAtSlider == SLIDER_COUNT)
                    SliderStack.Children.RemoveAt(0);
                else
                    SliderStack.Children.RemoveAt(NowAtSlider + 1);
            }

            NowAtSlider++;
        }

        private void PausePlay()
        {
            if (_isPlaying)
                Media.Pause();
            else
                Media.Play();

            _isPlaying = !_isPlaying;
        }

        private void Calibrate()
        {
            float interval = _msPassedSinceStart - _lastPressMS;

            if (_recentIntervals.Count == CALIBRATION_COUNT)
            {
                _recentIntervals.Dequeue();
            }

            _recentIntervals.Enqueue(interval);

            // Calculate the average interval
            float averageInterval = _recentIntervals.Average();
            BPMS = averageInterval;

            _lastPressMS = _msPassedSinceStart;

            BPMLbl.Text = _bpm.ToString();
        }

        private void PickFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Audio files (*.mp3, *.wav)|*.mp3;*.wav|All files (*.*)|*.*" };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                Media.Source = new(selectedFilePath);
                Media.Play();
                _isPlaying = true;
            }
        }

        private void BPMLbl_Completed()
        {
            float.TryParse(BPMLbl.Text, out _bpm);
        }
    }
}