using System.Threading;

namespace tiantang_auto_harvest
{
    public static class CancellationTokenHelper
    {
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public static CancellationToken GetCancellationToken() => _cancellationTokenSource.Token;
    }
}
