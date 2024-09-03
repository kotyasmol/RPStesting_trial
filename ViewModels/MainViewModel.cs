using System;
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

        public enum StartAddress // значения стенда 
        {
            ACConnection = 1300,          // 1300 - Подключение AC (230V)
            LatrConnection,               // 1301 - Подключение ЛАТР
            ACBConnection,                // 1302 - Подключение АКБ (ИМИТАТОР АКБ)
            ACBPolarity,                  // 1303 - Полярность АКБ
            TemperatureSimulator,         // 1304 - Имитатор термодатчика (-40, -35, -30)
            AC_OKRelayState,              // 1305 - Состояние реле 1 (AC_OK)
            RelayState,                   // 1306 - Состояние реле 2 (Relay)
            LoadSwitchKey,                // 1307 - Ключ подключения нагрузки
            ResistanceSetting,            // 1308 - Установка сопротивления для контроля тока зарядки, Ом (от 3,3 до 267)
            ACBVoltage,                   // 1309 - Напряжение на АКБ, mV
            ACBAmperage,                  // 1310 - Ток через АКБ, mA
            V230PresenceAtEntrance,       // 1311 - Присутствие напряжения 230V на входе RPS
            V230PresenceAtExit,           // 1312 - Присутствие напряжения 230V на выходе RPS
            Sensor1Temperature,           // 1313 - Температура датчика 1
            Sensor2Temperature,           // 1314 - Температура датчика 2
            CoolerControlKey,             // 1315 - Ключ управления вентиляторами
            FanOffTemperature,            // 1316 - Температура выключения вентиляторов
            FanOnTemperature,             // 1317 - Температура включения вентиляторов
            MaxRadiatorTemperature,       // 1318 - Установка максимальной температуры на радиаторе
            StatisticsReset               // 1319 - Очистка статистики
        }

        public enum StartAddressPlate
        {
            DeviceType = 1000,              // 1000 - Тип устройства
            HardwareVersion,                // 1001 - Аппаратная версия платы
            FirmwareVersion,                // 1002 - Версия прошивки
            PowerType,                      // 1003 - Тип питания (0 - АКБ / 1 - VAC)
            ACBVoltage,                     // 1004 - Напряжение на АКБ в mV
            ChargingVoltage,                // 1005 - Напряжение зарядки АКБ в mV
            ACBCurrent,                     // 1006 - Ток через АКБ в mA
            BoardTemperature,               // 1007 - Температура на плате в градусах
            BATLedStatus,                   // 1008 - Состояние светодиода BAT
            ACBConnectionSwitch,            // 1009 - Ключ подключения АКБ
            ChargingSwitch,                 // 1010 - Ключ включения зарядки
            OptoRelay,                      // 1011 - Оптореле
            Unused_AC_OKOptocoupler,        // 1012 - Оптрон AC_OK (не используется)
            FullDischargeVoltage,           // 1013 - Напряжение полного отключения
            ACBLowVoltage,                  // 1014 - Низкое напряжение АКБ
            BatteryRunTimeEstimate,         // 1015 - Прогноз времени работы от АКБ
            TestPassFlag,                   // 1016 - Флаг прохождения тестирования
            BoardIdentifier,                // 1017 - Идентификатор платы
            LTC4151HealthFlag,              // 1018 - Флаг исправности LTC4151
            ACBVoltageADC,                  // 1019 - Напряжение АКБ (АЦП)
            ACBCurrentADC,                  // 1020 - Ток через АКБ (АЦП)
            TestMode                       // 1021 - Тестовый режим
        }


        public MainViewModel()
        {

            string jsonFilePath = "C:/Users/kotyo/Downloads/Telegram Desktop/RPS-01 v11.json";

            ConnectCommand = new RelayCommand(Connect, param => !IsConnected);
            DisconnectCommand = new RelayCommand(Disconnect, param => IsConnected);
            ReadAllRegistersCommand = new RelayCommand(ReadAllRegisters, param => IsConnected);
            RunSelfTestCommand = new RelayCommand(RunSelfTestCommandExecute, param => IsConnected);

            LogMessages = new ObservableCollection<string>();  // Инициализация списка логов
            IsACConnected = false; // Content="Подключение AC (230V)"  худо бедно работает 
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
                ushort startAddress = (ushort)StartAddress.ACConnection; // 1300-й регистр - подача питания на плату, без него  плата = пустышка нечитаемая

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
        private void WriteModbus(byte slaveID, ushort registerAddress, int value) // запись в один регистр, универсальная получается 
        {
            try
            {
                _modbusMaster.WriteSingleRegister(slaveID, registerAddress, (ushort)value);
                Log($"Значение {value} успешно записано в регистр {registerAddress} для устройства с ID {slaveID}.");
            }
            catch (Exception ex)
            {
                Log($"Ошибка при записи значения {value} в регистр {registerAddress} для устройства с ID {slaveID}: {ex.Message}");
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

                foreach (StartAddress address in Enum.GetValues(typeof(StartAddress))) // чтение всего со стенда
                {
                    ushort startAddress = (ushort)address;
                    ushort numOfPoints = 1;

                    // Чтение значения регистра
                    ushort[] holdingRegister = _modbusMaster.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

                    // Добавление значения в список
                    registerValues.Add($"{address}: {holdingRegister[0]}");
                    Log($"Read register {address} ({startAddress}): {holdingRegister[0]}");
                }

                foreach (StartAddressPlate address in Enum.GetValues(typeof(StartAddressPlate))) // чтение всего с платы
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

        public bool CheckRps01MinMaxParam(StartAddressPlate mbAddr, int maxValue, int minValue, int timeout) // новенькое!! чисто для платочки!!!!!!! люблименькой)
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

        // проверка preheating
        public void SetRpsPreheating(int value)
        {
            Log($"Установка эквивалента температуры: {value}");
            byte slaveID = 2;
            ushort registerAddress = 1304;
            WriteModbus(slaveID, registerAddress, value);
        }

        /*
        emit syslog("Проверка Preheating",C);
        WAIT_OK("Установите джампер «PREHEATING» в положение «YES»");
        WAIT_OK("Установите напряжение на ЛАТР 230В");
        emit syslog("Старт при -30",C);
        emit set_rps_preheating(-30);
         */

        public void RunSelfTest(byte slaveID, TestConfigModel config)
        {
            if (config.IsBuildinTestEnabled)
            {
                Log("Самотестирование RPS-01 запущено.");

                // 1. Проверка температуры на плате
                if (config.IsTemperMinMaxEnabled)
                {
                    ushort temperature = ReadRegister(slaveID, (ushort)StartAddressPlate.BoardTemperature);
                    Log($"Считывание температуры: {temperature}°C.");

                    if (!CheckRps01MinMaxParam(StartAddressPlate.BoardTemperature, config.TemperMax, config.TemperMin, config.RpsReadDelay))
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
                    ushort relay1State = ReadRegister(slaveID, (ushort)StartAddressPlate.ACBCurrent);
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
                    ushort relay2State = ReadRegister(slaveID, (ushort)StartAddressPlate.ChargingVoltage);
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
                    ushort firmwareVersion = ReadRegister(slaveID, (ushort)StartAddressPlate.FirmwareVersion);
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

        #endregion








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
}