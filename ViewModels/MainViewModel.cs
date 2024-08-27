﻿using System;
using System.IO.Ports;
using System.Windows.Input;
using Modbus.Device;
using System.ComponentModel;
using System.Collections.ObjectModel;
using static RPStesting.ViewModels.MainViewModel;
using Newtonsoft.Json;
using RPStesting.Models;
using System.IO;

namespace RPStesting.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private IModbusSerialMaster _modbusMaster;
        private SerialPort _serialPort;
        private ushort _registerValue;
        private string _inputValue;
        private bool _isConnected;

        public ObservableCollection<string> LogMessages { get; private set; }         // ObservableCollection для логов
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ReadAllRegistersCommand { get; }
        public ICommand WriteRegisterCommand { get; }
        public ICommand ValidateParametersCommand { get; }

        private bool _isACConnected; // действительно ли это нужно

        public bool IsACConnected
        {
            get => _isACConnected;
            set
            {
                if (_isACConnected != value)
                {
                    _isACConnected = value;
                    OnPropertyChanged(nameof(IsACConnected));
                    WriteACRegister(value);
                }
            }
        }

        public string RegisterValue
        {
            get => _registerValue.ToString();
            private set
            {
                if (_registerValue.ToString() != value)
                {
                    _registerValue = ushort.Parse(value);
                    OnPropertyChanged(nameof(RegisterValue));
                }
            }
        }

        public string InputValue
        {
            get => _inputValue;
            set
            {
                if (_inputValue != value)
                {
                    _inputValue = value;
                    OnPropertyChanged(nameof(InputValue));
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        public enum StartAdress
        {
            AC = 1300,       // 1300 - Подключение AC (230V)
            LATR_ON,         // 1301 - Подключение ЛАТР
            ACB,             // 1302 - подключение акб
            ACB_POL,         // 1303 - Полярность АКБ
            TEMP_IMIT,       // 1304 - Имитатор термодатчика (-40, -35, -30)
            AC_OK,           // 1305 - Состояние реле 1 (AC_OK)
            RELAY,           // 1306 - Состояние реле 2 (Relay)
            KEY,             // 1307 - Ключ подключения нагрузки
            RESIST,          // 1308 - Установка сопротивления для контроля тока зарядки, Ом (от 3,3 до 267)
            VOLTAGE_ON_ACB,  // 1309 - Напряжение на АКБ, mV
            AMPERAGE_ON_ACB, // 1310 - Ток через АКБ, mA
            V230_ENTRANCE,   // 1311 - Присутствие напряжения 230V на входе RPS
            V230_EXIT,       // 1312 - Присутствие напряжения 230V на выходе RPS
            TEMP_ONE,        // 1313 - Температура датчика 1
            TEMP_TWO,        // 1314 - Температура датчика 2
            COOLER_KEY,      // 1315 - Ключ управления вентиляторами
            TEMP_OFF,        // 1316 - Температура выключения вентиляторов
            TEMP_ON,         // 1317 - Температура включения вентиляторов
                             // 1318 
                             // 1319
        }

        public enum StartAdressPlate
        {
            AC_PL = 1000,       // 1000 - Подключение AC (230V)
            LATR_ON_PL,         // 1001 - Подключение ЛАТР
            ACB_PL,             // 1002 - подключение акб
            ACB_POL_PL,         // 1003 - Полярность АКБ
            TEMP_IMIT,       // 1004 - Имитатор термодатчика (-40, -35, -30)
            AC_OK,           // 1005 - Состояние реле 1 (AC_OK)
            RELAY,           // 1006 - Состояние реле 2 (Relay)
            KEY,             // 1007 - Ключ подключения нагрузки
            RESIST,          // 1008 - Установка сопротивления для контроля тока зарядки, Ом (от 3,3 до 267)
            VOLTAGE_ON_ACB,  // 1009 - Напряжение на АКБ, mV
            AMPERAGE_ON_ACB, // 1010 - Ток через АКБ, mA
            V230_ENTRANCE,   // 1011 - Присутствие напряжения 230V на входе RPS
            V230_EXIT,       // 1012 - Присутствие напряжения 230V на выходе RPS
            TEMP_ONE,        // 1013 - Температура датчика 1
            TEMP_TWO,        // 1014 - Температура датчика 2
            COOLER_KEY,      // 1015 - Ключ управления вентиляторами
            TEMP_OFF,        // 1016 - Температура выключения вентиляторов
            TEMP_ON,         // 1017 - Температура включения вентиляторов
                             // 1018 
                             // 1019
        }

        public MainViewModel()
        {

            string jsonFilePath = "C:/Users/kotyo/Desktop/profiles/RPS-01.json";
            /*string jsonData = File.ReadAllText(jsonFilePath);
            TestConfigModel config = JsonConvert.DeserializeObject<TestConfigModel>(jsonData);*/
            ConnectCommand = new RelayCommand(Connect, param => !IsConnected);

            DisconnectCommand = new RelayCommand(Disconnect, param => IsConnected);
            ReadAllRegistersCommand = new RelayCommand(ReadAllRegisters, param => IsConnected);
            WriteRegisterCommand = new RelayCommand(WriteRegister, param => IsConnected);
            ValidateParametersCommand = new RelayCommand(ValidateParameters, param => IsConnected);

            LogMessages = new ObservableCollection<string>();  // Инициализация списка логов
            IsACConnected = false;
            IsConnected = false;
        }

        private void Log(string message)
        {
            LogMessages.Add($"{DateTime.Now}: {message}");
        }
        private void WriteACRegister(bool isConnected)
        {
            try
            {
                byte slaveID = 2;
                ushort startAddress = (ushort)StartAdress.AC; // 1300-й регистр

                ushort valueToWrite = isConnected ? (ushort)1 : (ushort)0; // 1 - Включено, 0 - Выключено

                _modbusMaster.WriteSingleRegister(slaveID, startAddress, valueToWrite);
                Log($"Register {startAddress} set to {valueToWrite}");
                ReadAllRegisters(null); // Обновление значений регистров
            }
            catch (Exception ex)
            {
                //Log($"Error writing to register {StartAdress}: {ex.Message}");
            }
        }
        private void Connect(object parameter)
        {
            _serialPort = new SerialPort("COM3")
            {
                BaudRate = 4800,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                ReadTimeout = 1000
            };

            try
            {
                _serialPort.Open();
                _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPort);
                _modbusMaster.Transport.Retries = 3;
                IsConnected = true;
                Log("Device connected successfully.");
            }
            catch (Exception ex)
            {
                Log($"Failed to connect: {ex.Message}");
            }
        }
        private void Disconnect(object parameter)
        {
            try
            {
                _modbusMaster?.Dispose();
                _serialPort?.Close();
                IsConnected = false;
                Log("Disconnected.");
            }
            catch (Exception ex)
            {
                Log($"Failed to disconnect: {ex.Message}");
            }
        }
        private void ReadAllRegisters(object parameter)
        {
            try
            {
                byte slaveIdFirst = 1; // плата
                byte slaveID = 2; // стенд 

                List<string> registerValues = new List<string>();
                List<string> registerValues_Plate = new List<string>();

                foreach (StartAdress address in Enum.GetValues(typeof(StartAdress)))
                {
                    ushort startAddress = (ushort)address;
                    ushort numOfPoints = 1;

                    // Чтение значения регистра
                    ushort[] holdingRegister = _modbusMaster.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

                    // Добавление значения в список
                    registerValues.Add($"{address}: {holdingRegister[0]}");
                    Log($"Read register {address} ({startAddress}): {holdingRegister[0]}");
                }

                foreach (StartAdressPlate address in Enum.GetValues(typeof(StartAdressPlate)))
                {
                    ushort startAddress = (ushort)address;
                    ushort numOfPoints = 1;

                    // Чтение значения регистра
                    ushort[] holdingRegister = _modbusMaster.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

                    // Добавление значения в список
                    registerValues_Plate.Add($"{address}: {holdingRegister[0]}");
                    Log($"Read register {address} ({startAddress}): {holdingRegister[0]}");
                }


                // Объединение всех значений в одну строку и вывод в RegisterValue
                RegisterValue = string.Join(Environment.NewLine, registerValues);
                Log("All registers read successfully.");
            }
            catch (Exception ex)
            {
                Log($"Read error: {ex.Message}");
            }
        }
        private void WriteRegister(object parameter)
        {
            try
            {
                byte slaveID = 2;
                ushort startAddress = 1317;

                if (ushort.TryParse(InputValue, out ushort newValue))
                {
                    _modbusMaster.WriteSingleRegister(slaveID, startAddress, newValue);
                    Log($"Written value {newValue} to register {startAddress}");
                    ReadAllRegisters(null); // Чтение значения после записи
                }
                else
                {
                    Log("Invalid input. Please enter a valid number.");
                }
            }
            catch (Exception ex)
            {
                Log($"Write error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// ВОЗНЯ
        public TestConfigModel Config { get; set; }
        private void LoadConfig()
        {
            string jsonFilePath = Config?.FirmwarePath ?? "C:/Users/kotyo/Desktop/profiles/RPS-01.json";
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(jsonFilePath);
                    Config = JsonConvert.DeserializeObject<TestConfigModel>(jsonData);

                    if (Config == null)
                    {
                        Config = new TestConfigModel(); // Создаем пустой объект, чтобы избежать NullReferenceException
                        Log("Configuration file is empty or could not be deserialized, using default configuration.");
                    }
                    else
                    {
                        Log($"Configuration loaded for model: {Config.ModelName}");
                    }
                }
                catch (Exception ex)
                {
                    Config = new TestConfigModel(); // Создаем пустой объект в случае ошибки десериализации
                    Log($"Failed to load configuration: {ex.Message}");
                }
            }
            else
            {
                Config = new TestConfigModel(); // Создаем пустой объект, если файл не найден
                Log("Configuration file not found. Using default configuration.");
            }
        }

        private void ValidateParameters(object parameter)
        {
            LoadConfig();
            if (Config == null)
            {
                Log("Configuration is not loaded.");
                return;
            }

            try
            {
                byte slaveID = 1; // плата

                ushort akbVoltage = ReadRegister(slaveID, 1013); // 1309 - Напряжение на АКБ
                if (Config.IsAkbDischargeVoltageEnabled && (akbVoltage < Config.AkbVoltageAcMin || akbVoltage > Config.AkbVoltageAcMax))
                {
                    Log($"AKB Voltage out of range: {akbVoltage} mV");
                }
                else
                {
                    Log($"AKB Voltage within range: {akbVoltage} mV");
                }

                // Другие проверки параметров...
            }
            catch (Exception ex)
            {
                Log($"Validation error: {ex.Message}");
            }
        }

        private ushort ReadRegister(byte slaveID, ushort registerAddress)
        {
            ushort[] result = _modbusMaster.ReadHoldingRegisters(slaveID, registerAddress, 1);
            return result[0];
        }

    }

    // Реализация команды RelayCommand
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}