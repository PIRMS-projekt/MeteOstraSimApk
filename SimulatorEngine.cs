using System;
using System.Collections.Generic;
using System.Text;
using MeteostraSimApk.Models;

namespace MeteostraSimApk.Engine
{
    public enum WeatherScenario
    {
        Manual,
        SummerDay,      // Bývalé Jasno
        AutumnStorm,    // Bývalá Bouřka
        Inversion,      // Smog
        FreezingNight   // Mrazivá noc
    }

    public class SimulatorEngine
    {
        private Random _rnd = new Random();

        // Minecraft styl: 0 až 24000 (0 = půlnoc, 6000 = 6:00, 12000 = poledne)
        public int TimeOfDayTicks { get; private set; } = 0;

        public Measurement CurrentData { get; private set; }
        public WeatherScenario CurrentScenario { get; set; } = WeatherScenario.Manual;

        public double NoiseChance { get; set; } = 0.0;
        public double OutlierChance { get; set; } = 0.0;
        public double NullChance { get; set; } = 0.0;
        public double BadChecksumChance { get; set; } = 0.0;

        public SimulatorEngine()
        {
            CurrentData = new Measurement(20.0, 50.0, 1013.0, 400.0);
        }

        public void Tick()
        {
            // Posun času (1 tick za běh cyklu). Po 24000 se resetuje na 0.
            TimeOfDayTicks = (TimeOfDayTicks + 1) % 24000;

            if (CurrentScenario != WeatherScenario.Manual)
            {
                ApplyScenarioLogic();
            }
        }

        private void ApplyScenarioLogic()
        {
            // Převod ticků (0-24000) na hodiny (0.0 - 24.0) pro snadnější matematiku
            double hour = (TimeOfDayTicks / 24000.0) * 24.0;

            switch (CurrentScenario)
            {
                case WeatherScenario.SummerDay:
                    // Letní den: Min teplota 15°C (ve 3:00), Max 34°C (v 15:00)
                    CurrentData.Temperature = 24.5 - 9.5 * Math.Cos((hour - 3) * Math.PI / 12);
                    CurrentData.Humidity = 50 + 20 * Math.Cos((hour - 3) * Math.PI / 12); // Vlhkost je inverzní k teplotě
                    CurrentData.Pressure = 1015;
                    CurrentData.CO2 = 400 + 20 * Math.Sin(hour); // Mírné běžné kolísání
                    break;

                case WeatherScenario.AutumnStorm:
                    // Podzimní bouřka: Běžný den, ale v 18:00 (Gaussova křivka) přijde prudký pokles tlaku a teploty
                    double stormEffect = Math.Exp(-Math.Pow(hour - 18, 2) / 4); // Zvonová křivka s vrcholem v 18:00

                    CurrentData.Temperature = (12.5 - 2.5 * Math.Cos((hour - 3) * Math.PI / 12)) - (5 * stormEffect);
                    CurrentData.Humidity = 65 + (30 * stormEffect); // Vlhkost vyletí až na 95 %
                    CurrentData.Pressure = 1010 - (25 * stormEffect); // Tlak spadne na 985 hPa
                    CurrentData.CO2 = 410;
                    break;

                case WeatherScenario.Inversion:
                    // Smog/Inverze: Chladno, stabilní vysoký tlak, CO2 roste večer, když lidé topí a vzduch stojí
                    CurrentData.Temperature = 2 - 4 * Math.Cos((hour - 4) * Math.PI / 12);
                    CurrentData.Pressure = 1030 + (_rnd.NextDouble() - 0.5); // Vysoký tlak se šumem
                    CurrentData.Humidity = 80;
                    // CO2 graduje kolem 20:00 (až na 1200 PPM)
                    CurrentData.CO2 = 400 + 800 * Math.Exp(-Math.Pow(hour - 20, 2) / 16);
                    break;

                case WeatherScenario.FreezingNight:
                    // Mrazivá noc: Maximum přes den je -5°C, v noci padá až na -20°C
                    CurrentData.Temperature = -12.5 - 7.5 * Math.Cos((hour - 3) * Math.PI / 12);
                    CurrentData.Humidity = 85;
                    CurrentData.Pressure = 1018;
                    CurrentData.CO2 = 400;
                    break;
            }

            ClampValues();
        }

        private void ClampValues()
        {
            if (CurrentData.Temperature > 85) CurrentData.Temperature = 85;
            if (CurrentData.Temperature < -40) CurrentData.Temperature = -40;
            if (CurrentData.Humidity > 100) CurrentData.Humidity = 100;
            if (CurrentData.Humidity < 0) CurrentData.Humidity = 0;
            if (CurrentData.Pressure > 1100) CurrentData.Pressure = 1100;
            if (CurrentData.Pressure < 30) CurrentData.Pressure = 30;
            if (CurrentData.CO2 > 5000) CurrentData.CO2 = 5000;
            if (CurrentData.CO2 < 400) CurrentData.CO2 = 400;
        }

        public Measurement GetProcessedData(out bool isNull, out bool isBadChecksum)
        {
            Measurement finalData = CurrentData.Clone();
            isNull = false;
            isBadChecksum = false;

            if (_rnd.NextDouble() * 100 < NullChance)
            {
                isNull = true;
                return finalData;
            }

            if (_rnd.NextDouble() * 100 < BadChecksumChance)
            {
                isBadChecksum = true;
            }

            if (_rnd.NextDouble() * 100 < OutlierChance)
            {
                finalData.Temperature = 500.0;
                finalData.Humidity = 200.0;
                finalData.Pressure = 0.0;
                finalData.CO2 = 9999.0;
                return finalData;
            }

            if (_rnd.NextDouble() * 100 < NoiseChance)
            {
                finalData.Temperature += (_rnd.NextDouble() - 0.5) * 5.0;
                finalData.Humidity += (_rnd.NextDouble() - 0.5) * 10.0;
                finalData.Pressure += (_rnd.NextDouble() - 0.5) * 4.0;
                finalData.CO2 += (_rnd.NextDouble() - 0.5) * 100.0;
            }

            return finalData;
        }
    }
}