using System;
using System.Globalization;
using MeteostraSimApk.Models;

namespace MeteostraSimApk.Services
{
    /// <summary>
    /// Zodpovida za formatovani dat do stringu a vypocet Checksumu (prosty soucet).
    /// </summary>
    public static class PacketBuilder
    {
        // Format: $ID_ZARIZENI;TEPLOTA;VLHKOST;TLAK;KVALITA_VZDUCHU;STATUS;CHECKSUM;@

        public static string BuildPacket(string deviceId, Measurement data, string status, bool forceBadChecksum = false, bool forceNullValues = false)
        {
            var culture = CultureInfo.InvariantCulture;
            string coreData;
            string checksumHex;

            if (forceNullValues)
            {
                // Simulace vypadku dat - Arduino posila -999 a status ER
                coreData = $"{deviceId};-999.00;-999.0;-999.0;-999;{status}";
                checksumHex = CalculateSumChecksum(-999.0, -999.0, -999.0, -999.0);
            }
            else
            {
                // Zaokrouhleni presne podle logiky v Arduinu
                double t = Math.Round(data.Temperature, 2);
                double h = Math.Round(data.Humidity, 1);
                double p = Math.Round(data.Pressure, 1);
                double aq = Math.Round(data.CO2, 0);

                // Sestaveni retezce
                coreData = $"{deviceId};{t.ToString("F2", culture)};{h.ToString("F1", culture)};{p.ToString("F1", culture)};{aq.ToString("F0", culture)};{status}";

                // Vypocet spravneho checksumu podle novych pravidel
                checksumHex = CalculateSumChecksum(t, h, p, aq);
            }

            // Chaos Engine: Umyslne poskozeni checksumu
            if (forceBadChecksum)
            {
                // Kdyz chceme chybu, proste tam posleme neplatny hex znak
                checksumHex = "FF";
            }

            return $"${coreData};{checksumHex};@\n";
        }

        /// <summary>
        /// Vypocita prosty soucet hodnot podle specifikace z Arduina.
        /// </summary>
        private static string CalculateSumChecksum(double t, double h, double p, double aq)
        {
            long sum = 0;

            sum += (long)(t * 100);    // 2 desetinna mista
            sum += (long)(h * 10);     // 1 desetinne misto
            sum += (long)(p * 10);     // 1 desetinne misto
            sum += (long)(aq);         // cele cislo

            // Vezmeme pouze nejnizsich 8 bitu
            byte cs = (byte)(sum & 0xFF);

            // Format na dvoumistny Hex string
            return cs.ToString("X2");
        }
    }
}