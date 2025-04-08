using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using FTD2XX_NET;
using SCME.Types;
using System.Threading;

namespace SCME.UI.IO
{
    public class FtdiLibrary
    {
        private const int REQUEST_DELAY_MS = 250;

        private const string DESCRIPTION_NAME = "Dual RS232 A";
        private const byte DIR_MASK = 0x3b; //0b00111011
        private const byte B1_MASK = 0x80;  //0b10000000
        private const byte B2_MASK = 0x40;  //0b01000000
        private const byte L1_MASK = 0x10;  //0b00010000
        private const byte L2_MASK = 0x20;  //0b00100000

        private readonly static object ms_Locker = new object();
        private readonly List<bool> m_Buttons;
        private readonly List<bool> m_Leds;
        private readonly DispatcherTimer m_GreenTimer;
        private readonly DispatcherTimer m_RedTimer;
        private readonly FTDI m_MyFtdiDevice;

        private byte m_BitMode = FTDI.FT_BIT_MODES.FT_BIT_MODE_MPSSE;
        private FTDI.FT_STATUS m_FtStatus;
        private bool m_Emulation;
        private volatile bool m_Stop;

        public FtdiLibrary()
        {
            IsConnected = false;
            IsStopButtonPressed = false;
            m_Buttons = new List<bool>(2) { false, false };
            m_Leds = new List<bool>(2) { false, false };

            m_GreenTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            m_GreenTimer.Tick += GreenTimer_Tick;
            m_RedTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            m_RedTimer.Tick += RedTimer_Tick;

            m_MyFtdiDevice = new FTDI();
        }

        
        internal bool IsConnected { get; private set; }
        internal bool IsStopButtonPressed { get; private set; }

        internal DeviceConnectionState Connect(bool Emulation, out string Message)
        {
            m_Emulation = Emulation;
            DeviceConnectionState state;
            Message = "";
            m_Stop = false;

            if (m_Emulation)
            {
                IsConnected = true;
                state = DeviceConnectionState.ConnectionSuccess; 

                return state;
            }

            if (!m_MyFtdiDevice.IsOpen)
            {
                IsConnected = false;
                m_FtStatus = m_MyFtdiDevice.OpenByDescription(DESCRIPTION_NAME);

                if (m_FtStatus == FTDI.FT_STATUS.FT_OK)
                    m_FtStatus = m_MyFtdiDevice.SetBaudRate(115200);
                if (m_FtStatus == FTDI.FT_STATUS.FT_OK)
                    m_FtStatus = m_MyFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
                if (m_FtStatus == FTDI.FT_STATUS.FT_OK)
                    m_FtStatus = m_MyFtdiDevice.SetBitMode(DIR_MASK, FTDI.FT_BIT_MODES.FT_BIT_MODE_MPSSE);

                if (m_FtStatus == FTDI.FT_STATUS.FT_OK)
                {
                    LedRedSwitch(false);
                    LedGreenSwitch(false);
                    IsConnected = true;

                    m_FtStatus = m_MyFtdiDevice.GetPinStates(ref m_BitMode);
                    
                    if (m_FtStatus == FTDI.FT_STATUS.FT_OK)
                    {
                        if ((m_BitMode & B2_MASK) == 0 && !m_Buttons[1])
                        {
                            m_Buttons[1] = true;
                            IsStopButtonPressed = m_Buttons[1];
                            Cache.Net.CallbackManager.GatewayButtonPressHandler(ComplexButtons.ButtonStopFTDI, m_Buttons[1]);
                        }
                        else if ((m_BitMode & B2_MASK) != 0 && m_Buttons[1])
                        {
                            m_Buttons[1] = false;
                            IsStopButtonPressed = m_Buttons[1];
                            Cache.Net.CallbackManager.GatewayButtonPressHandler(ComplexButtons.ButtonStopFTDI, m_Buttons[1]);
                        }
                    }
                    state = DeviceConnectionState.ConnectionSuccess;

                    ThreadPool.QueueUserWorkItem(PoolingRoutine);
                }
                else
                {
                    m_MyFtdiDevice.Close();
                    Message = "FT status " + m_FtStatus;
                    state = DeviceConnectionState.ConnectionFailed;
                }
            }
            else
                state = DeviceConnectionState.ConnectionSuccess;

            return state;
        }

        internal void Disconnect()
        {
            IsConnected = false;

            if (m_Emulation)
                return;

            if (m_MyFtdiDevice != null && m_MyFtdiDevice.IsOpen)
            {
                m_Stop = true;
                
                ProcessLeds(L1_MASK);
                ProcessLeds(L2_MASK);
                m_MyFtdiDevice.Close();
            }
        }

        internal void LedRedSwitch(bool Enable)
        {
            if (m_Emulation)
                return;

            if (m_RedTimer.IsEnabled)
                m_RedTimer.Stop();
            m_Leds[0] = Enable;
            LedWrite();
        }

        internal void LedRedToggle()
        {
            if (m_Emulation)
                return;

            if (m_RedTimer.IsEnabled)
                m_RedTimer.Stop();
            m_Leds[0] = !m_Leds[0];
            LedWrite();
        }

        internal void LedRedBlinkStart()
        {
            if (m_Emulation)
                return;

            m_RedTimer.Start();
        }

        internal void LedRedBlinkStop()
        {
            if (m_Emulation) 
                return;

            m_RedTimer.Stop();
        }

        private void RedTimer_Tick(object Sender, EventArgs E)
        {
            m_Leds[0] = !m_Leds[0];
            LedWrite();
        }

        internal void LedGreenSwitch(bool Enable)
        {
            if (m_Emulation)
                return;

            if (m_GreenTimer.IsEnabled)
                m_GreenTimer.Stop();
            m_Leds[1] = Enable;
            LedWrite();
        }

        internal void LedGreenToggle()
        {
            if (m_Emulation)
                return;

            if (m_GreenTimer.IsEnabled)
                m_GreenTimer.Stop();
            m_Leds[1] = !m_Leds[1];
            LedWrite();
        }

        internal void LedGreenBlinkStart()
        {
            if (m_Emulation) 
                return;

            m_GreenTimer.Start();
        }

        internal void LedGreenBlinkStop()
        {
            if (m_Emulation)
                return;

            m_GreenTimer.Stop();
        }

        private void GreenTimer_Tick(object Sender, EventArgs E)
        {
            m_Leds[1] = !m_Leds[1];
            LedWrite();
        }
        
        private void PoolingRoutine(object Dummy)
        {
            try
            {
                while (!m_Stop)
                {
                    lock (ms_Locker)
                    {
                        m_FtStatus = m_MyFtdiDevice.GetPinStates(ref m_BitMode);

                        if (m_FtStatus == FTDI.FT_STATUS.FT_OK)
                            ProcessButtons();
                        else
                            throw new Exception("FT status " + m_FtStatus);
                    }

                    Thread.Sleep(REQUEST_DELAY_MS);
                }
            }
            catch (Exception ex)
            {
                Cache.Net.CallbackManager.ExceptionHandler(ComplexParts.FTDI, ex.Message);
            }
        }

        private void ProcessButtons()
        {
            if ((m_BitMode & B2_MASK) == 0 && !m_Buttons[1])
            {
                m_Buttons[1] = true; 
                IsStopButtonPressed = m_Buttons[1];
                Cache.Net.CallbackManager.GatewayButtonPressHandler(ComplexButtons.ButtonStopFTDI, m_Buttons[1]);
            }
            else if ((m_BitMode & B2_MASK) != 0 && m_Buttons[1])
            {
                m_Buttons[1] = false;
                IsStopButtonPressed = m_Buttons[1];
                Cache.Net.CallbackManager.GatewayButtonPressHandler(ComplexButtons.ButtonStopFTDI, m_Buttons[1]);
            }

            if ((m_BitMode & B1_MASK) == 0 && !m_Buttons[0])
            {
                m_Buttons[0] = true;
                Cache.Net.CallbackManager.GatewayButtonPressHandler(ComplexButtons.ButtonStartFTDI, m_Buttons[0]);
            }
            else if ((m_BitMode & B1_MASK) != 0 && m_Buttons[0])
            {
                m_Buttons[0] = false;
                Cache.Net.CallbackManager.GatewayButtonPressHandler(ComplexButtons.ButtonStartFTDI, m_Buttons[0]);
            }
        }

        private void LedWrite()
        {
            lock (ms_Locker)
            {
                m_FtStatus = m_MyFtdiDevice.GetPinStates(ref m_BitMode);

                if (m_FtStatus == FTDI.FT_STATUS.FT_OK)
                    ProcessLeds(m_BitMode);
                else
                    m_MyFtdiDevice.Close();
            }
        }

        private void ProcessLeds(byte BMode)
        {
            if ((BMode & L1_MASK) == 0 && m_Leds[0])
            {
                m_Leds[0] = true;
                WriteLeds(BMode, L1_MASK, true);
            }
            else if ((BMode & L1_MASK) != 0 && !m_Leds[0])
            {
                m_Leds[0] = false;
                WriteLeds(BMode, L1_MASK, false);
            }

            if ((BMode & L2_MASK) == 0 && m_Leds[1])
            {
                m_Leds[1] = true;
                WriteLeds(BMode, L2_MASK, true);
            }
            else if ((BMode & L2_MASK) != 0 && !m_Leds[1])
            {
                m_Leds[1] = false;
                WriteLeds(BMode, L2_MASK, false);
            }
        }

        private void WriteLeds(byte BMode, byte Mask, bool Enabled)
        {
            unchecked
            {
                if (Enabled)
                    BMode |= Mask;
                else
                    BMode &= (byte)~Mask;
            }

            var data = new byte[] { 0x80, BMode, 0x30 };
            uint bytesWritten = 0;
            m_FtStatus = m_MyFtdiDevice.Write(data, data.Length, ref bytesWritten);
        }
    }
}
