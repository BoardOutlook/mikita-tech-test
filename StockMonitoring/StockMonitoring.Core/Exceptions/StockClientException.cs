using System;

namespace StockMonitoring.Core.Exceptions;

public class StockClientException : Exception
{
    public string Endpoint { get; }

    public StockClientException(string message, string endpoint, Exception innerException) 
        : base(message, innerException)
    {
        Endpoint = endpoint;
    }
}
