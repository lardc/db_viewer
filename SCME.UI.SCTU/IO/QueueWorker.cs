using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Threading;
using SCME.Types;
using SCME.Types.SCTU;
using SCME.UI.Properties;
using SCME.UI.CustomControl;

namespace SCME.UI.IO
{
    public class QueueWorker
    {
        private readonly ControlLogic m_Net;
        private readonly ConcurrentQueue<Action> m_ActionQueue;
        private readonly DispatcherTimer m_Timer;
        private volatile bool m_Stop;

        public QueueWorker(ControlLogic Net)
        {
            m_Net = Net;
            m_ActionQueue = new ConcurrentQueue<Action>();
            m_Timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 50) };
            m_Timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            Action action;

            while (m_ActionQueue.TryDequeue(out action)) { }

            m_Stop = false;
            m_Timer.Start();
        }

        private void Timer_Tick(object Sender, EventArgs E)
        {
            Action act;

            if (m_Stop)
            {
                m_Timer.Stop();
                return;
            }

            while (m_ActionQueue.TryDequeue(out act))
                act.Invoke();
        }

        public void AddCommonConnectionEvent(DeviceConnectionState State, string Message)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    switch (State)
                    {
                        case DeviceConnectionState.ConnectionSuccess:
                            {
                                //Cache.Calibration.PatchClamp();
                                Cache.Main.mainFrame.Navigate(Cache.ActivationWorkPage); //Cache.SctuPage
                                Cache.Welcome.IsRestartEnable = true;
                                Cache.Welcome.IsBackEnable = true;

                                if (Settings.Default.FTDIPresent)
                                {
                                    Cache.FTDI.LedRedSwitch(false);
                                    Cache.FTDI.LedGreenSwitch(false);
                                }

                                Cache.Main.IsSafetyBreakIconVisible = !Cache.Net.GetButtonState(ComplexButtons.ButtonSC1);
                            }
                            break;
                        case DeviceConnectionState.ConnectionFailed:
                            {
                                Cache.Welcome.IsRestartEnable = true;

                                if (Settings.Default.FTDIPresent)
                                {
                                    Cache.FTDI.LedGreenSwitch(false);
                                    Cache.FTDI.LedRedBlinkStart();
                                }
                            }
                            break;
                        case DeviceConnectionState.DisconnectionError:
                        case DeviceConnectionState.DisconnectionSuccess:
                            {
                                if (Cache.Main.IsNeedToRestart)
                                    m_Net.Initialize(Cache.Main.Param);

                                if (Settings.Default.FTDIPresent)
                                {
                                    Cache.FTDI.LedRedSwitch(false);
                                    Cache.FTDI.LedGreenSwitch(false);
                                }
                            }
                            break;
                    }
                });
        }

        public void AddDeviceConnectionEvent(ComplexParts Device, DeviceConnectionState State, string Message)
        {
            m_ActionQueue.Enqueue(() => Cache.Welcome.State(Device, State, Message));
        }

        public void AddTestAllEvent(DeviceState State, string Message)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                    {
                        Cache.UserTest.SetResultAll(State);
                    }
                    else
                    {
                        Cache.Gate.SetResultAll(State);
                        Cache.SL.SetResultAll(State);
                        Cache.Bvt.SetResultAll(State);
                    }

                    if (Settings.Default.FTDIPresent)
                    {
                        if (State == DeviceState.InProcess)
                        {
                            Cache.FTDI.LedRedSwitch(false);
                            Cache.FTDI.LedGreenBlinkStart();
                        }
                        else if (State == DeviceState.Fault)
                        {
                            Cache.FTDI.LedGreenSwitch(false);
                            Cache.FTDI.LedRedBlinkStart();
                        }
                        else
                        {
                            Cache.FTDI.LedRedSwitch(false);
                            Cache.FTDI.LedGreenSwitch(false);
                        }
                    }
                });
        }

        public void AddExceptionEvent(ComplexParts Device, string Message)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    Cache.Welcome.IsBackEnable = false;
                    Cache.Welcome.IsRestartEnable = true;
                    Cache.Welcome.State(Device, DeviceConnectionState.ConnectionFailed, Message);

                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                    {
                        Cache.UserTest.SetResultAll(DeviceState.Fault);
                    }
                    else
                    {
                        Cache.Gate.SetResultAll(DeviceState.Fault);
                        Cache.SL.SetResultAll(DeviceState.Fault);
                        Cache.Bvt.SetResultAll(DeviceState.Fault);
                    }

                    Cache.Main.mainFrame.Navigate(Cache.Welcome);

                    if (Settings.Default.FTDIPresent)
                    {
                        Cache.FTDI.LedGreenSwitch(false);
                        Cache.FTDI.LedRedBlinkStart();
                    }
                });
        }

        public void AddStopEvent()
        {
            m_ActionQueue.Enqueue(delegate
            {
                var dw = new DialogWindow(Resources.Information, Resources.StopButtonIsPressed);

                dw.ButtonConfig(DialogWindow.EbConfig.OK);
                dw.ShowDialog();

                if (dw.DialogResult ?? false)
                {
                    //пользователь нажал в появившемся диалоговом окне кнопку OK. читаем состояние аппаратной кнопки 'Стоп', которая имеет фиксацию
                    bool IsButtonStopPressed = Cache.Net.GetButtonState(ComplexButtons.ButtonStop);

                    //кнопка 'Стоп' с фиксацией, поэтому прежде чем отвести пресс проверим, что кнопка 'Стоп' отжата в исходное состояние
                    switch (IsButtonStopPressed)
                    {
                        //кнопка 'Стоп' вжата - провоцируем её повторное срабатывание чтобы снова попасть в обработку нажатия кнопки 'Стоп' и так будем делать до тех пор, пока пользователь её не отожмёт
                        case (true):                            
                            Cache.Net.ProvocationButtonResponse(ComplexButtons.ButtonStop);
                            break;

                        //кнопка 'Стоп' отжата
                        default:
                            //сбрасываем состояние SafetyTrig. справедливо для оптической, механической и шторки подключенной как кнопка 'Стоп'. этот вызов сбросит состояние Halt пресса, только после этого можно разжать пресс
                            Cache.Net.ClearSafetyTrig();

                            //разжимаем зажимное устройство
                            Cache.Clamp.IsRunning = false;
                            Cache.Clamp.Unclamp();

                            //прячем иконку Safety
                            Cache.Main.IsSafetyBreakIconVisible = false;

                            //строим строку с именами устройств, которые не готовы к очередному измерению
                            string NotReadyDevicesToStart = Cache.Net.NotReadyDevicesToStart();
                            string NotReadyDevicesToStartDynamic = Cache.Net.NotReadyDevicesToStartDynamic();

                            if (NotReadyDevicesToStartDynamic != "")
                            {
                                if (NotReadyDevicesToStart != "")
                                    NotReadyDevicesToStart = NotReadyDevicesToStart + ", ";

                                NotReadyDevicesToStart = NotReadyDevicesToStart + NotReadyDevicesToStartDynamic;
                            }

                            //проверяем есть ли блоки, которые не готовы к проведению очередного измерения
                            if (NotReadyDevicesToStart != "")
                            {
                                //ругаемся, т.к. есть блоки, которые не готовы выполнить очередное измерение
                                //вывешиваем пользователю форму с сообщением о не готовых к очередному измерению блоках
                                dw = new DialogWindow(Resources.Information, "Блоки: " + NotReadyDevicesToStart + " не готовы для проведения измерений.");

                                dw.ButtonConfig(DialogWindow.EbConfig.OK);
                                dw.ShowDialog();
                            }

                            break;
                    }
                }
            });
        }

        public void AddButtonPressedEvent(ComplexButtons Button, bool State)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (State && (Button == ComplexButtons.ButtonStartFTDI || Button == ComplexButtons.ButtonStart))
                    {
                        if (Equals(Cache.Main.mainFrame.Content, Cache.UserTest))
                            Cache.UserTest.StartFirst();
                        else if (Equals(Cache.Main.mainFrame.Content, Cache.Gate))
                            Cache.Gate.Start();
                        else if (Equals(Cache.Main.mainFrame.Content, Cache.SL))
                            Cache.SL.Start();
                        else if (Equals(Cache.Main.mainFrame.Content, Cache.Bvt))
                            Cache.Bvt.Start();
                    }

                    if (State && (Button == ComplexButtons.ButtonStopFTDI || Button == ComplexButtons.ButtonStop))
                        Cache.Net.StopByButtonStop();

                    if (Button == ComplexButtons.ButtonSC1)
                    {
                        Cache.Main.IsSafetyBreakIconVisible = !State;

                        if (!State)
                            Cache.Net.Stop();
                    }
                });
        }

        public void AddSafetyHandlerEvent(bool Alarm, ComplexSafety SafetyType, ComplexButtons Button)
        {
            m_ActionQueue.Enqueue(delegate
            {
                //максимально быстро реагируем на возникший Safety Alarm
                if (Alarm)
                    Cache.Net.Stop();

                //показываем или прячем иконку состояния системы Safety
                Cache.Main.IsSafetyBreakIconVisible = Alarm;

                if (Alarm)
                {
                    //вывешиваем пользователю форму с сообщением о сработавшей системе безопасности
                    string Message;

                    //определяемся с тем, какая система безопасности сработала чтобы выдать пользователю вразумительное сообщение
                    switch (SafetyType)
                    {
                        case ComplexSafety.Optical:
                            //принятый параметр Button для случая оптической шторки не имеет никакого значения 
                            Message = Resources.SafetyOpticalAlarm;
                            break;

                        case ComplexSafety.Mechanical:
                            //дописываем какой датчик сработал
                            Message = Resources.CloseSafetyHood + " (" + Button.ToString() + ").";
                            break;

                        default:
                            Message = "Unknown safety system is in alarm, realization fault.";
                            break;
                    }

                    var dw = new DialogWindow(Resources.Information, Message);

                    dw.ButtonConfig(DialogWindow.EbConfig.OK);
                    dw.ShowDialog();

                    if (dw.DialogResult ?? false)
                    {
                        //пользователь нажал в появившемся диалоговом окне кнопку OK
                        //сбрасываем состояние SafetyTrig. справедливо и для оптической и механической шторки. только после этого можно разжимать пресс                     
                        Cache.Net.ClearSafetyTrig();

                        //разжимаем зажимное устройство
                        Cache.Clamp.IsRunning = false;
                        Cache.Clamp.Unclamp();

                        //прячем иконку Safety
                        Cache.Main.IsSafetyBreakIconVisible = false;

                        //строим строку с именами устройств, которые не готовы к очередному измерению
                        string NotReadyDevicesToStart = Cache.Net.NotReadyDevicesToStart();
                        string NotReadyDevicesToStartDynamic = Cache.Net.NotReadyDevicesToStartDynamic();

                        if (NotReadyDevicesToStartDynamic != "")
                        {
                            if (NotReadyDevicesToStart != "")
                                NotReadyDevicesToStart = NotReadyDevicesToStart + ", ";

                            NotReadyDevicesToStart = NotReadyDevicesToStart + NotReadyDevicesToStartDynamic;
                        }

                        //проверяем есть ли блоки, которые не готовы к проведению очередного измерения
                        if (NotReadyDevicesToStart != "")
                        {
                            //ругаемся, т.к. есть блоки, которые не готовы выполнить очередное измерение
                            //вывешиваем пользователю форму с сообщением о не готовых к очередному измерению блоках
                            dw = new DialogWindow(Resources.Information, "Блоки: " + NotReadyDevicesToStart + " не готовы для проведения измерений.");

                            dw.ButtonConfig(DialogWindow.EbConfig.OK);
                            dw.ShowDialog();
                        }
                    }
                }
            });
        }

        public void AddGatewayWarningEvent(Types.Gateway.HWWarningReason Warning) { }

        public void AddGatewayFaultEvent(Types.Gateway.HWFaultReason Fault) { }

        public void AddCommutationSwitchEvent(Types.Commutation.CommutationMode ComSwitch) { }

        public void AddCommutationWarningEvent(Types.Commutation.HWWarningReason Warning) { }

        public void AddCommutationFaultEvent(Types.Commutation.HWFaultReason Fault) { }

        public void AddGateAllEvent(DeviceState State) { }

        public void AddGateKelvinEvent(DeviceState state, bool isKelvinOk, IList<short> Array, long testTypeId)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetResultGateKelvin(state, isKelvinOk, testTypeId);
                    else
                        Cache.Gate.SetResultKelvin(state, isKelvinOk);
                });
        }

        public void AddGateResistanceEvent(DeviceState state, float resistance, long testTypeId)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetResultGateResistance(state, resistance, testTypeId);
                    else
                        Cache.Gate.SetResultResistance(state, resistance);
                });
        }

        public void AddGateGateEvent(DeviceState state, float igt, float vgt, IList<short> arrayI, IList<short> arrayV, long testTypeId)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetResultGateGate(state, igt, vgt, arrayI, arrayV, testTypeId);
                    else
                        Cache.Gate.SetResultGT(state, igt, vgt, arrayI, arrayV);
                });
        }

        public void AddGateIhEvent(DeviceState state, float ih, IList<short> array, long testTypeId)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetResultGateIh(state, ih, array, testTypeId);
                    else
                        Cache.Gate.SetResultIh(state, ih, array);
                });
        }

        public void AddGateIlEvent(DeviceState state, float il, long testTypeId)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetResultGateIl(state, il, testTypeId);
                    else
                        Cache.Gate.SetResultIl(state, il);
                });
        }

        public void AddGateWarningEvent(Types.Gate.HWWarningReason Warning)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetGateWarning(Warning);
                    else
                        Cache.Gate.SetWarning(Warning);
                });
        }

        public void AddGateFaultEvent(Types.Gate.HWFaultReason Fault)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetGateFault(Fault);
                    else
                        Cache.Gate.SetFault(Fault);
                });
        }

        public void AddGateProblemEvent(Types.Gate.HWProblemReason Problem)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetGateProblem(Problem);
                    else
                        Cache.Gate.SetProblem(Problem);
                });
        }

        public void AddSLEvent(DeviceState state, Types.SL.TestResults result)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (!result.IsSelftest)
                    {
                        if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                            Cache.UserTest.SetResultSl(state, result);
                        else
                            Cache.SL.SetResultVtm(state, result);
                    }
                    else
                        Cache.Selftest.SetResult(state, result);
                });
        }

        public void AddSLWarningEvent(Types.SL.HWWarningReason Warning)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetSLWarning(Warning);
                    else if (Cache.Main.mainFrame.Content.Equals(Cache.Selftest))
                        Cache.Selftest.SetWarning(Warning);
                    else
                        Cache.SL.SetWarning(Warning);
                });
        }

        public void AddSLFaultEvent(Types.SL.HWFaultReason Fault)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetSLFault(Fault);
                    else if (Cache.Main.mainFrame.Content.Equals(Cache.Selftest))
                        Cache.Selftest.SetFault(Fault);
                    else
                        Cache.SL.SetFault(Fault);
                });
        }

        public void AddSLProblemEvent(Types.SL.HWProblemReason Problem)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetSLProblem(Problem);
                    else if (Cache.Main.mainFrame.Content.Equals(Cache.Selftest))
                        Cache.Selftest.SetProblem(Problem);
                    else
                        Cache.SL.SetProblem(Problem);
                });
        }

        public void AddBvtAllEvent(DeviceState State)
        {
            m_ActionQueue.Enqueue(delegate
            {
                Cache.UserTest.SetResultBvtAll(State);
            });
        }

        public void AddBvtDirectEvent(DeviceState State, Types.BVT.TestResults Result)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetResultBvtDirect(State, Result);
                    else
                        Cache.Bvt.SetResultBvtDirect(State, Result);
                });
        }

        public void AddBvtReverseEvent(DeviceState State, Types.BVT.TestResults Result)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetResultReverseBvt(State, Result);
                    else
                        Cache.Bvt.SetResultReverseBvt(State, Result);
                });
        }

        public void AddBvtWarningEvent(Types.BVT.HWWarningReason Warning)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetBvtWarning(Warning);
                    else
                        Cache.Bvt.SetWarning(Warning);
                });
        }

        public void AddBvtFaultEvent(Types.BVT.HWFaultReason Fault)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetBvtFault(Fault);
                    else
                        Cache.Bvt.SetFault(Fault);
                });
        }

        public void AddBvtProblemEvent(Types.BVT.HWProblemReason Problem)
        {
            m_ActionQueue.Enqueue(delegate
                {
                    if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                        Cache.UserTest.SetBvtProblem(Problem);
                    else
                        Cache.Bvt.SetProblem(Problem);
                });
        }

        public void AddClampingSwitchEvent(Types.Clamping.SqueezingState State, IList<float> ArrayF, IList<float> ArrayFd)
        {
            m_ActionQueue.Enqueue(delegate
            {
                Cache.Main.SetClampState(State);

                if (Cache.Main.mainFrame.Content.Equals(Cache.Clamp))
                    Cache.Clamp.SetResult(State, ArrayF, ArrayFd);
            });
        }

        public void AddClampingWarningEvent(Types.Clamping.HWWarningReason Warning)
        {
            m_ActionQueue.Enqueue(delegate
            {
                Cache.Main.SetClampWarning(Warning);

                if (Cache.Main.mainFrame.Content.Equals(Cache.Clamp))
                    Cache.Clamp.SetWarning(Warning);
            });
        }

        public void AddClampingProblemEvent(Types.Clamping.HWProblemReason Problem)
        {
            m_ActionQueue.Enqueue(delegate
            {
                if (Cache.Main.mainFrame.Content.Equals(Cache.Clamp))
                    Cache.Clamp.SetProblem(Problem);
            });
        }

        public void AddClampingFaultEvent(Types.Clamping.HWFaultReason Fault)
        {
            m_ActionQueue.Enqueue(delegate
            {
                Cache.Main.SetClampFault(Fault);

                if (Cache.Main.mainFrame.Content.Equals(Cache.Clamp))
                    Cache.Clamp.SetFault(Fault);
            });
        }

        public void AddDVdtEvent(DeviceState State, Types.dVdt.TestResults Result)
        {
            m_ActionQueue.Enqueue(delegate
            {
                if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                    Cache.UserTest.SetResultdVdt(State, Result);
                else
                    Cache.DVdt.SetResult(State, Result);
            });
        }

        public void AddDVdtWarningEvent(Types.dVdt.HWWarningReason Warning)
        {
            m_ActionQueue.Enqueue(delegate
            {
                if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                    Cache.UserTest.SetDVdtWarning(Warning);
                else
                    Cache.DVdt.SetWarning(Warning);
            });
        }

        public void AddDVdtFaultEvent(Types.dVdt.HWFaultReason Fault)
        {
            m_ActionQueue.Enqueue(delegate
            {
                if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                    Cache.UserTest.SetDVdtFault(Fault);
                else
                    Cache.DVdt.SetFault(Fault);
            });
        }

        public void AddClampingTopTempEvent(int temeprature)
        {
            m_ActionQueue.Enqueue(delegate
            {
                if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                    Cache.UserTest.SetTopTemp(temeprature);
                else
                    Cache.Clamp.SetTopTemp(temeprature);

            });
        }

        public void AddClampingBottomTempEvent(int temeprature)
        {
            m_ActionQueue.Enqueue(delegate
            {
                if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                    Cache.UserTest.SetBottomTemp(temeprature);
                else
                    Cache.Clamp.SetBottomTemp(temeprature);
            });
        }

        public void AddSctuEvent(SctuHwState state, SctuTestResults results)
        {
            m_ActionQueue.Enqueue(delegate
            {
                //if (Cache.Main.mainFrame.Content.Equals(Cache.UserTest))
                //    Cache.UserTest.SetDVdtWarning(Warning);
                //else
                Cache.SctuPage.SetResults(state, results);                
            });
        }
    }
}