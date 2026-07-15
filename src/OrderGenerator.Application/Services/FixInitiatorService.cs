using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderGenerator.Application.Services;

public class FixInitiatorService : IHostedService
{
    private readonly FixOrderInitiator _initiator;
    private readonly ILogger<FixInitiatorService> _logger;
    private SocketInitiator? _fixInitiator;

    public FixInitiatorService(FixOrderInitiator initiator, ILogger<FixInitiatorService> logger)
    {
        _initiator = initiator;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting FIX Initiator service");

            var settings = new SessionSettings("initiator.cfg");
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new FileLogFactory(settings);

            _fixInitiator = new SocketInitiator(_initiator, storeFactory, settings, logFactory);
            _fixInitiator.Start();

            _logger.LogInformation("FIX Initiator started successfully");

            //todo: validar a necessidade ainda, b.o no composer
            await Task.Delay(5000, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FIX Initiator");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_fixInitiator != null)
            {
                _fixInitiator.Stop();
                _logger.LogInformation("FIX Initiator stopped");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping FIX Initiator");
        }
    }
}
