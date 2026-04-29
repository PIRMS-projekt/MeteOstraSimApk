using System;
using System.IO;
using System.Text.Json;
using System.Globalization;
using MeteostraSimApk.Models;

namespace MeteostraSimApk.Services
{
    public class FileLogger
    {
        public enum LogFormat { CSV, TXT, JSON }

        public string FilePath { get; set; } = string.Empty;
        public LogFormat Format { get; set; }

        // Přidán parametr TimeSpan simTime
        public bool LogData(Measurement data, string rawPacket, TimeSpan simTime, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(FilePath)) return false;

            try
            {
                string line = "";
                var culture = CultureInfo.InvariantCulture; // Zaručí tečky místo čárek i v CSV
                string timeString = simTime.ToString(@"hh\:mm\:ss");

                switch (Format)
                {
                    case LogFormat.CSV:
                        // Format: Čas;Teplota;Vlhkost;Tlak;CO2 (Vše oříznuté, tečky místo čárek)
                        line = $"{timeString};{data.Temperature.ToString("F2", culture)};{data.Humidity.ToString("F1", culture)};{data.Pressure.ToString("F2", culture)};{data.CO2.ToString("F0", culture)}";
                        break;

                    case LogFormat.TXT:
                        // TXT zůstává beze změny, protože je složen v PacketBuilderu správně
                        line = rawPacket.TrimEnd();
                        break;

                    case LogFormat.JSON:
                        // Pro JSON vytvoříme na míru ušitý anonymní objekt s předem zaokrouhlenými čísly a časem
                        var jsonObject = new
                        {
                            Time = timeString,
                            Temperature = Math.Round(data.Temperature, 2),
                            Humidity = Math.Round(data.Humidity, 1),
                            Pressure = Math.Round(data.Pressure, 2),
                            CO2 = Math.Round(data.CO2, 0)
                        };
                        line = JsonSerializer.Serialize(jsonObject);
                        break;
                }

                File.AppendAllText(FilePath, line + Environment.NewLine);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}