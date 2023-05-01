using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace MyFunctionApp
{
    public static class ProcessServiceBusMessage
    {
        [FunctionName("ProcessServiceBusMessage")]
        public static void Run(
            [ServiceBusTrigger("sbq-demo", Connection = "ServiceBus")]Message message,
            ILogger log, ExecutionContext context)
        {
            var msg = System.Text.Encoding.UTF8.GetString(message.Body);
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {msg}");
        }
    }
}
