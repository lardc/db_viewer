using System.Collections.Generic;
using System.ServiceModel;
using SCME.Types.BaseTestParams;
using SCME.Types.Commutation;
using SCME.Types.Profiles;
using SCME.Types.SCTU;

namespace SCME.Types
{
    public class ControlServerProxy : ClientBase<IExternalControl>, IExternalControl
    {
        public ControlServerProxy(string ServerEndpointConfigurationName, IClientCallback CallbackServiceProvider)
            : base(new InstanceContext(CallbackServiceProvider), ServerEndpointConfigurationName)
        {
        }

        public void Check()
        {
            Channel.Check();
        }

        public void Subscribe()
        {
            Channel.Subscribe();
        }

        public void Unsubscribe()
        {
            Channel.Unsubscribe();
        }

        public void Initialize(TypeCommon.InitParams Param)
        {
            Channel.Initialize(Param);
        }

        public void Deinitialize()
        {
            Channel.Deinitialize();
        }

        public InitializationResult IsInitialized()
        {
            return Channel.IsInitialized();
        }

        public bool GetButtonState(ComplexButtons Button)
        {
            return Channel.GetButtonState(Button);
        }

        public void ProvocationButtonResponse(ComplexButtons Button)
        {
            Channel.ProvocationButtonResponse(Button);
        }

        public ComplexSafety GetSafetyType()
        {
            return Channel.GetSafetyType();
        }

        public bool Start(Gate.TestParameters ParametersGate, SL.TestParameters ParametersVTM,
                          BVT.TestParameters ParametersBVT, ATU.TestParameters ParametersATU, QrrTq.TestParameters ParametersQrrTq, RAC.TestParameters ParametersRAC, IH.TestParameters ParametersIH, RCC.TestParameters ParametersRCC,
                          Commutation.TestParameters ParametersCommutation, Clamping.TestParameters ParametersClamping)
        {
            return Channel.Start(ParametersGate, ParametersVTM, ParametersBVT, ParametersATU, ParametersQrrTq, ParametersRAC, ParametersIH, ParametersRCC, ParametersCommutation, ParametersClamping);
        }

        public void Stop()
        {
            Channel.Stop();
        }

        public void StopByButtonStop()
        {
            Channel.StopByButtonStop();
        }

        public void ClearSafetyTrig()
        {
            Channel.ClearSafetyTrig();
        }

        public void SafetySystemOn()
        {
            Channel.SafetySystemOn();
        }

        public void SafetySystemOff()
        {
            Channel.SafetySystemOff();
        }

        public string NotReadyDevicesToStart()
        {
            return Channel.NotReadyDevicesToStart();
        }

        public string NotReadyDevicesToStartDynamic()
        {
            return Channel.NotReadyDevicesToStartDynamic();
        }

        public void SqueezeClamping(Clamping.TestParameters ParametersClamping)
        {
            Channel.SqueezeClamping(ParametersClamping);
        }

        public void UnsqueezeClamping(Clamping.TestParameters ParametersClamping)
        {
            Channel.UnsqueezeClamping(ParametersClamping);
        }

        public void ClearFault(ComplexParts Device)
        {
            Channel.ClearFault(Device);
        }

        public ushort ActivationWorkPlace(ComplexParts Device, ChannelByClumpType ChByClumpType, SctuWorkPlaceActivationStatuses ActivationStatus)
        {
            return Channel.ActivationWorkPlace(Device, ChByClumpType, ActivationStatus);
        }

        public ushort ReadRegister(ComplexParts Device, ushort Address)
        {
            return Channel.ReadRegister(Device, Address);
        }

        public void WriteRegister(ComplexParts Device, ushort Address, ushort Value)
        {
            Channel.WriteRegister(Device, Address, Value);
        }

        public void CallAction(ComplexParts Device, ushort Address)
        {
            Channel.CallAction(Device, Address);
        }

        public void GateWriteCalibrationParameters(Gate.CalibrationParameters Parameters)
        {
            Channel.GateWriteCalibrationParameters(Parameters);
        }

        public Gate.CalibrationParameters GateReadCalibrationParameters()
        {
            return Channel.GateReadCalibrationParameters();
        }

        public void SLWriteCalibrationParameters(SL.CalibrationParameters Parameters)
        {
            Channel.SLWriteCalibrationParameters(Parameters);
        }

        public SL.CalibrationParameters SLReadCalibrationParameters()
        {
            return Channel.SLReadCalibrationParameters();
        }

        public void BVTWriteCalibrationParameters(BVT.CalibrationParams Parameters)
        {
            Channel.BVTWriteCalibrationParameters(Parameters);
        }

        public BVT.CalibrationParams BVTReadCalibrationParameters()
        {
            return Channel.BVTReadCalibrationParameters();
        }

        public Clamping.CalibrationParams CSReadCalibrationParameters()
        {
            return Channel.CSReadCalibrationParameters();
        }

        public void CSWriteCalibrationParameters(Clamping.CalibrationParams Parameters)
        {
            Channel.CSWriteCalibrationParameters(Parameters);
        }

        public dVdt.CalibrationParams DvDtReadCalibrationParameters()
        {
            return Channel.DvDtReadCalibrationParameters();
        }

        public void DvDtWriteCalibrationParameters(dVdt.CalibrationParams Parameters)
        {
            Channel.DvDtWriteCalibrationParameters(Parameters);
        }

        public ATU.CalibrationParams ATUReadCalibrationParameters()
        {
            return Channel.ATUReadCalibrationParameters();
        }

        public void ATUWriteCalibrationParameters(ATU.CalibrationParams Parameters)
        {
            Channel.ATUWriteCalibrationParameters(Parameters);
        }

        public QRR.CalibrationParams QRRReadCalibrationParameters()
        {
            return Channel.QRRReadCalibrationParameters();
        }

        public void QRRWriteCalibrationParameters(QRR.CalibrationParams Parameters)
        {
            Channel.QRRWriteCalibrationParameters(Parameters);
        }

        public Gate.CalibrationResultGate GatePulseCalibrationGate(ushort Current)
        {
            return Channel.GatePulseCalibrationGate(Current);
        }

        public ushort GatePulseCalibrationMain(ushort Current)
        {
            return Channel.GatePulseCalibrationMain(Current);
        }

        public void WriteResults(ResultItem Item, List<string> Errors)
        {
            Channel.WriteResults(Item, Errors);
        }

        public void SaveProfiles(List<ProfileItem> Item)
        {
            Channel.SaveProfiles(Item);
        }

        public bool RequestRemotePrinting(string GroupName, string CustomerName, string DeviceType,
            ReportSelectionPredicate Predicate)
        {
            return Channel.RequestRemotePrinting(GroupName, CustomerName, DeviceType, Predicate);
        }

        public bool StartDynamic(TestParameters paramsComm, Clamping.TestParameters paramsClamp, Gate.TestParameters[] parametersGate, SL.TestParameters[] parametersVtm,
                          BVT.TestParameters[] parametersBvt, dVdt.TestParameters[] parametersDvDt, ATU.TestParameters[] parametersAtu,
                          QrrTq.TestParameters[] parametersQrrTq, RAC.TestParameters[] parametersRac, SctuTestParameters[] parametersSctu)
        {
            return Channel.StartDynamic(paramsComm, paramsClamp, parametersGate, parametersVtm, parametersBvt, parametersDvDt, parametersAtu, parametersQrrTq, parametersRac, parametersSctu);
        }

        public bool StartHeating(int temperature)
        {
            return Channel.StartHeating(temperature);
        }

        public void StopHeating()
        {
            Channel.StopHeating();
        }

        public void SetPermissionToUseCanDataBus(bool PermissionToUseCanDataBus)
        {
            Channel.SetPermissionToUseCanDataBus(PermissionToUseCanDataBus);
        }

    }
}