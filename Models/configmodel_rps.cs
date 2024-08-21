using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPStesting.Models
{
    class configmodel_rps
    {
        int config_loaded;//конфиг загружен
        int preheating_test;//тестирование preheating
        int rkn_test;//тестировать ли RKN
        int buildin_test;//проводить ли самотестирование
        int charging_test;
        int temper_min;//min/max пороги для проверки датчика температуры
        int temper_max;
        int akb_voltage_ac_min;//напряжение на акб при зарядке
        int akb_voltage_ac_max;
        int akb_charge_voltage_min;//напряжение зарядки
        int akb_charge_voltage_max;
        int load_XX_test;
        int load_XX_current_min;//ток разрядки на ХХ
        int load_XX_current_max;
        int load_16ohm_test;
        int load_16ohm_current_min;//ток разрядки на 16 ом
        int load_16ohm_current_max;
        int load_16ohm_voltage_min;//напряжение разрядки на 16 ом
        int load_16ohm_voltage_max;
        int load_22ohm_test;
        int load_22ohm_current_min;//ток разрядки на 22 ом
        int load_22ohm_current_max;
        int load_22ohm_voltage_min;//напряжение разрядки на 22 ом
        int load_22ohm_voltage_max;
        int load_68ohm_test;
        int load_68ohm_current_min;//ток разрядки на 68 ом
        int load_68ohm_current_max;
        int load_68ohm_voltage_min;//напряжение разрядки на 68 ом
        int load_68ohm_voltage_max;
        int rps_read_delay;//задержка на считывание значений с платы RPS

        //stand New
        int rkn_startup_time_min;//время старта RKN при подаче питания
        int rkn_startup_time_max;
        int rkn_disable_time; //максимальное время на отключение
        int relay1_test;
        int relay2_test;
        int fw_version;//версия ПО на плате RPS

        int rkn_380v_test;//тестировать ли подачей 380В
        int preheating_position;//положение джампера

        int load_100ohm_test;
        int load_100ohm_current_min;
        int load_100ohm_current_max;
        int load_100ohm_voltage_min;
        int load_100ohm_voltage_max;

        int load_70ohm_test;
        int load_70ohm_current_min;
        int load_70ohm_current_max;
        int load_70ohm_voltage_min;
        int load_70ohm_voltage_max;

        int load_50ohm_test;
        int load_50ohm_current_min;
        int load_50ohm_current_max;
        int load_50ohm_voltage_min;
        int load_50ohm_voltage_max;

        int load_30ohm_test;
        int load_30ohm_current_min;
        int load_30ohm_current_max;
        int load_30ohm_voltage_min;
        int load_30ohm_voltage_max;

        int load_20ohm_test;
        int load_20ohm_current_min;
        int load_20ohm_current_max;
        int load_20ohm_voltage_min;
        int load_20ohm_voltage_max;

        int load_15ohm_test;
        int load_15ohm_current_min;
        int load_15ohm_current_max;
        int load_15ohm_voltage_min;
        int load_15ohm_voltage_max;

        int load_15ohm_rps01_current_min;
        int load_15ohm_rps01_current_max;
        int load_15ohm_rps01_voltage_min;
        int load_15ohm_rps01_voltage_max;
    }
}
