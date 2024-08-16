using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modbus.Utility;
using Modbus;

namespace RPStesting
{
    public class ModbusDevice
    {
        private IModbusSerialMaster _master;
        private SerialPort _serialPort;

        public ModbusDevice(string portName, int baudRate = 4800, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.Open();

        }

        public ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            return _master.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);
        }

        public void WriteSingleRegister(byte slaveId, ushort address, ushort value)
        {
            _master.WriteSingleRegister(slaveId, address, value);
        }

        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }
    }
}