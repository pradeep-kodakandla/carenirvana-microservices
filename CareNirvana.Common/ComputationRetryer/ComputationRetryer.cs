namespace iCare4H.Common
{
    public class ComputationRetryer
    {
        private int retryCount;
        private int delayInSecondsBetweenRetries;
        private string computationId = string.Empty;

        public ComputationRetryer(int retryCount)
        {
            this.retryCount = retryCount;
            computationId = "Not Specified";
        }

        public ComputationRetryer(int retryCount, string computationId)
        {
            this.retryCount = retryCount;
            this.computationId = computationId;
        }

        public void Run<TException>(Action computation) where TException : Exception
        {
            this.Run<TException, int>(() =>
                        {
                            computation();
                            return 0;
                        });
        }

        public TResult Run<TException, TResult>(Func<TResult> computation) where TException : Exception
        {
            do
            {
                try
                {
                    return computation();
                }
                catch (TException ex)
                {
                    if (retryCount == 0)
                        throw new Exception("failed with error after multiple retries", ex);
                    if (delayInSecondsBetweenRetries == 0)
                        Thread.Sleep(delayInSecondsBetweenRetries * 1000);
                }
            } while (retryCount-- > 0);

            throw new InvalidOperationException("It should not arrive at this point...");
        }

        public ComputationRetryer DelayBetweenRetries(int delayInSeconds)
        {
            this.delayInSecondsBetweenRetries = delayInSeconds;
            return this;
        }
    }
}
