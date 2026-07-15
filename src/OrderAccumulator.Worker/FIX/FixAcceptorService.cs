using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderAccumulator.Worker.FIX;

/// <summary>
/// FIX Acceptor service - hosts the FIX server and listens for incoming connections
/// </summary>
public class FixAcceptorService : IHostedService
{
    private readonly FixOrderListener _listener;
    private readonly ILogger<FixAcceptorService> _logger;
    private ThreadedSocketAcceptor? _acceptor;

    public FixAcceptorService(FixOrderListener listener, ILogger<FixAcceptorService> logger)
    {
        _listener = listener;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting FIX Acceptor on port 9000");

            var settings = new SessionSettings("acceptor.cfg");
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new FileLogFactory(settings);

            _acceptor = new ThreadedSocketAcceptor(_listener, storeFactory, settings, logFactory);
            _acceptor.Start();

            _logger.LogInformation("FIX Acceptor started successfully, listening on port 9000");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FIX Acceptor");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_acceptor != null)
            {
                _acceptor.Stop();
                _logger.LogInformation("FIX Acceptor stopped");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping FIX Acceptor");
        }
    }
}
