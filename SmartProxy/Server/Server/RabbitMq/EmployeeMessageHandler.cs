using DataLayer.Entities;
using Server.Repositories;

namespace Server.RabbitMq
{
    public class EmployeeMessageHandler
    {
        private readonly IRabbitMQService<Employee> _rabbitService;
        private readonly ILogger<EmployeeMessageHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public EmployeeMessageHandler(
            IRabbitMQService<Employee> rabbitService,
            ILogger<EmployeeMessageHandler> logger,
            IServiceScopeFactory scopeFactory)
        {
            _rabbitService = rabbitService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync()
        {
            await _rabbitService.StartConsumer(async message =>
            {
                using var scope = _scopeFactory.CreateScope();
                var employeeRepository = scope.ServiceProvider.GetRequiredService<IRepository<Employee>>();

                var employee = message.Payload;
           
                switch (message.MessageType)
                {
                    case MessageType.Upsert:
                        if (employee == null)
                        {
                            _logger.LogInformation("The employee data sent to RabbitMQ is null.");
                            return;
                        }
                        var local = await employeeRepository.GetByIdAsync(employee.Id);
                        if (local == null || employee.LastChangedAt > local.LastChangedAt)
                        {
                            await employeeRepository.UpsertAsync(employee, employee.Id);
                            _logger.LogInformation($"Applied update for Employee {employee.Id}");
                        }
                        else
                        {
                            _logger.LogInformation($"Skipped update for Employee {employee.Id} because local is newer.");
                        }
                        break;

                    case MessageType.Delete:
                        if(message.Id == null)
                        {
                            _logger.LogInformation($"The received ID for deletion is null");
                            return;
                        }
                        await employeeRepository.DeleteAsync(message.Id);
                        _logger.LogInformation($"Deleted Employee {message.Id}");
                        break;
                }
            });
        }
    }
}
