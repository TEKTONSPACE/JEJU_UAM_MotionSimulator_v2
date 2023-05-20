using InnoMotion.Controller_IMotion;
using InnoMotion.Controller_InnoML;
using InnoMotion.Types;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using System.Text;

namespace JEJU_UAM_MotionSimulator
{
    public class MotionData
    {
        public bool isFileLoaded;
        public Int32 motionBuffer;
        public Int32 motionSource;
        public int duration;

        private string SCVFilePath;

        public MotionData(string path)
        {
            SCVFilePath = path;
            isFileLoaded = false;
        }

        public void FinalizeMotionData()
        {
            Console.WriteLine($"Fialize {SCVFilePath}");
            InnoML.imDeleteSource(motionSource);
            InnoML.imDeleteBuffer(motionBuffer);
            isFileLoaded = false;
        }

        public void LoadMotionData()
        {
            Console.WriteLine($"Load Motion Data : {SCVFilePath}");
            motionBuffer = InnoML.imLoadBuffer(SCVFilePath);
            motionSource = InnoML.imCreateSource(motionBuffer);
            duration = InnoML.imBufferGetDuration(motionBuffer);

            isFileLoaded = true;

            Console.WriteLine($"Load Motion Data Complete : {SCVFilePath}");
        }
    }
}
