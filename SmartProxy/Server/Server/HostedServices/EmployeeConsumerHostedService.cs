
namespace Server.HostedServices
{
    public class EmployeeConsumerHostedService : IHostedService
    {
        private readonly ILogger<EmployeeConsumerHostedService> _logger;

        public EmployeeConsumerHostedService(
            ILogger<EmployeeConsumerHostedService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Employee Consumer Hosted Service is starting.");

 
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            try
            {
                _logger.LogInformation("Employee Consumer started successfully and is now listening for messages.");

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

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Employee Consumer Hosted Service is stopping.");
            return Task.CompletedTask;
        }
    }
}