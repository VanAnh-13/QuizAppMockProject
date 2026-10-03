using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.Services.Authentication;

namespace Quizapp.Infrastructure.Messaging;

public sealed class PasswordResetWorker(
    IServiceScopeFactory scopes,
    ILogger<PasswordResetWorker> logger) : BackgroundService, IPasswordResetQueue
{
    // ponytail: queued email is lost on restart; use durable delivery if guaranteed sending becomes necessary.
    private readonly Channel<PasswordResetEmail> _messages = Channel.CreateBounded<PasswordResetEmail>(
        new BoundedChannelOptions(PasswordResetOptions.QueueCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

    public bool TryEnqueue(PasswordResetEmail message)
    {
        if (_messages.Writer.TryWrite(message))
            return true;

        logger.LogWarning("Password reset email queue is full.");
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _messages.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                await scope.ServiceProvider.GetRequiredService<IAuthService>()
                    .SendPasswordResetEmailAsync(message, timeout.Token);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                // SMTP exceptions may contain credentials, addresses or message contents.
                logger.LogWarning("Password reset email delivery failed. A new request may be submitted.");
            }
        }
    }
}
