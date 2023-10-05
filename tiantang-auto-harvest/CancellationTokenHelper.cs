using System.Threading;
using Microsoft.Extensions.Logging;

namespace tiantang_auto_harvest
{
    public static class CancellationTokenHelper
    {
        private static readonly ILogger _logger = LoggerFactory.Create(builder => { }).CreateLogger("CancellationTokenHelper");

        private static CancellationTokenSource _cancellationTokenSource
        {
            get
            {
                var cancellationTokenSource = new CancellationTokenSource();

                cancellationTokenSource.Token.Register(() =>
                {
                    _logger.LogWarning("Cancellation token is cancelled");
                });

                return cancellationTokenSource;
            }
        }

        public static CancellationToken GetCancellationToken() => _cancellationTokenSource.Token;
    }
}
