using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CircleBeat;

public partial class MainWindow : Window
{
    private const int SLIDER_COUNT = 5;

    private ulong _milisecsPassedSinceStart = 0;
    private ulong _firstPressMili = 0;

    private float MBP { get => 60000 / _bpm; set => _bpm = 60000 / value; }
    private float _bpm = 187;

    private int NowAt { get => _nowAt; set { _nowAt = value; if (_nowAt == SLIDER_COUNT + 1) _nowAt = 0; } }
    private int _nowAt = 0;

    private bool _isPlaying = false;

    public MainWindow()
    {
        InitializeComponent();

        Media.LoadedBehavior = MediaState.Manual;
        Media.UnloadedBehavior = MediaState.Manual;

        new Thread(UpdateLoop).Start();
        KeyDown += OnKeyDown;
    }

    public void UpdateLoop()
    {
        while (true)
        {
            _milisecsPassedSinceStart++;

            Dispatcher.Invoke(() =>
            {
                BeatSldr.Value = _milisecsPassedSinceStart % MBP / (MBP / 10f);
                CountLbl.Content = _milisecsPassedSinceStart;
            });

            Thread.Sleep(1);
        }
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Escape)
        {
            Close();
            return;
        }
        else if (e.Key is System.Windows.Input.Key.A)
        {
            Calibrate();
            return;
        }
        else if (e.Key is System.Windows.Input.Key.P)
        {
            PausePlay();
            return;
        }

        Tap();
    }

    private void Tap()
    {
        Slider slider = new() { Value = _milisecsPassedSinceStart % MBP / (MBP / 10f) };

        MainStack.Children.Insert(NowAt, slider);

        if (MainStack.Children.Count > SLIDER_COUNT)
        {
            if (NowAt == SLIDER_COUNT)
                MainStack.Children.RemoveAt(0);
            else
                MainStack.Children.RemoveAt(NowAt + 1);
        }

        NowAt++;
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
        MBP = _milisecsPassedSinceStart - _firstPressMili;

        BPMLbl.Content = _bpm;

        _firstPressMili = _milisecsPassedSinceStart;
    }

    private void PickFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog();
        openFileDialog.Filter = "Audio files (*.mp3, *.wav)|*.mp3;*.wav|All files (*.*)|*.*";

        if (openFileDialog.ShowDialog() == true)
        {
            string selectedFilePath = openFileDialog.FileName;

            Media.Source = new(selectedFilePath);

            Media.Play();
            _isPlaying = true;
        }
    }
}
