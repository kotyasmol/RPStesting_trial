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
using System.Windows;
using HandyControl.Tools.Command;
using HandyControl.Tools;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace RPStesting.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        /* 
         private ushort _registerValue;
         private string _inputValue;

         public ICommand DisconnectCommand { get; }
         public ICommand ReadAllRegistersCommand { get; }



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






         private void WriteACRegister(bool isConnected)
         {
             try
             {
                 byte slaveID = 2;
                 ushort startAddress = (ushort)StartAddress.ACConnection; // 1300-й регистр - подача питания на плату, без него  плата = пустышка нечитаемая

                 ushort valueToWrite = isConnected ? (ushort)1 : (ushort)0; // 1 - Включено, 0 - Выключено
                 WriteModbus(slaveID, startAddress, valueToWrite);
                 //_modbusMaster.WriteSingleRegister(slaveID, startAddress, valueToWrite);
                 Log($"Register {startAddress} set to {valueToWrite}");
                 ReadAllRegisters(null); // Обновление значений регистров
             }
             catch (Exception ex)
             {
                //Log($"Error writing to register {StartAddress}: {ex.Message}");
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

                 foreach (StartAddress address in Enum.GetValues(typeof(StartAddress))) // чтение всего со стенда
                 {
                     ushort startAddress = (ushort)address;
                     ushort numOfPoints = 1;

                     // Чтение значения регистра
                     ushort[] holdingRegister = _modbusMaster.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

                     // Добавление значения в список
                     registerValues.Add($"{address}: {holdingRegister[0]}");
                     //Log($"Read register {address} ({startAddress}): {holdingRegister[0]}"); ВРЕМЕННО  КОММЕНТ
                 }

                 foreach (StartAddressPlate address in Enum.GetValues(typeof(StartAddressPlate))) // чтение всего с платы
                 {
                     ushort startAddress = (ushort)address;
                     ushort numOfPoints = 1;

                     // Чтение значения регистра
                     ushort[] holdingRegister = _modbusMaster.ReadHoldingRegisters(slaveIdFirst, startAddress, numOfPoints);

                     // Добавление значения в список
                     registerValues_Plate.Add($"{address}: {holdingRegister[0]}");
                     //Log($"Read register {address} ({startAddress}): {holdingRegister[0]}"); ВРЕМЕННО КОММЕНТ
                 }


                 Log("All registers read successfully.");
             }
             catch (Exception ex)
             {
                 Log($"Read error: {ex.Message}");
             }
         }




         /// конфиг 
        









         public void SetRpsPreheating(int value)
         {
             Log($"Установка эквивалента температуры: {value}");
             byte slaveID = 2;
             ushort registerAddress = 1304;
             WriteModbus(slaveID, registerAddress, value);
         }
         
         private int GetRknStartupTime(byte slaveID)
         {
             try
             {
                 // Чтение времени старта RKN с соответствующего регистра
                 ushort registerAddress = (ushort)StartAddress.TemperatureSimulator;
                 ushort[] registerValues = _modbusMaster.ReadHoldingRegisters(slaveID, registerAddress, 1);

                 // Возвращаем время старта RKN
                 int rknStartupTime = registerValues[0];
                 Log($"Время старта узла RKN: {rknStartupTime}");
                 return rknStartupTime;
             }
             catch (Exception ex)
             {
                 Log($"Ошибка при чтении времени старта узла RKN: {ex.Message}");
                 throw;
             }
         }

         #region Автоматическое тестирование всех параметров платы

         // Основной метод запуска тестирования
      

         #endregion

         #region Подготовка к тестированию

         // Подготовка регистров перед тестом
         public void PrepareMasterForTesting(byte slaveID)
         {
             // Подключение ЛАТР
             WriteModbus(slaveID, (ushort)StartAddress.LatrConnection, 1);
             Log("Подключение ЛАТР");

             // Отключение AC
             WriteModbus(slaveID, (ushort)StartAddress.ACConnection, 0);
             Log("Отключение AC");

             // Отключение сопротивления нагрузки
             WriteModbus(slaveID, (ushort)StartAddress.LoadSwitchKey, 0);
             Log("Отключение сопротивления нагрузки");

             // Установка сопротивления нагрузки: 100 Ом
             WriteModbus(slaveID, (ushort)StartAddress.ResistanceSetting, 100);
             Log("Установка сопротивления нагрузки: 100 Ом");

             // Отключение RELAY
             WriteModbus(slaveID, (ushort)StartAddress.RelayState, 0);
             Log("Отключение RELAY");

             // Отключение AC_OK
             WriteModbus(slaveID, (ushort)StartAddress.AC_OKRelayState, 0);
             Log("Отключение AC_OK");

             Log("Подготовка к тестированию завершена.");
         }

         #endregion

         #region Тестирование узла Preheating

         // Тестирование узла Preheating
         private void RunPreheatingTest(byte slaveID, TestConfigModel config)
         {
             if (!config.IsPreheatingTestEnabled)
             {
                 Log("Тестирование узла Preheating отключено.");
                 return;
             }

             Log("Запуск теста узла Preheating");

             // Запрос на установку джампера
             MessageBox.Show("Установите джампер «PREHEATING» в положение «YES»", "Инструкция", MessageBoxButton.OK, MessageBoxImage.Information);

             // Старт при -30°C
             Log("Старт при -30°C");
             SetRpsPreheating(-30);


             // Проверка времени старта узла RKN
             int rknStartupTime = GetRknStartupTime(slaveID);
             Log($"Время старта узла RKN: {rknStartupTime}");

             // Переход на -35°C
             SetRpsPreheating(-35);
             Log("Установка температуры -35°C");

             // Добавляем проверки температур на каждом этапе (например, мин/макс значения)
            /* if (!CheckRps01MinMaxParam(StartAddress.TemperatureSimulator, config.TemperMax, config.TemperMin, config.RpsReadDelay))
             {
                 throw new Exception("Ошибка при проверке температуры узла Preheating.");
             }

             Log("Тестирование узла Preheating успешно завершено.");
         }

         #endregion

         #region Самотестирование

         // Самотестирование RPS-01
         public void RunSelfTest(byte slaveID, TestConfigModel config)
         {
             if (!config.IsBuildinTestEnabled)
             {
                 Log("Самотестирование отключено.");
                 return;
             }

             Log("Запуск самотестирования RPS-01");

             // Проверка температуры на плате
             if (config.IsTemperMinMaxEnabled)
             {
                 ushort temperature = ReadRegister(slaveID, (ushort)StartAddressPlate.BoardTemperature);
                 Log($"Считывание температуры: {temperature}°C.");

                 if (!CheckRps01MinMaxParam(StartAddressPlate.BoardTemperature, config.TemperMax, config.TemperMin, config.RpsReadDelay))
                 {
                     throw new Exception("Температура на плате выходит за пределы допустимых значений.");
                 }

                 Log("Температура на плате в пределах нормы.");
             }

             // Проверка состояния реле RELAY
             if (config.IsRelay1TestEnabled)
             {
                 ushort relay1State = ReadRegister(slaveID, (ushort)StartAddressPlate.ACBCurrent);
                 Log($"Состояние реле RELAY: {relay1State}.");

                 if (relay1State != config.Relay1Test)
                 {
                     throw new Exception("Неправильное состояние реле RELAY.");
                 }

                 Log("Состояние реле RELAY в пределах нормы.");
             }

             // Проверка состояния реле AC_OK
             if (config.IsRelay2TestEnabled)
             {
                 ushort relay2State = ReadRegister(slaveID, (ushort)StartAddressPlate.ChargingVoltage);
                 Log($"Состояние реле AC_OK: {relay2State}.");

                 if (relay2State != config.Relay2Test)
                 {
                     throw new Exception("Неправильное состояние реле AC_OK.");
                 }

                 Log("Состояние реле AC_OK в пределах нормы.");
             }

             // Проверка версии прошивки
             if (config.FirmwareVersion != 0)
             {
                 ushort firmwareVersion = ReadRegister(slaveID, (ushort)StartAddressPlate.FirmwareVersion);
                 Log($"Версия прошивки: {firmwareVersion}.");

                 if (firmwareVersion != config.FirmwareVersion)
                 {
                     throw new Exception("Неправильная версия прошивки.");
                 }

                 Log("Версия прошивки соответствует ожидаемой.");
             }

             Log("Самотестирование завершено успешно.");
         }

         #endregion

         #region Тестирование узла RKN

         // Тестирование узла RKN
         public void RunRknTest(byte slaveID, TestConfigModel config)
         {
             if (!config.IsRknTestEnabled)
             {
                 Log("Тестирование узла RKN отключено.");
                 return;
             }

             Log("Запуск тестирования узла RKN");

             // Проверка времени старта RKN
             int rknStartupTime = GetRknStartupTime(slaveID);
             if (rknStartupTime < config.RknStartupTimeMin || rknStartupTime > config.RknStartupTimeMax)
             {
                 throw new Exception("Время старта узла RKN выходит за допустимые пределы.");
             }

             Log($"Время старта узла RKN: {rknStartupTime} - в пределах нормы.");

             // Дополнительные проверки узла RKN...
         }

         #endregion

         #region Завершение теста

         // Возврат параметров стенда в исходное состояние
         private void ResetStandParameters(byte slaveID)
         {
             WriteModbus(slaveID, (ushort)StartAddress.LatrConnection, 0);
             WriteModbus(slaveID, (ushort)StartAddress.ACConnection, 0);
             WriteModbus(slaveID, (ushort)StartAddress.LoadSwitchKey, 0);
             WriteModbus(slaveID, (ushort)StartAddress.RelayState, 0);
             WriteModbus(slaveID, (ushort)StartAddress.AC_OKRelayState, 0);
             Log("Параметры стенда возвращены в исходное состояние.");
         }

         #endregion

       */
        private ModbusSerialMaster _modbusMaster;
        private SerialPort _serialPort;

        public ICommand ConnectCommand { get; }
        private bool _isConnected; 
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
        private void Connect(object parameter)
        {
            _serialPort = new SerialPort("COM3") // добавить выбор порта 
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
                Log("Стенд подключен.");
            }
            catch (Exception ex)
            {
                Log($"Невозможно подключиться: {ex.Message}");
            }
        }


        public ICommand SelectJsonFileCommand { get; }
        public TestConfigModel Config { get; set; } // Добавляем свойство Config
        private string _jsonFilePath;
        public string JsonFilePath
        {
            get => _jsonFilePath;
            set
            {
                if (_jsonFilePath != value)
                {
                    _jsonFilePath = value;
                    OnPropertyChanged(nameof(JsonFilePath));
                    LoadConfig(); // Загрузка конфигурации после выбора файла
                }
            }
        }
        private void SelectJsonFile(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Выберите JSON файл конфигурации"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                JsonFilePath = openFileDialog.FileName;
                Log($"Выбран файл: {JsonFilePath}");
            }
        }
        private void LoadConfig()
        {
            string jsonFilePath = JsonFilePath; // Используем путь, выбранный пользователем
            if (string.IsNullOrEmpty(jsonFilePath))
            {
                Log("Путь к файлу конфигурации не задан. Пожалуйста, выберите файл конфигурации.");
                return;
            }

            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(jsonFilePath);
                    Config = JsonConvert.DeserializeObject<TestConfigModel>(jsonData);

                    if (Config == null)
                    {
                        Config = new TestConfigModel(); // Создаем пустой объект, чтобы избежать NullReferenceException
                        Log($"Файл конфигурации пуст или не может быть десериализован с использованием конфигурации по умолчанию.");
                    }
                    else
                    {
                        Log($"Конфигурация загружена для модели: {Config.ModelName}");
                    }
                }
                catch (Exception ex)
                {
                    Config = new TestConfigModel(); // Создаем пустой объект в случае ошибки десериализации
                    Log($"Не удалось загрузить конфигурацию: {ex.Message}");
                }
            }
            else
            {
                Config = new TestConfigModel(); // Создаем пустой объект, если файл не найден
                Log("Файл конфигурации не найден. Использование конфигурации по умолчанию.");
            }
        }


        public enum StartAddress // значения стенда слейв 2 
        {
            ACConnection = 1300,          // 1300 - Подключение AC (230V)
            LatrConnection,               // 1301 - Подключение ЛАТР
            ACBConnection,                // 1302 - Подключение АКБ (ИМИТАТОР АКБ)
            ACBPolarity,                  // 1303 - Полярность АКБ
            TemperatureSimulator,         // 1304 - Имитатор термодатчика (-40, -35, -30)
            AC_OKRelayState,              // RO 1305 - Состояние реле 1 (AC_OK) 
            RelayState,                   // RO 1306 - Состояние реле 2 (Relay)
            LoadSwitchKey,                // 1307 - Ключ подключения нагрузки
            ResistanceSetting,            // 1308 - Установка сопротивления для контроля тока зарядки, Ом (от 3,3 до 267)
            ACBVoltage,                   // RO 1309 - Напряжение на АКБ, mV
            ACBAmperage,                  // RO 1310 - Ток через АКБ, mA
            V230PresenceAtEntrance,       // RO 1311 - Присутствие напряжения 230V на входе RPS
            V230PresenceAtExit,           // RO 1312 - Присутствие напряжения 230V на выходе RPS
            Sensor1Temperature,           // RO 1313 - Температура датчика 1
            Sensor2Temperature,           // RO 1314 - Температура датчика 2
            CoolerControlKey,             // 1315 - Ключ управления вентиляторами
            FanOffTemperature,            // 1316 - Температура выключения вентиляторов
            FanOnTemperature,             // 1317 - Температура включения вентиляторов
            MaxRadiatorTemperature,       // 1318 - Установка максимальной температуры на радиаторе
            StatisticsReset               // 1319 - Очистка статистики
        }
        public enum StartAddressPlate // слейв 1
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
        private ushort ReadRegister(byte slaveID, ushort registerAddress)
        {
            ushort[] result = _modbusMaster.ReadHoldingRegisters(slaveID, registerAddress, 1);
            return result[0];
        }
        private void WriteRegister(byte slaveID, ushort registerAddress, int value) 
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



        public void ProcessPreheatingPosition(JObject jsonObject)
        {
            // Проверка, существует ли ключ "preheating_position" и является ли числом
            if (jsonObject["preheating_position"] != null && jsonObject["preheating_position"].Type == JTokenType.Integer)
            {
                // Присваиваем значение из JSON
                Config.PreheatingPosition = jsonObject["preheating_position"].ToObject<int>();
            }
            else
            {
                // Если ключ не найден или значение не является числом, присваиваем значение по умолчанию
                Config.PreheatingPosition = 0;
                Log("Переменная не найдена: preheating_position");
            }
        }



        public ICommand StartTestingCommand { get; }
        private void StartTestCommandExecute(object parameter)
        {
            StartTesting(1, Config);  // Вызов метода StartTesting
        }
        public void StartTesting(byte slaveID, TestConfigModel config)
        {
            try
            {
                /*
                // 1. Подготовка регистров
                PrepareMasterForTesting(slaveID);
                

                // 2. Тестирование узла Preheating
                RunPreheatingTest(slaveID, config);

                // 3. Самотестирование
                RunSelfTest(slaveID, config);

                // 4. Тестирование узла RKN
                RunRknTest(slaveID, config);
                */
                 MessageBox.Show("Переведите джампер в положение YES", "Инструкция", MessageBoxButton.OK, MessageBoxImage.Information);
                if (Preheatingtest()) { Log("Все этапы тестирования завершены успешно."); }


            }
            catch (Exception ex)
            {
                Log($"Ошибка тестирования: {ex.Message}");
            }
            finally
            {
                WriteRegister(2, (ushort)StartAddress.LatrConnection, 0);
                WriteRegister(2, (ushort)StartAddress.ACConnection, 0);
                WriteRegister(2, (ushort)StartAddress.LoadSwitchKey, 1); //или не 1?
                WriteRegister(2, (ushort)StartAddress.ResistanceSetting, 4);

                Log("Все параметры стенда возвращены в исходное состояние.");
            }
        }
        public bool Preheatingtest()
        {
            try
            {
                WriteRegister(2, (ushort)StartAddress.LatrConnection, 1);
                WriteRegister(2, (ushort)StartAddress.ACConnection, 0);
                WriteRegister(2, (ushort)StartAddress.LoadSwitchKey, 0);
                WriteRegister(2, (ushort)StartAddress.ResistanceSetting, 100);

                return true;
            }
            catch (Exception ex)
            {
                Log($"Не получилось записать в : {ex.Message}");
                return false;
            }
        }



        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(Connect, param => !IsConnected);
            SelectJsonFileCommand = new RelayCommand(SelectJsonFile);
            StartTestingCommand = new RelayCommand(StartTestCommandExecute);


            LogMessages = new ObservableCollection<string>();

        }


        private void Log(string message)
        {
            LogMessages.Add($"{DateTime.Now}: {message}");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged; // Для обновления GUI
        public ObservableCollection<string> LogMessages { get; private set; }

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