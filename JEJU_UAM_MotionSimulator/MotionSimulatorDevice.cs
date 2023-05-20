using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using InnoMotion.Controller_IMotion;
using InnoMotion.Controller_InnoML;
using InnoMotion.Types;

namespace JEJU_UAM_MotionSimulator
{
    public class MotionSimulatorDevice
    {
        public Int32 ImContext;

        //public IntPtr iMotionInstance;                              //IMotion 모션 디바이스 객체

        public uint deviceIP;                                         //모션 디바이스 IP 끝 번호

        public MotionSimulatorDevice(uint ip)
        {
            deviceIP = ip;
            InitializeDevice();
        }

        public void InitializeDevice()
        {
            //iMotionInstance = IMotion.IMotion_Create(deviceIP, descriptionPtr);
            ImContext = InnoML.imCreateContext(0, deviceIP);
        }
    }
}
