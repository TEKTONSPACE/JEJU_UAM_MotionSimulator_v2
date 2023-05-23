using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Web;
using InnoMotion;
using InnoMotion.Controller_IMotion;
using InnoMotion.Controller_InnoML;
using InnoMotion.Types;
using System.Diagnostics;

namespace JEJU_UAM_MotionSimulator
{
    class MainProgram
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0; // 콘솔 숨기기
        const int SW_SHOW = 1; // 콘솔 보이기

        private static bool isNamedPipeConnected = false;
        private static bool isFinalized = false;

        private static MotionSimulatorDevicesSetting motionSimulatorSetting;
        private static MotionDataPlayer motionDataPlayer;
        private static NamedPipeStreamer namedPipeStreamer;

        public MainProgram()
        {
            InnoML.imSetLogFunction(debugCallback);

            //xml에서 ip 정보를 받아와 각각의 장치 초기화
            motionSimulatorSetting = new MotionSimulatorDevicesSetting();

            //xml에서 csv파일 경로를 받아와 각각의 모션 데이터 로드
            motionDataPlayer = new MotionDataPlayer();

            //Named Pipe stream 생성
            namedPipeStreamer = new NamedPipeStreamer();
        }

        public static IMotionDebugCallback debugCallback = new IMotionDebugCallback(MainProgram.Callback_Debug);
        public static void Callback_Debug(UInt32 id, string message, IntPtr userdata)
        {
            Console.WriteLine(message);
            return;
        }

        public static void SetMotionData(int videoIndex)
        {
            motionDataPlayer.SetCurrentMotionData(videoIndex, motionSimulatorSetting.motionSimulatorDevices[0]);
            motionSimulatorSetting.OnAllDevice();
            
            for(int i =0; i< motionSimulatorSetting.motionSimulatorDevices.Length; i++)
            {
                InnoML.imSetContext(motionSimulatorSetting.motionSimulatorDevices[i].ImContext);
                while (true)
                {
                    IM_DIAGNOSTIC_AXIS_INFO[] descAxis = new IM_DIAGNOSTIC_AXIS_INFO[MotionTypes.IM_FORMAT_CHANNELS_DEFAULT];
                    int error = InnoML.imGetDiagnostic(descAxis, MotionTypes.IM_FORMAT_CHANNELS_DEFAULT);

                    if (error == 0)
                    {
                        if (descAxis[0].bBusy != 0 || descAxis[1].bBusy != 0 || descAxis[2].bBusy != 0)
                        {
                            //Console.WriteLine($"Device {i} Axis Check... : 0 - {descAxis[0].bBusy} 1 - {descAxis[1].bBusy} 2 - {descAxis[2].bBusy}");
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            Console.WriteLine($"Device {i} Axis Check Success : 0 - {descAxis[0].bBusy} 1 - {descAxis[1].bBusy} 2 - {descAxis[2].bBusy}");

                            if (i == motionSimulatorSetting.motionSimulatorDevices.Length - 1)
                            {
                                Thread.Sleep(1000);
                                namedPipeStreamer.SendMotionReadyMessage();
                            }

                            break;
                        }

                    }
                    else
                    {
                        Console.WriteLine($"Device Conntection Fail : error code {error}");
                        return;
                    }
                }
            }
        }

        public static void OnExit(object sender, EventArgs e)
        {
            if(!isFinalized)
            {
                motionDataPlayer.StopMotionData();
                motionSimulatorSetting.OffAllDevice();

                for (int i = 0; i < motionSimulatorSetting.motionSimulatorDevices.Length; i++)
                {
                    InnoML.imSetContext(motionSimulatorSetting.motionSimulatorDevices[i].ImContext);
                    while (true)
                    {
                        IM_DIAGNOSTIC_AXIS_INFO[] descAxis = new IM_DIAGNOSTIC_AXIS_INFO[MotionTypes.IM_FORMAT_CHANNELS_DEFAULT];
                        int error = InnoML.imGetDiagnostic(descAxis, MotionTypes.IM_FORMAT_CHANNELS_DEFAULT);

                        if (error == 0)
                        {
                            if (descAxis[0].bBusy != 0 || descAxis[1].bBusy != 0 || descAxis[2].bBusy != 0)
                            {
                                //Console.WriteLine($"Device {i} Axis Check... : 0 - {descAxis[0].bBusy} 1 - {descAxis[1].bBusy} 2 - {descAxis[2].bBusy}");

                                Thread.Sleep(1000);
                            }
                            else
                            {
                                Console.WriteLine($"Device {i} Axis Check Success : 0 - {descAxis[0].bBusy} 1 - {descAxis[1].bBusy} 2 - {descAxis[2].bBusy}");

                                break;
                            }

                        }
                        else
                        {
                            Console.WriteLine($"Device Conntection Fail : error code {error}");
                            break;
                        }
                    }
                }

                motionDataPlayer.FinalizeAllMotionData();
                motionSimulatorSetting.FinalizeAllDevice();
            }
        }

        public static void OnNamedPipeDisconnect()
        {
            isNamedPipeConnected = false;
        }

        public static void OnStopMotionData()
        {
            motionDataPlayer.StopMotionData();
            Thread.Sleep(500);
            motionSimulatorSetting.OffAllDevice();
        }

        public static void OnCheckDeviceConnection()
        {
            namedPipeStreamer.SendDeviceConnectionInfo(motionSimulatorSetting.CheckDevicesConnection());
        }

        public static void OnPlayMotionData()
        {
            motionSimulatorSetting.OnAllDevice();
            motionDataPlayer.PlayMotionData();
        }

        private static void NamedPipeThread()
        {
            namedPipeStreamer.OnCheckDeviceConnection += OnCheckDeviceConnection;
            namedPipeStreamer.OnVideoSelected += SetMotionData;
            namedPipeStreamer.OnVideoPlay += OnPlayMotionData;
            namedPipeStreamer.OnVideoEnd += OnStopMotionData;
            namedPipeStreamer.OnDisconnect += OnNamedPipeDisconnect;
            namedPipeStreamer.OnCurrentVideoTime += (millisecond) => motionDataPlayer.PauseMotion(millisecond);

            while(isNamedPipeConnected)
            {
                Thread.Sleep(1000);
                if (!namedPipeStreamer.IsConnected())
                {
                    Console.WriteLine("named pipe is disconnect without message");
                    break;
                }
            }
        }

        static void Main(string[] args)
        {

            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityBoostEnabled = true;
            currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
            
            XmlHandler xmlHandler = new XmlHandler(Directory.GetCurrentDirectory() + "/DeviceInfo.xml");
            string isVisible = xmlHandler.ReadXmlNode("SystemSetting", "Console", "isVisible");

            if (isVisible == null)
            {
                 xmlHandler.WriteXmlNode("0", "SystemSetting", "Console", "isVisible");
                 isVisible = "0";
            }

            if(isVisible =="0")
            {
                var hWindow = GetConsoleWindow();
                ShowWindow(hWindow, SW_HIDE);
            }

            MainProgram mainProgram = new MainProgram();

            AppDomain.CurrentDomain.ProcessExit += OnExit;

            //Named pipe 통신 시작
            namedPipeStreamer.OpenNamedPipe();
            isNamedPipeConnected = true;

            Thread namedPipeThread = new Thread(NamedPipeThread);
            namedPipeThread.Start();
            namedPipeThread.Join();

            motionDataPlayer.StopMotionData();
            motionSimulatorSetting.OffAllDevice();

            for (int i = 0; i < motionSimulatorSetting.motionSimulatorDevices.Length; i++)
            {
                InnoML.imSetContext(motionSimulatorSetting.motionSimulatorDevices[i].ImContext);
                while (true)
                {
                    IM_DIAGNOSTIC_AXIS_INFO[] descAxis = new IM_DIAGNOSTIC_AXIS_INFO[MotionTypes.IM_FORMAT_CHANNELS_DEFAULT];
                    int error = InnoML.imGetDiagnostic(descAxis, MotionTypes.IM_FORMAT_CHANNELS_DEFAULT);

                    if (error == 0)
                    {
                        if (descAxis[0].bBusy != 0 || descAxis[1].bBusy != 0 || descAxis[2].bBusy != 0)
                        {
                            //Console.WriteLine($"Device {i} Axis Check... : 0 - {descAxis[0].bBusy} 1 - {descAxis[1].bBusy} 2 - {descAxis[2].bBusy}");
                            
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            Console.WriteLine($"Device {i} Axis Check Success : 0 - {descAxis[0].bBusy} 1 - {descAxis[1].bBusy} 2 - {descAxis[2].bBusy}");

                            break;
                        }

                    }
                    else
                    {
                        Console.WriteLine($"Device Conntection Fail : error code {error}");
                        break;
                    }
                }
            }

            motionDataPlayer.FinalizeAllMotionData();
            motionSimulatorSetting.FinalizeAllDevice();


            isFinalized = true;
            //Console.ReadLine();
        }
    }
}
