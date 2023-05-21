using InnoMotion.Controller_IMotion;
using InnoMotion.Controller_InnoML;
using InnoMotion.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using static JEJU_UAM_MotionSimulator.MotionDataPlayer;

namespace JEJU_UAM_MotionSimulator
{
    
    public class MotionSourcePair
    {
        public MotionSimulatorDevice motionDevice;
        public IntPtr motionSource;

        public MotionSourcePair(MotionSimulatorDevice device, IntPtr source)
        {
            this.motionDevice = device;
            this.motionSource = source;
        }
    }

    public class MotionDataPlayer
    {
        public List<MotionData> loadedMotionData;       //각 경로에 따라 로드된 모션 데이터 모음
        
        private List<string> motionDataCSVFilePath;         //로드할 CSV 파일 경로 모음
        private MotionData currentMotionData;

        private XmlHandler xmlHandler;

        private bool isPlaying;


        public MotionDataPlayer()
        {
            isPlaying = false;
            LoadAllMotionData();
        }

        public void LoadAllMotionData()
        {
            Console.WriteLine("Load All Motion Data");

            //xml로 csv 파일 경로 불러오기
            motionDataCSVFilePath = new List<string>();
            xmlHandler = new XmlHandler(Directory.GetCurrentDirectory() + "/CSVFileInfo.xml");
            motionDataCSVFilePath = xmlHandler.ReadXmlNodeList("MotionSimulator", "CSVFile");

            //xml에 등록된 path가 없는 경우
            if (motionDataCSVFilePath.Count == 0)
            {
                //영상 1
                string filePath = "./MotionData/waveform_sine.csv";
                //해당 경로에 csv 파일이 있으면 경로 저장
                if (File.Exists(filePath))
                {
                    xmlHandler.WriteXmlNode(filePath, "MotionSimulator", "CSVFile", "FilePath", true);
                    motionDataCSVFilePath.Add(filePath);
                }
                else
                {
                    Console.WriteLine($"File path {filePath} is not exist");
                }

                //영상 2
                filePath = "./MotionData/MotionData_Vib_5s.csv";

                if (File.Exists(filePath))
                {
                    xmlHandler.WriteXmlNode(filePath, "MotionSimulator", "CSVFile", "FilePath", true);
                    motionDataCSVFilePath.Add(filePath);
                }
                else
                {
                    Console.WriteLine($"File path {filePath} is not exist");
                }
            }

            loadedMotionData = new List<MotionData>();

            for (int i =0; i< motionDataCSVFilePath.Count; i++)
            {
                MotionData newMotionData = new MotionData(motionDataCSVFilePath[i]);
                newMotionData.LoadMotionData();
                loadedMotionData.Add(newMotionData);
            }
        }

        public void FinalizeAllMotionData()
        {
            Console.WriteLine("Finalize All Motion Data");
            for(int i=0; i < loadedMotionData.Count; i++)
            {
                if (loadedMotionData[i].isFileLoaded)
                {
                    loadedMotionData[i].FinalizeMotionData();
                }
            }
        }

        public void SetCurrentMotionData(int CSVFileIndex, MotionSimulatorDevice Device )
        {
            Console.WriteLine($"Set Current Motion Data {CSVFileIndex}");

            currentMotionData = loadedMotionData[CSVFileIndex];
            if (!currentMotionData.isFileLoaded)
            {
                Console.WriteLine($"CSV File {CSVFileIndex} is not Loaded");
                return;
            }
        }

        public void PlayMotionData()
        {
            InnoML.imSourcePlay(currentMotionData.motionSource);
            isPlaying = true;
            CheckDuration();
        }

        public void StopMotionData()
        {
            if(isPlaying)
            {
                InnoML.imSourceStop(currentMotionData.motionSource);
                Console.WriteLine("Stop Motion Data End");
                isPlaying = false;
            }
        }

        public void CheckDuration()
        {
            Int32 imbuffer = InnoML.imSourceGetBuffer(currentMotionData.motionSource);
            Int32 totalDuration = InnoML.imBufferGetDuration(imbuffer);

            while(InnoML.imSourceGetPosition(currentMotionData.motionSource) < totalDuration)
            {
                Console.WriteLine($"Play Time : {InnoML.imSourceGetPosition(currentMotionData.motionSource)} / {totalDuration}");
                if (!isPlaying)
                    break;
            }
        }

    }
}
