using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPStesting
{
    public class MainModel
    {
        private SerialPort _serialPort;
        private IModbusSerialMaster _modbusMaster;


        public MainModel(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serialPort = new SerialPort(portName)
            {
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                ReadTimeout = 1000
            };
            _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPort);
        }

        public bool OpenConnection()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                return true;
            }
            catch (Exception ex)
            {
                // Логирование или дополнительная обработка ошибки может быть добавлена здесь
                return false;
            }
        }

        public void CloseConnection()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public bool WriteSingleRegister(byte slaveId, ushort address, ushort value)
        {
            try
            {
                _modbusMaster.WriteSingleRegister(slaveId, address, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public (bool Success, ushort Value) ReadHoldingRegister(byte slaveId, ushort address)
        {
            try
            {
                ushort[] result = _modbusMaster.ReadHoldingRegisters(slaveId, address, 1);
                return (true, result[0]);
            }
            catch (Exception)
            {
                return (false, 0);
            }
        }

        public void Dispose()
        {
            CloseConnection();
            _serialPort.Dispose();
        }
    }
}