using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Demo.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {   
        
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly Azure.Messaging.ServiceBus.ServiceBusClient _serviceBusClient;
        private readonly IConfiguration _configuration;
        
        public WeatherForecastController(ILogger<WeatherForecastController> logger, 
            System.Net.Http.HttpClient httpClient,
            Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _serviceBusClient = serviceBusClient;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<string> Get(System.Threading.CancellationToken cancellationToken)
        {
            _logger.LogInformation(2001, "TRACING DEMO: WebApp API weather forecast request forwarded");
            await using var sender = _serviceBusClient.CreateSender("sbq-demo");
            await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage("Demo Message"), cancellationToken);
            var httpClientString = _configuration.GetValue<string>("DemoServiceUrl");
            return await _httpClient.GetStringAsync(httpClientString, cancellationToken);
        }
    }
}
