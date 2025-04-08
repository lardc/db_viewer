using System.Runtime.Serialization;

namespace SCME.Types.SCTU
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum SctuHwState
    {
        /// <summary>
        /// Cостояние после включения питания
        /// </summary>
        [EnumMember]
        None = 0,
        
        /// <summary>
        /// Состояние ошибки, которое можно сбросить
        /// </summary>
        [EnumMember]
        Fault = 1,
        
        /// <summary>
        /// Состояние ошибки, требующее перезапуска питания
        /// </summary>
        [EnumMember]
        Disabled = 2,
        
        /// <summary>
        /// Состояние ожидания истечения таймаута между импульсами ударного тока
        /// </summary>
        [EnumMember]
        WaitTimeOut = 3,
        
        /// <summary>
        /// В состоянии ожидания заряда батареи
        /// </summary>
        [EnumMember]
        BatteryChargeWait = 4,
        
        /// <summary>
        /// Готов
        /// </summary>
        [EnumMember]
        Ready = 5,
        
        /// <summary>
        /// Установка сконфигурирована под заданное значение ударного тока
        /// </summary>
        [EnumMember]
        PulseConfigReady = 6,
        
        /// <summary>
        /// Установка в состоянии формирования импульса ударного тока
        /// </summary>
        [EnumMember]
        PulseStart = 7,
        
        /// <summary>
        /// Импульс ударного тока сформирован
        /// </summary>
        [EnumMember]
        PulseEnd = 8,
        
        /// <summary>
        /// Установка в состоянии конфигурации под заданное значение ударного тока
        /// </summary>
        [EnumMember]
        PulseConfig = 9,
        
        /// <summary>
        /// Установка в состоянии отправки команды на заряд батареи силовым блокам
        /// </summary>
        [EnumMember]
        BatteryChargeStart = 10,

        /// <summary>
        /// Начало измерения
        /// </summary>
        [EnumMember]       
        InProcess = 11
    }
}
