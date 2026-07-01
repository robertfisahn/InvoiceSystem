using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Infrastructure.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int ThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            return await next();
        }
        finally
        {
            timer.Stop();
            var elapsedMs = timer.ElapsedMilliseconds;

            if (elapsedMs > ThresholdMs)
            {
                var requestName = typeof(TRequest).Name;
                logger.LogWarning("Wolne zapytanie: Handler {RequestName} wykonał się w {ElapsedMs}ms.", requestName, elapsedMs);
            }
        }
    }
}
