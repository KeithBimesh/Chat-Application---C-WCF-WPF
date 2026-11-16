using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharedChat
{
    [DataContract]
    public class ServiceResult
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; }

        public static ServiceResult Pass(string message = "")
        {
            return new ServiceResult
            {
                Success = true,
                Message = message
            };
        }

        public static ServiceResult Fail(string message)
        {
            return new ServiceResult
            {
                Success = false,
                Message = message
            };
        }
    }
    
}
