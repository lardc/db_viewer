using System;
using System.Runtime.Serialization;

namespace SCME.Types
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ComplexParts
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        FTDI = 1,
        [EnumMember]
        Service = 2,
        [EnumMember]
        Adapter = 3,
        [EnumMember]
        Gateway = 4,
        [EnumMember]
        Commutation = 5,
        [EnumMember]
        CommutationEx = 6,
        [EnumMember]
        Gate = 7,
        [EnumMember]
        SL = 8,
        [EnumMember]
        BVT = 9,
        [EnumMember]
        Clamping = 10,
        [EnumMember]
        DvDt = 11,
        [EnumMember]
        QRR = 12,
        [EnumMember]
        Sctu = 13,
        [EnumMember]
        ATU = 14,
        [EnumMember]
        QrrTq = 15,
        [EnumMember]
        RAC = 16,
        [EnumMember]
        IH = 17,
        [EnumMember]
        RCC = 18,
        [EnumMember]
        Database = 255
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    [Flags]
    public enum InitializationResult
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        SyncInProgress = 1,
        [EnumMember]
        SyncedWithServer = 2,
        [EnumMember]
        ModulesInitialized = 4
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ComplexButtons
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        ButtonSC1,
        [EnumMember]
        ButtonSC2,
        [EnumMember]
        ButtonStart,
        [EnumMember]
        ButtonStop,
        [EnumMember]
        ButtonStartFTDI,
        [EnumMember]
        ButtonStopFTDI,
        [EnumMember]
        ClampSlidingSensor
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ScreenButtons
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        Stop
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ComplexSafety
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        Mechanical,
        [EnumMember]
        Optical,
        [EnumMember]
        AsButtonStop
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ComplexLamps
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        Lamp1,
        [EnumMember]
        Lamp2,
        [EnumMember]
        Lamp3
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum LogMessageType
    {
        [EnumMember]
        Note,
        [EnumMember]
        Warning,
        [EnumMember]
        Problem,
        [EnumMember]
        Error,
        [EnumMember]
        Info
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ReportSelectionPredicate
    {
        [EnumMember]
        All = 0,
        [EnumMember]
        QC,
        [EnumMember]
        Rejected
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum DeviceState
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        InProcess,
        [EnumMember]
        Success,
        [EnumMember]
        Stopped,
        [EnumMember]
        Problem,
        [EnumMember]
        Fault,
        [EnumMember]
        Disabled,
        [EnumMember]
        Heating,
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum DeviceConnectionState
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        ConnectionInProcess,
        [EnumMember]
        ConnectionSuccess,
        [EnumMember]
        ConnectionFailed,
        [EnumMember]
        ConnectionTimeout,
        [EnumMember]
        DisconnectionInProcess,
        [EnumMember]
        DisconnectionSuccess,
        [EnumMember]
        DisconnectionError
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ChannelByClumpType
    {
        [EnumMember]
        NullValue = 0,
        [EnumMember]
        Channel1 = 1,
        [EnumMember]
        Channel2 = 2
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum SubjectForMeasure
    {
        [EnumMember]
        Unknown = 0,
        [EnumMember]        
        PSE = 1,
        [EnumMember]        
        PSD = 2
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TypeCommon
    {
        [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
        public class InitParams
        {
            [DataMember]
            public int TimeoutService { get; set; }

            [DataMember]
            public int TimeoutAdapter { get; set; }

            [DataMember]
            public bool IsGateEnabled { get; set; }

            [DataMember]
            public int TimeoutGate { get; set; }

            [DataMember]
            public bool IsSLEnabled { get; set; }

            [DataMember]
            public int TimeoutSL { get; set; }

            [DataMember]
            public bool IsBVTEnabled { get; set; }

            [DataMember]
            public int TimeoutBVT { get; set; }

            [DataMember]
            public bool IsInternalEnabled { get; set; }

            [DataMember]
            public bool IsClampEnabled { get; set; }

            [DataMember]
            public int TimeoutClamp { get; set; }

            [DataMember]
            public bool IsdVdtEnabled { get; set; }

            [DataMember]
            public int TimeoutdVdt { get; set; }

            [DataMember]
            public bool IsQRREnabled { get; set; }

            [DataMember]
            public int TimeoutQRR { get; set; }

            [DataMember]
            public bool IsSctuEnabled { get; set; }

            [DataMember]
            public int TimeoutSctu { get; set; }

            [DataMember]
            public bool IsATUEnabled { get; set; }

            [DataMember]
            public int TimeoutATU { get; set; }

            [DataMember]
            public bool IsQrrTqEnabled { get; set; }

            [DataMember]
            public int TimeoutQrrTq { get; set; }

            [DataMember]
            public bool IsRACEnabled { get; set; }

            [DataMember]
            public int TimeoutRAC { get; set; }

            [DataMember]
            public bool IsIHEnabled { get; set; }

            [DataMember]
            public int TimeoutIH { get; set; }

            [DataMember]
            public bool IsRCCEnabled { get; set; }

            [DataMember]
            public int TimeoutRCC { get; set; }
        }

        [DataMember]
        public InitParams Params { get; set; }

        public TypeCommon()
        {
            Params = new InitParams();
        }
    }
}