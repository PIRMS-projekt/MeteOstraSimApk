using System.Collections.Generic;
using System.Text;
using System;

namespace MeteostraSimApk.Models
{
    /// <summary>
    /// Reprezentuje jeden okamžitý stav naměřených veličin.
    /// </summary>
    public class Measurement
    {
        public double Temperature { get; set; } // °C (-40 az 85)
        public double Humidity { get; set; }    // % (0 az 100)
        public double Pressure { get; set; }    // hPa (300 az 1100)
        public double CO2 { get; set; }         // PPM (400 az 5000)

        // Konstruktor pro nastavení výchozích hodnot
        public Measurement(double temp, double hum, double press, double co2)
        {
            Temperature = temp;
            Humidity = hum;
            Pressure = press;
            CO2 = co2;
        }

        // Klonování pro bezpečné předávání dat bez referenčních chyb
        public Measurement Clone()
        {
            return new Measurement(Temperature, Humidity, Pressure, CO2);
        }
    }
}