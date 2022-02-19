using System;
using System.Runtime.ExceptionServices;
using LegacyWrapper.Common.Serialization;
using LegacyWrapper.ErrorHandling;
using LegacyWrapper.Interop;
using LegacyWrapper.Transport;
using PommaLabs.Thrower;

namespace LegacyWrapper.Handler
{
    internal class CallRequestHandler : ICallRequestHandler
    {
        private readonly PipeServer _pipeServer;

        public CallRequestHandler(PipeServer pipeServer)
        {
            Raise.ArgumentNullException.IfIsNull(pipeServer);

            _pipeServer = pipeServer;
        }

        /// <summary>
        /// Outsourced main method of the legacy dll wrapper.
        /// </summary>
        [HandleProcessCorruptedStateExceptions]
        public void Call()
        {
            try
            {
                ProcessMessages();
            }
            catch (Exception e)
            {
                WriteExceptionToClient(e);
            }
        }

        private void ProcessMessages()
        {
            CallData data;
            do
            {
                data = _pipeServer.ReceiveCallRequest();
                InvokeFunction(data);
            } while (data.Status != KeepAliveStatus.Close);
        }

        private void InvokeFunction(CallData callData)
        {
            CallResult callResult = UnmanagedLibraryLoader.InvokeUnmanagedFunction(callData);

            _pipeServer.SendCallResponse(callResult);
        }

        private void WriteExceptionToClient(Exception e)
        {
            string errorMessage = "An error occured while calling a library function. See the inner exception for details.";

            CallResult callResult = new CallResult();
            callResult.Exception = new LegacyWrapperException(errorMessage, e);

            _pipeServer.SendCallResponse(callResult);
        }
    }
}
