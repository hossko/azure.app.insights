using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Demo.Worker
{
    public class ProcessServiceBusMessage
    {
        public ProcessServiceBusMessage()
        {

        }

        [FunctionName("ProcessServiceBusMessage")]
        public void Run(
            [ServiceBusTrigger("sbq-demo", Connection = "ServiceBus")]Message message,
            ILogger log, ExecutionContext context)
        {
            using var activity = new System.Diagnostics.Activity("ServiceBusProcessor.ProcessMessage");
         


            var msg = System.Text.Encoding.UTF8.GetString(message.Body);
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {msg}");
         
        }
    }
}
