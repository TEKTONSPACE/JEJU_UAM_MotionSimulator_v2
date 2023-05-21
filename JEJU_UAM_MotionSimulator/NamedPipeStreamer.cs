using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace JEJU_UAM_MotionSimulator
{
    public class NamedPipeStreamer
    {
        public Action<int> OnVideoSelected;
        public Action OnCheckDeviceConnection;
        public Action OnVideoPlay;
        public Action OnVideoEnd;
        public Action OnDisconnect;

        public Action<int> OnCurrentVideoTime;

        private const string serverPipeName = "JejuUAMProject-CMS-Message";
        private const string clientPipeName = "JejuUAMProject-MotionSimulator-Message";

        private const string connectSuccessMessage = "Connect_Success";
        private const string motionReadyMessage = "Motion_Ready";

        private NamedPipeClient namedPipeClient;
        private NamedPipeServer namedPipeServer;

        public NamedPipeStreamer()
        {
            namedPipeClient = new NamedPipeClient(clientPipeName);
            namedPipeServer = new NamedPipeServer(serverPipeName);
        }

        public bool IsConnected()
        {
            if (namedPipeClient.isConnected() && namedPipeServer.isConnected())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OpenNamedPipe()
        {
            namedPipeServer.ServerOpen();
            namedPipeClient.ClientOpen();

            namedPipeServer.OnReceiveMessage += OnReceiveMessage;
        }

        private void OnReceiveMessage(string message)
        {
            Console.Write($"On Receive Message : {message}");
            switch(message)
            {
                //CMS 실행 시
                case "Connection_Check":
                    namedPipeClient.SendMessage(connectSuccessMessage);
                    break;

                case "DeviceConnection_Check":
                    OnCheckDeviceConnection?.Invoke();
                    break;

                //CMS에서 영상 1번 선택
                case "CSVFile_00":
                    OnVideoSelected?.Invoke(0);
                    break;

                //CMS에서 영상 2번 선택
                case "CSVFile_01":
                    OnVideoSelected?.Invoke(1);
                    break;

                //CMS에서 영상 플레이 시
                case "Motion_Play":
                    OnVideoPlay?.Invoke();
                    break;

                //영상이 끝났을 때
                case "Motion_Stop":
                    OnVideoEnd?.Invoke();
                    break;

                //프로그램 종료 시
                case "Disconnect":
                    namedPipeServer.ServerClose();
                    namedPipeClient.ClientClose();
                    OnDisconnect?.Invoke();
                    break;

                default:
                    //현재 시간 싱크
                    string[] splitMessage = message.Split('_');
                    if (splitMessage[0] == "CurrentTime")
                    {
                        Int32 milliseconds = int.Parse(splitMessage[1]);
                        OnCurrentVideoTime?.Invoke(milliseconds);
                    }
                    break;

            }
        }

        public void SendMotionReadyMessage()
        {
            namedPipeClient.SendMessage(motionReadyMessage);
        }

        public void SendDeviceConnectionInfo(string deviceConnectionInfo)
        {
            namedPipeClient.SendMessage("DeviceConnection_"+ deviceConnectionInfo);
            //Console.WriteLine("DeviceConnection_" + deviceConnectionInfo);
        }


        
    }
}
