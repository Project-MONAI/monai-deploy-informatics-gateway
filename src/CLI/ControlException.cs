using System;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ControlException : Exception
    {
        public int ErrorCode { get; }

        public ControlException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
