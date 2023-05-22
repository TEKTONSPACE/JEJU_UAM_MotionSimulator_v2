using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using InnoMotion;
using InnoMotion.Controller_IMotion;
using InnoMotion.Controller_InnoML;
using InnoMotion.Types;

namespace JEJU_UAM_MotionSimulator
{
    public class MotionSimulatorDevicesSetting
    {

        public MotionSimulatorDevice[] motionSimulatorDevices;

        private XmlHandler xmlHandler;
        private int numberOfDevices;
        private List<uint> IPUintList;

        private const int SAMPLE_CHANNELS = MotionTypes.IM_FORMAT_CHANNELS_DEFAULT; //3DOF

        private IM_DEVICE_DESC deviceDescription;          //모션 디바이스 정보 구조체
        private IntPtr descriptionPtr;

        public MotionSimulatorDevicesSetting()
        {
            InitializeAllDevice();
        }

        public void InitializeAllDevice()
        {
            //장치 개수 기본 4대
            numberOfDevices = 4;

            //xml로 기기 정보 불러오기
            xmlHandler = new XmlHandler(Directory.GetCurrentDirectory() + "/DeviceInfo.xml");
            List<string> IPList = xmlHandler.ReadXmlNodeList("MotionSimulator", "Device");
            IPUintList = new List<uint>();

            descriptionPtr = Marshal.AllocHGlobal(Marshal.SizeOf(deviceDescription));
            Marshal.StructureToPtr(deviceDescription, descriptionPtr, false);

            //기입된 ip 정보가 없는 경우 11~14로 설정
            if (IPList.Count == 0)
            {
                for (int i = 0; i < numberOfDevices; i++)
                {
                    xmlHandler.WriteXmlNode((11 + i).ToString(), "MotionSimulator", "Device", "IP", true);
                }

                IPList = xmlHandler.ReadXmlNodeList("MotionSimulator", "Device");
            }
            //ip 정보가 있는 경우 ip 갯수에 따라 기기 갯수 저장
            else
            {
                numberOfDevices = IPList.Count;
            }

            foreach (string ip in IPList)
            {
                IPUintList.Add(uint.Parse(ip));
            }

            //기기 갯수만큼 초기화
            motionSimulatorDevices = new MotionSimulatorDevice[numberOfDevices];

            for (int i = 0; i < motionSimulatorDevices.Length; i++)
            {
                motionSimulatorDevices[i] = new MotionSimulatorDevice(IPUintList[i]);
            }

        }

        public void FinalizeAllDevice()
        {
            InnoML.imSetContext(0);
            
            for(int i =0; i< motionSimulatorDevices.Length; i++)
            {
                InnoML.imDestroyContext(motionSimulatorDevices[i].ImContext);
            }

            Marshal.FreeHGlobal(descriptionPtr);

        }

        public void OnAllDevice()
        {
            InnoML.imSetContext(0);

            

            for (int i = motionSimulatorDevices.Length - 1; i >= 0; i--)
            {
                InnoML.imSetContext(motionSimulatorDevices[i].ImContext);
                if (i > 0)
                {
                    InnoML.imStart(null, default, motionSimulatorDevices[i - 1].ImContext);
                }
            }

            InnoML.imStart();
        }

        public void OffAllDevice()
        {
            for (int i = motionSimulatorDevices.Length - 1; i >= 0; i--)
            {
                InnoML.imSetContext(motionSimulatorDevices[i].ImContext);
                InnoML.imStop();
            }
        }

        public string CheckDevicesConnection()
        {
            InnoML.imSetContext(0);
            string deviceConnection = "";
            for (int i = 0; i < motionSimulatorDevices.Length; i++)
            {
                //int error = IMotion.IMotion_GetInfo(motionSimulatorDevices[i].iMotionInstance, out info);
                InnoML.imSetContext(motionSimulatorDevices[i].ImContext);
                IM_DIAGNOSTIC_AXIS_INFO[] descAxis = new IM_DIAGNOSTIC_AXIS_INFO[SAMPLE_CHANNELS];
                int isConnented = InnoML.imGetDiagnostic(descAxis, MotionTypes.IM_FORMAT_CHANNELS_DEFAULT);
                //장치 연결 성공
                if (isConnented == 0)
                {
                    deviceConnection += "1";
                }
                //장치 연결 실패
                else
                {
                    deviceConnection += "0";
                }
            }

            return deviceConnection;
        }
    }
}
