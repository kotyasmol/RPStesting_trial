using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RPStesting.Models
{
    public class TestConfigModel
    {
        [JsonProperty("model_name")]
        public string ModelName { get; set; }

        [JsonProperty("rkn_test")]
        public int RknTest { get; set; }

        [JsonProperty("buildin_test")]
        public int BuildinTest { get; set; }

        [JsonProperty("temper_min")]
        public int TemperMin { get; set; }

        [JsonProperty("temper_max")]
        public int TemperMax { get; set; }

        [JsonProperty("Напряжение на АКБ при разрядке")]
        public bool IsAkbDischargeVoltageEnabled { get; set; }

        [JsonProperty("akb_voltage_ac_min")]
        public int AkbVoltageAcMin { get; set; }

        [JsonProperty("akb_voltage_ac_max")]
        public int AkbVoltageAcMax { get; set; }

        [JsonProperty("Напряжение на АКБ при зарядке")]
        public bool IsAkbChargeVoltageEnabled { get; set; }

        [JsonProperty("akb_charge_voltage_min")]
        public int AkbChargeVoltageMin { get; set; }

        [JsonProperty("akb_charge_voltage_max")]
        public int AkbChargeVoltageMax { get; set; }

        [JsonProperty("load_XX_test")]
        public int LoadXXTest { get; set; }

        [JsonProperty("load_XX_current_min")]
        public int LoadXXCurrentMin { get; set; }

        [JsonProperty("load_XX_current_max")]
        public int LoadXXCurrentMax { get; set; }

        [JsonProperty("load_16ohm_test")]
        public int Load16OhmTest { get; set; }

        [JsonProperty("load_16ohm_current_min")]
        public int Load16OhmCurrentMin { get; set; }

        [JsonProperty("load_16ohm_current_max")]
        public int Load16OhmCurrentMax { get; set; }

        [JsonProperty("load_16ohm_voltage_min")]
        public int Load16OhmVoltageMin { get; set; }

        [JsonProperty("load_16ohm_voltage_max")]
        public int Load16OhmVoltageMax { get; set; }

        [JsonProperty("load_22ohm_test")]
        public int Load22OhmTest { get; set; }

        [JsonProperty("load_22ohm_current_min")]
        public int Load22OhmCurrentMin { get; set; }

        [JsonProperty("load_22ohm_current_max")]
        public int Load22OhmCurrentMax { get; set; }

        [JsonProperty("load_22ohm_voltage_min")]
        public int Load22OhmVoltageMin { get; set; }

        [JsonProperty("load_22ohm_voltage_max")]
        public int Load22OhmVoltageMax { get; set; }

        [JsonProperty("load_68ohm_test")]
        public int Load68OhmTest { get; set; }

        [JsonProperty("load_68ohm_current_min")]
        public int Load68OhmCurrentMin { get; set; }

        [JsonProperty("load_68ohm_current_max")]
        public int Load68OhmCurrentMax { get; set; }

        [JsonProperty("load_68ohm_voltage_min")]
        public int Load68OhmVoltageMin { get; set; }

        [JsonProperty("load_68ohm_voltage_max")]
        public int Load68OhmVoltageMax { get; set; }

        [JsonProperty("Задержка на считывание параметров с платы RPS-01")]
        public bool IsRpsReadDelayEnabled { get; set; }

        [JsonProperty("rps_read_delay")]
        public int RpsReadDelay { get; set; }

        [JsonProperty("firmware_path")]
        public string FirmwarePath { get; set; }
    }
}
