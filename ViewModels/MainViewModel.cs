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

        public ObservableCollection<string> LogMessages { get; private set; }    // ObservableCollection для логов
        public event PropertyChangedEventHandler PropertyChanged; // Для обновления GUI

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ReadAllRegistersCommand { get; }
        public ICommand WriteRegisterCommand { get; }
        public ICommand RunSelfTestCommand { get; }


        private bool _isACConnected;

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

        public enum StartAdress // тоже надо добавить остальные регистры, но пока не трогаю потому что страшно
        {
            AC = 1300,       // 1300 - Подключение AC (230V)
            LATR_ON,         // 1301 - Подключение ЛАТР
            ACB,             // 1302 - подключение акб // ИМИТАТОР АКБ!!!
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
                             // 1318 - Установка max температуры на радиаторе. При превышении температуры отключаем ключ цепи нагрузки. Мин = 60, Макс = 150
                             // 1319 - Очистка статистики
        }

        public enum StartAdressPlate // надо разобраться тут со всякими с 1016 - далее 
        {
            AC_PL = 1000,       // 1000 - Тип устройства
            LATR_ON_PL,         // 1001 - Аппаратная версия платы
            ACB_PL,             // 1002 - Версия прошивки
            ACB_POL_PL,         // 1003 - Тип питания (0-АКБ / 1 - VAC)
            TEMP_IMIT,       // 1004 - Напряжение на АКБ в mV
            AC_OK,           // 1005 - Напряжение зарядки АКБ в mV
            RELAY,           // 1006 - Ток через АКБ в mA
            KEY,             // 1007 - Температура на плате в градусах
            RESIST,          // 1008 - Состояние светодиода BAT
            VOLTAGE_ON_ACB,  // 1009 - Ключ подключения АКБ
            AMPERAGE_ON_ACB, // 1010 - Ключ включения зарядки
            V230_ENTRANCE,   // 1011 - Оптореле
            V230_EXIT,       // 1012 - Оптронон AC_OK - не используется 
            TEMP_ONE,        // 1013 - Напряжение полного отключения
            TEMP_TWO,        // 1014 - Низкое напряжение АКБ
            COOLER_KEY,      // 1015 - Прогноз времени работы от АКБ
            TEMP_OFF,        // 1016 - Флаг прохождения тестирования
            TEMP_ON,         // 1017 - Идентификатор платы
                             // 1018 - Флаг исправности LTC4151
                             // 1019 - Напряжение АКБ (АЦП)
                             // 1020 - Ток через АКБ (АЦП)
                             // 1021 - тестовый режим
        }

        public MainViewModel()
        {

            string jsonFilePath = "C:/Users/kotyo/Downloads/Telegram Desktop/RPS-01 v11.json";

            ConnectCommand = new RelayCommand(Connect, param => !IsConnected);
            DisconnectCommand = new RelayCommand(Disconnect, param => IsConnected);
            ReadAllRegistersCommand = new RelayCommand(ReadAllRegisters, param => IsConnected);
            RunSelfTestCommand = new RelayCommand(RunSelfTestCommandExecute, param => IsConnected);


            LogMessages = new ObservableCollection<string>();  // Инициализация списка логов
            IsACConnected = false; // хз пока зачем
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
                ushort startAddress = (ushort)StartAdress.AC; // 1300-й регистр - подача питания на плату, без него  плата = пустышка нечитаемая

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
            _serialPort = new SerialPort("COM3") // в будущем добавить выбор порта 
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
        private ushort ReadRegister(byte slaveID, ushort registerAddress)
        {
            ushort[] result = _modbusMaster.ReadHoldingRegisters(slaveID, registerAddress, 1);
            return result[0];
        }
        private void ReadAllRegisters(object parameter)
        {
            try
            {
                byte slaveIdFirst = 1; // плата
                byte slaveID = 2; // стенд 

                List<string> registerValues = new List<string>();
                List<string> registerValues_Plate = new List<string>();

                foreach (StartAdress address in Enum.GetValues(typeof(StartAdress))) // чтение всего со стенда
                {
                    ushort startAddress = (ushort)address;
                    ushort numOfPoints = 1;

                    // Чтение значения регистра
                    ushort[] holdingRegister = _modbusMaster.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

                    // Добавление значения в список
                    registerValues.Add($"{address}: {holdingRegister[0]}");
                    Log($"Read register {address} ({startAddress}): {holdingRegister[0]}");
                }

                foreach (StartAdressPlate address in Enum.GetValues(typeof(StartAdressPlate))) // чтение всего с платы
                {
                    ushort startAddress = (ushort)address;
                    ushort numOfPoints = 1;

                    // Чтение значения регистра
                    ushort[] holdingRegister = _modbusMaster.ReadHoldingRegisters(slaveIdFirst, startAddress, numOfPoints);

                    // Добавление значения в список
                    registerValues_Plate.Add($"{address}: {holdingRegister[0]}");
                    Log($"Read register {address} ({startAddress}): {holdingRegister[0]}");
                }


                Log("All registers read successfully."); 
            }
            catch (Exception ex)
            {
                Log($"Read error: {ex.Message}"); 
            }
        }
      /*  private void WriteRegister(object parameter) 
        {
            try
            {
                byte slaveID = 2;
                ushort startAddress = 1317;

                if (ushort.TryParse(InputValue, out ushort newValue))
                {
                    _modbusMaster.WriteSingleRegister(slaveID, startAddress, newValue);
                    Log($"Written value {newValue} to register {startAddress}");
                    ReadAllRegisters(null); // Чтение значения после записи, мб заменить на чтение одного регистра а не всех. (с логом что х - текущее значение регистра)
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
        }*/

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// ВОЗНЯ
        public TestConfigModel Config { get; set; }
        private void LoadConfig()
        {
            string jsonFilePath = Config?.FirmwarePath ?? "C:/Users/kotyo/Downloads/Telegram Desktop/RPS-01 v11.json";
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

        #region автоматическое тестирование всех параметров платы


        public bool CheckRps01MinMaxParam(StartAdressPlate mbAddr, int maxValue, int minValue, int timeout) // новенькое!! чисто для платочки!!!!!!! люблименькой)
        {
            int readCnt = 0;
            ushort value;

            while (true) // сюда надо засунуть колво чтений 
            {
                value = ReadRegister(1, (ushort)mbAddr); // 1 - Slave ID устройства

                // Отладка - вывод текущего значения
                Log($"Считывание значения для {mbAddr}: {value}, ожидаемый диапазон: {minValue} - {maxValue}");

                if (value <= maxValue && value >= minValue)
                {
                    return true;
                }

                if (readCnt >= timeout)
                {
                    return false;
                }
                System.Threading.Thread.Sleep(Config.RpsReadDelay); // Пауза 1 секунда
                readCnt++;
            }
        }

        // самотестирование поехали
        private void RunSelfTestCommandExecute(object parameter)
        {
            LoadConfig(); // Загружаем конфигурацию перед выполнением теста
            if (Config != null)
            {
                try
                {
                    RunSelfTest(1, Config); // 1 - ID платы
                }
                catch (Exception ex)
                {
                    Log($"Ошибка самотестирования: {ex.Message}");
                }
            }
            else
            {
                Log("Конфигурация не загружена. Самотестирование невозможно.");
            }
        }
        public void RunSelfTest(byte slaveID, TestConfigModel config)
        {
            if (config.IsBuildinTestEnabled)
            {
                Log("Самотестирование RPS-01 запущено.");

                // 1. Проверка температуры на плате
                if (config.IsTemperMinMaxEnabled)
                {
                    ushort temperature = ReadRegister(slaveID, (ushort)StartAdressPlate.KEY);
                    Log($"Считывание температуры: {temperature}°C.");

                    if (!CheckRps01MinMaxParam(StartAdressPlate.KEY, config.TemperMax, config.TemperMin, config.RpsReadDelay))
                    {
                        Log($"Ошибка: Температура на плате ({temperature}°C) выходит за пределы допустимых значений ({config.TemperMin}-{config.TemperMax}°C).");
                        throw new Exception("Температура вне допустимого диапазона.");
                    }
                    else
                    {
                        Log("Температура на плате в пределах нормы.");
                    }
                }   

                // 2. Проверка состояния реле RELAY
                if (config.IsRelay1TestEnabled)
                {
                    ushort relay1State = ReadRegister(slaveID, (ushort)StartAdressPlate.RELAY);
                    //                    ushort relay1State = ReadRegister(slaveID, (ushort)StartAdressPlate.RELAY); --- БЫЛО
                    Log($"Состояние реле RELAY: {relay1State}.");

                    if (relay1State != config.Relay1Test)
                    {
                        Log($"Ошибка: Состояние реле RELAY ({relay1State}) не соответствует ожидаемому ({config.Relay1Test}).");
                        throw new Exception("Неправильное состояние реле RELAY.");
                    }
                    else
                    {
                        Log("Состояние реле RELAY в пределах нормы.");
                    }
                }

                // 3. Проверка состояния реле AC_OK (работа от акб или че)-------------------------------------------------------запомнить
                if (config.IsRelay2TestEnabled)
                {
                    ushort relay2State = ReadRegister(slaveID, (ushort)StartAdressPlate.AC_OK);
                    Log($"Состояние реле AC_OK: {relay2State}.");

                    if (relay2State != config.Relay2Test)
                    {
                        Log($"Ошибка: Состояние реле AC_OK ({relay2State}) не соответствует ожидаемому ({config.Relay2Test}).");
                        throw new Exception("Неправильное состояние реле AC_OK.");
                    }
                    else
                    {
                        Log("Состояние реле AC_OK в пределах нормы.");
                    }
                }

                // 4. Проверка версии прошивки
                if (config.FirmwareVersion != 0)
                {
                    ushort firmwareVersion = ReadRegister(slaveID, (ushort)StartAdressPlate.ACB_PL);
                    Log($"Версия прошивки: {firmwareVersion}.");

                    if (firmwareVersion != config.FirmwareVersion)
                    {
                        Log($"Ошибка: Версия прошивки ({firmwareVersion}) не соответствует ожидаемой ({config.FirmwareVersion}).");
                        throw new Exception("Неправильная версия прошивки.");
                    }
                    else
                    {
                        Log("Версия прошивки соответствует ожидаемой.");
                    }
                }

                Log("Самотестирование RPS-01 завершено успешно.");
            }
            else
            {
                Log("Самотестирование отключено.");
            }
        }



        /* private void ValidateParameters(object parameter)
         {
             LoadConfig();
             if (Config == null)
             {
                 Log("Конфигурация не загружена.");
                 return;
             }

             try
             {
                 byte slaveID = 1; // плата
                 ushort akbVoltage = ReadRegister(slaveID, 1004); // напряжение на АКБ в мВ
                 if (Config.IsAkbDischargeVoltageEnabled && (akbVoltage < Config.AkbVoltageAcMin || akbVoltage > Config.AkbVoltageAcMax))
                 {
                     Log($"Напряжение АКБ вне допустимого диапазона: {akbVoltage} мВ");

                 }
                 else
                 {
                     Log($"Напряжение АКБ - ОК: {akbVoltage} мВ");
                 }



                 ushort loadXXCurrent = ReadRegister(2, 1310); // Ток нагрузки ХХ в мА
                 if (loadXXCurrent < Config.LoadXXCurrentMin || loadXXCurrent > Config.LoadXXCurrentMax)
                 {
                     Log($"Ток нагрузки ХХ вне допустимого диапазона: {loadXXCurrent} мА");

                 }
                 else
                 {
                     Log($"Ток нагрузки ХХ - ОК: {loadXXCurrent} мА");
                 }

                 ushort temperature = ReadRegister(slaveID, 1007); // Температура на плате в градусах
                 if (temperature < Config.TemperMin || temperature > Config.TemperMax)
                 {
                     Log($"Температура на плате вне допустимого диапазона: {temperature} °C");
                 }
                 else
                 {
                     Log($"Температура на плате - ОК: {temperature} °C");
                 }

                 // Другие проверки параметров
             }
             catch (Exception ex)
             {
                 Log($"Ошибка проверки параметров: {ex.Message}");
             }
         }
                */
        #endregion


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