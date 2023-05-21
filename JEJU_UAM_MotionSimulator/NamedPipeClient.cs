using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;

namespace JEJU_UAM_MotionSimulator
{
    public class NamedPipeClient
    {
        private string pipeName;
        private PipeDirection pipeDirection;
        private PipeOptions pipeOption;
        private TokenImpersonationLevel tokenImpersonationLevel;

        private NamedPipeClientStream pipeClientStream;

        public NamedPipeClient(string pipeName, PipeDirection pipeDirection = PipeDirection.InOut, PipeOptions pipeOption = PipeOptions.None, TokenImpersonationLevel tokenImpersonationLeve = TokenImpersonationLevel.Impersonation)
        {
            this.pipeName = pipeName;
            this.pipeDirection = pipeDirection;
            this.pipeOption = pipeOption;
            this.tokenImpersonationLevel= tokenImpersonationLeve;
        }

        public bool isConnected()
        {
            return pipeClientStream.IsConnected;
        }

        public void ClientOpen()
        {
            pipeClientStream = new NamedPipeClientStream(".", pipeName, pipeDirection, pipeOption,
                tokenImpersonationLevel);

            Console.WriteLine("Named pipe client : Connecting to server....");
            pipeClientStream.Connect();
            
        }

        public void SendMessage(string message)
        {
            if (pipeClientStream.IsConnected)
            {
                StreamString streamString = new StreamString(pipeClientStream);
                int reult = streamString.WriteString(message);

                if (reult == -1)
                {
                    pipeClientStream.Close();
                }
            }   
        }

        public void ClientClose()
        {
            pipeClientStream.Close();
        }
    }
}
