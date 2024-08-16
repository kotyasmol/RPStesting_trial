using System;
using System.Collections.Generic;
using System.Linq;
using Stylet;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Modbus.Device;
using System.Security.Cryptography;
using HandyControl.Controls;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using HandyControl.Tools.Command;
using System.Windows.Input;

namespace RPStesting
{
        public class MainViewModel : INotifyPropertyChanged
        {
            private MainModel _modbusModel;
            private bool _isConnected;
            private ushort _registerValue;
            private string _statusMessage;
            private ICommand _toggleRegisterCommand;
            private ICommand _readRegisterCommand;
            private ICommand _connectCommand;

            public MainViewModel()
            {
                // Initialize ModbusModel with proper settings
                _modbusModel = new MainModel("COM3", 4800, Parity.Even, 8, StopBits.One);
                _connectCommand = new RelayCommand(param => OpenConnection(), param => !_isConnected);
                _toggleRegisterCommand = new RelayCommand(param => ToggleRegister(), param => _isConnected);
                _readRegisterCommand = new RelayCommand(param => ReadRegister(), param => _isConnected);
            }

            public ushort RegisterValue
            {
                get => _registerValue;
                set
                {
                    _registerValue = value;
                    OnPropertyChanged(nameof(RegisterValue));
                }
            }

            public string StatusMessage
            {
                get => _statusMessage;
                set
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }

            public ICommand ToggleRegisterCommand => _toggleRegisterCommand;
            public ICommand ReadRegisterCommand => _readRegisterCommand;
            public ICommand ConnectCommand => _connectCommand;

            private void OpenConnection()
            {
                if (_isConnected)
                {
                    StatusMessage = "Connection is already open.";
                    return;
                }

                try
                {
                    if (_modbusModel.OpenConnection())
                    {
                        _isConnected = true;
                        StatusMessage = "Connection opened successfully.";
                    }
                    else
                    {
                        StatusMessage = "Failed to open connection.";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to open connection: {ex.Message}";
                    _modbusModel.CloseConnection();
                }
            }

            private void ToggleRegister()
            {
                if (_modbusModel == null) return;

                // Toggle between 0 and 1
                ushort newValue = RegisterValue == 0 ? (ushort)1 : (ushort)0;
                bool writeSuccess = _modbusModel.WriteSingleRegister(2, 1300, newValue);

                if (writeSuccess)
                {
                    RegisterValue = newValue;
                    StatusMessage = "Toggle successful.";
                }
                else
                {
                    StatusMessage = "Toggle failed.";
                }
            }

            public void ReadRegister()
            {
                if (_modbusModel == null) return;

                var (success, value) = _modbusModel.ReadHoldingRegister(2, 1300);
                if (success)
                {
                    RegisterValue = value;
                    StatusMessage = $"Read successful: {value:X}";
                }
                else
                {
                    StatusMessage = "Read failed.";
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            ~MainViewModel()
            {
                _modbusModel?.Dispose();
            }
        }
    }