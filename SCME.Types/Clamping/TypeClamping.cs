using System.Runtime.Serialization;

namespace SCME.Types.Clamping
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDeviceState
    {
        [EnumMember] None = 0,
        [EnumMember] Fault = 1,
        [EnumMember] Disabled = 2,
        [EnumMember] Ready = 3,
        [EnumMember] Halt = 4,
        [EnumMember] ClampingDone = 8,
        [EnumMember] ClampingUpdate = 9
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFaultReason
    {
        [EnumMember] None = 0,
        [EnumMember] SafetyCircuit = 1,
        [EnumMember] Thermocouple = 2,
        [EnumMember] CANOpen = 3
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDisableReason
    {
        [EnumMember] None = 0,
        [EnumMember] BadClock = 1001
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWWarningReason
    {
        [EnumMember] None = 0,
        [EnumMember] WatchdogReset = 1001
    };

    public enum HeatingChannel
    {
        [EnumMember] Top = 0,
        [EnumMember] Bottom = 1,
        [EnumMember] Setting = 2
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWProblemReason
    {
        [EnumMember] None = 0,
        [EnumMember] NoForce = 1
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ClampingForceInternal
    {
        [EnumMember] Contact = 0,
        [EnumMember] Custom
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestParameters
    {
        [DataMember]
        public bool SkipClamping { get; set; }
        [DataMember]
        public ClampingForceInternal StandardForce { get; set; }
        [DataMember]
        public float CustomForce { get; set; }

        [DataMember]
        public bool IsHeightMeasureEnabled { get; set; }
        
        /// <summary>
        /// Высота прибора в мм, необходима для правильного алгоритма работы 
        /// </summary>
        [DataMember]
        public ushort Height { get; set; }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class CalibrationParams
    {
        [DataMember]
        public ushort ForceFineN { get; set; }

        [DataMember]
        public ushort ForceFineD { get; set; }

        [DataMember]
        public short ForceOffset { get; set; }
    }
}