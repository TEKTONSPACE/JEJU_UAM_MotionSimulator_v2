using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace JEJU_UAM_MotionSimulator
{
    public class NamedPipeServer
    {
        public Action<string> OnReceiveMessage;

        private string pipeName;
        private PipeDirection pipeDirection;

        private NamedPipeServerStream pipeServerStream;

        public NamedPipeServer(string pipeName, PipeDirection pipeDirection = PipeDirection.InOut)
        {
            this.pipeName = pipeName;
            this.pipeDirection = pipeDirection;
        }

        public bool isConnected()
        {
            return pipeServerStream.IsConnected;
        }

        public void ServerOpen()
        {
            Console.WriteLine("Named pipe server : Waiting for client connect....");
            Thread serverThread = new Thread(ServerThread);
            serverThread.Start();
        }

        public void ServerClose()
        {
            pipeServerStream.Close();
        }

        private void ServerThread()
        {
            pipeServerStream = new NamedPipeServerStream(pipeName, pipeDirection);

            pipeServerStream.WaitForConnection();

            Console.WriteLine("Named pipe server : Client connected on server thread.");

            StreamString streamString = new StreamString(pipeServerStream);

            while(pipeServerStream.IsConnected)
            {
                string message = streamString.ReadString();
                Console.WriteLine(message);
                OnReceiveMessage?.Invoke(message);
            }
        }
    }
}
