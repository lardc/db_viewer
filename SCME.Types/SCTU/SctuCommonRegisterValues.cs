using System.Runtime.Serialization;

namespace SCME.Types.SCTU
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum SctuWorkPlaceActivationStatuses : ushort
    {
        /// <summary>
        /// Статус активации рабочего места установки ударного тока 'Свободно'.
        /// </summary>
        [EnumMember]
        WORKPLACE_IS_FREE = 0,

        /// <summary>
        /// Статус активации рабочего места установки ударного тока 'Занято'.
        /// </summary>
        [EnumMember]
        WORKPLACE_IN_USE = 1,

        /// <summary>
        /// Статус активации рабочего места установки ударного тока 'Заблокировано'.
        /// </summary>
        WORKPLACE_IS_BLOCKED = 2
    }
}