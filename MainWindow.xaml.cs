using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MeteostraSimApk.Engine;
using MeteostraSimApk.Models;
using MeteostraSimApk.Services;

namespace MeteostraSimApk
{
    public partial class MainWindow : Window
    {
        private SimulatorEngine _engine;
        private SerialService _serialService;
        private FileLogger _fileLogger;
        private DispatcherTimer _timer;

        private bool _isRunning = false;
        private DateTime _startTime;
        private int _errorCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            _engine = new SimulatorEngine();
            _serialService = new SerialService();
            _fileLogger = new FileLogger();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += Timer_Tick;
        }

        private void OutputMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (GridComSettings != null)
                GridComSettings.IsEnabled = ChkUseComPort.IsChecked == true;

            if (PanelFileSettings != null)
                PanelFileSettings.IsEnabled = ChkUseFile.IsChecked == true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _engine.Tick();
            Measurement data = _engine.GetProcessedData(out bool isNull, out bool isBadChecksum);

            int status = isNull ? 0 : 1;
            // Přebíráme jméno zařízení dynamicky z UI
            string deviceId = TxtDeviceId.Text.Trim();
            string packet = PacketBuilder.BuildPacket(deviceId, data, status, isBadChecksum, isNull);

            if (isNull || isBadChecksum || data.Temperature == 500.0)
            {
                _errorCount++;
                LblErrorStats.Text = $"Vygenerováno chyb: {_errorCount}";
            }

            if (ChkUseComPort.IsChecked == true && _serialService.IsOpen)
            {
                _serialService.SendData(packet, out string serialErr);
                if (!string.IsNullOrEmpty(serialErr))
                {
                    StopSimulation();
                    MessageBox.Show($"Chyba sériového portu za běhu:\n{serialErr}", "Kritická chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (ChkUseFile.IsChecked == true && !string.IsNullOrEmpty(_fileLogger.FilePath))
            {
                double hoursPassed = (_engine.TimeOfDayTicks / 24000.0) * 24.0;
                TimeSpan simulatedTime = TimeSpan.FromHours(hoursPassed);

                _fileLogger.LogData(data, packet, simulatedTime, out string fileErr);
            }

            TimeSpan runningTime = DateTime.Now - _startTime;
            LblTime.Text = $"Čas běhu: {runningTime:hh\\:mm\\:ss}";

            TxtTrafficLog.AppendText(packet);
            TxtTrafficLog.ScrollToEnd();

            if (_engine.CurrentScenario != WeatherScenario.Manual)
            {
                UpdateManualSliders(data);
            }
        }

        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning) StartSimulation();
            else StopSimulation();
        }

        private void StartSimulation()
        {
            // Ošetření povinného jména zařízení
            if (string.IsNullOrWhiteSpace(TxtDeviceId.Text))
            {
                MessageBox.Show("Zadejte platné ID zařízení.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool useCom = ChkUseComPort.IsChecked == true;
            bool useFile = ChkUseFile.IsChecked == true;

            if (!useCom && !useFile)
            {
                MessageBox.Show("Vyberte alespoň jednu metodu výstupu.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (useCom)
            {
                if (string.IsNullOrWhiteSpace(TxtComPort.Text))
                {
                    MessageBox.Show("Zadejte název COM portu (např. COM1).", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int baudRate = 9600;
                if (CmbBaudRate.SelectedItem is ComboBoxItem selectedItem && int.TryParse(selectedItem.Content.ToString(), out int parsedBaud))
                {
                    baudRate = parsedBaud;
                }

                if (!_serialService.OpenPort(TxtComPort.Text, baudRate, out string error))
                {
                    MessageBox.Show($"Nepodařilo se otevřít port {TxtComPort.Text}.\nDetail: {error}", "Chyba připojení", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (useFile)
            {
                if (string.IsNullOrWhiteSpace(_fileLogger.FilePath) || _fileLogger.FilePath == "Vyberte cestu k souboru...")
                {
                    MessageBox.Show("Vyberte prosím cestu k záložnímu souboru pomocí tlačítka [...].", "Chybí soubor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (useCom) _serialService.ClosePort();
                    return;
                }

                if (RbCsv.IsChecked == true) _fileLogger.Format = FileLogger.LogFormat.CSV;
                else if (RbJson.IsChecked == true) _fileLogger.Format = FileLogger.LogFormat.JSON;
                else _fileLogger.Format = FileLogger.LogFormat.TXT;
            }

            // Zamčení políčka
            TxtDeviceId.IsEnabled = false;
            ChkUseComPort.IsEnabled = false;
            ChkUseFile.IsEnabled = false;
            GridComSettings.IsEnabled = false;
            PanelFileSettings.IsEnabled = false;

            _startTime = DateTime.Now;
            _timer.Start();
            _isRunning = true;

            BtnStartStop.Content = "ZASTAVIT SIMULACI";
            BtnStartStop.Background = System.Windows.Media.Brushes.LightCoral;
        }

        private void StopSimulation()
        {
            _timer.Stop();
            _serialService.ClosePort();
            _isRunning = false;

            BtnStartStop.Content = "SPUSTIT SIMULACI";
            BtnStartStop.Background = System.Windows.Media.Brushes.LightGreen;

            // Odemčení
            TxtDeviceId.IsEnabled = true;
            ChkUseComPort.IsEnabled = true;
            ChkUseFile.IsEnabled = true;
            OutputMethod_Changed(null, null);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            StopSimulation();
            _engine = new SimulatorEngine();
            _errorCount = 0;
            LblErrorStats.Text = "Vygenerováno chyb: 0";
            LblTime.Text = "Čas běhu: 00:00:00";
            TxtTrafficLog.Clear();
            UpdateManualSliders(_engine.CurrentData);

            SldNoise.Value = 0;
            SldOutlier.Value = 0;
            SldNull.Value = 0;
            SldChecksum.Value = 0;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "CSV soubory (*.csv)|*.csv|JSON soubory (*.json)|*.json|Textové soubory (*.txt)|*.txt";

            if (dialog.ShowDialog() == true)
            {
                _fileLogger.FilePath = dialog.FileName;
                TxtFilePath.Text = dialog.FileName;

                if (dialog.FilterIndex == 1) RbCsv.IsChecked = true;
                else if (dialog.FilterIndex == 2) RbJson.IsChecked = true;
                else if (dialog.FilterIndex == 3) RbTxt.IsChecked = true;
            }
        }

        private void SldSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(SldSpeed.Value);
            }
        }

        private void Scenario_Changed(object sender, RoutedEventArgs e)
        {
            if (_engine == null) return;

            if (RbScenManual.IsChecked == true) _engine.CurrentScenario = WeatherScenario.Manual;
            else if (RbScenSummer.IsChecked == true) _engine.CurrentScenario = WeatherScenario.SummerDay;
            else if (RbScenStorm.IsChecked == true) _engine.CurrentScenario = WeatherScenario.AutumnStorm;
            else if (RbScenInversion.IsChecked == true) _engine.CurrentScenario = WeatherScenario.Inversion;
            else if (RbScenFreeze.IsChecked == true) _engine.CurrentScenario = WeatherScenario.FreezingNight;

            if (GrpManual != null) GrpManual.IsEnabled = _engine.CurrentScenario == WeatherScenario.Manual;
        }

        private void ManualSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null && _engine.CurrentScenario == WeatherScenario.Manual)
            {
                _engine.CurrentData.Temperature = SldTemp.Value;
                _engine.CurrentData.Humidity = SldHum.Value;
                _engine.CurrentData.Pressure = SldPress.Value;
                _engine.CurrentData.CO2 = SldCo2.Value;
            }
        }

        private void ChaosSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null)
            {
                _engine.NoiseChance = SldNoise.Value;
                _engine.OutlierChance = SldOutlier.Value;
                _engine.NullChance = SldNull.Value;
                _engine.BadChecksumChance = SldChecksum.Value;
            }
        }

        private void UpdateManualSliders(Measurement data)
        {
            SldTemp.ValueChanged -= ManualSlider_ValueChanged;
            SldHum.ValueChanged -= ManualSlider_ValueChanged;
            SldPress.ValueChanged -= ManualSlider_ValueChanged;
            SldCo2.ValueChanged -= ManualSlider_ValueChanged;

            SldTemp.Value = data.Temperature;
            SldHum.Value = data.Humidity;
            SldPress.Value = data.Pressure;
            SldCo2.Value = data.CO2;

            SldTemp.ValueChanged += ManualSlider_ValueChanged;
            SldHum.ValueChanged += ManualSlider_ValueChanged;
            SldPress.ValueChanged += ManualSlider_ValueChanged;
            SldCo2.ValueChanged += ManualSlider_ValueChanged;
        }
    }
}