using System;
using System.IO.Ports;

namespace MeteostraSimApk.Services
{
    /// <summary>
    /// Zajistuje bezpecnou komunikaci pres virtualni COM port (VSPE).
    /// </summary>
    public class SerialService
    {
        private SerialPort _serialPort;

        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;

        public SerialService()
        {
            _serialPort = new SerialPort();
        }

        public bool OpenPort(string portName, int baudRate, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;
                _serialPort.DataBits = 8;
                _serialPort.Parity = Parity.None;
                _serialPort.StopBits = StopBits.One;

                _serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public void ClosePort()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public bool SendData(string data, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                if (IsOpen)
                {
                    _serialPort.Write(data);
                    return true;
                }
                errorMessage = "Port neni otevren.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}