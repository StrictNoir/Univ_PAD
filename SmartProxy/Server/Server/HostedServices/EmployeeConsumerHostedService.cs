using Server.RabbitMq;

namespace Server.HostedServices
{
    public class EmployeeConsumerHostedService : BackgroundService
    {
        private readonly EmployeeMessageHandler _handler;
        private readonly ILogger<EmployeeConsumerHostedService> _logger;

        public EmployeeConsumerHostedService(
            EmployeeMessageHandler handler,
            ILogger<EmployeeConsumerHostedService> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Employee Consumer Hosted Service is starting.");

 
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            try
            {
                await _handler.StartAsync();
                _logger.LogInformation("Employee Consumer started successfully and is now listening for messages.");

       
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Employee Consumer Hosted Service is stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in Employee Consumer Hosted Service.");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Employee Consumer Hosted Service is stopping.");
            await base.StopAsync(cancellationToken);
        }
    }
}