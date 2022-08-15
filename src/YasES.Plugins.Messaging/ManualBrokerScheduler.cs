using System;
using System.Threading;

namespace YasES.Plugins.Messaging
{
    /// <summary>
    /// This scheduler allows to execute <see cref="IBrokerCommands"/>
    /// manually by calling the correponding methods from code. While
    /// this was created for easy testing, it might be useful in special
    /// circumstances.
    /// </summary>
    public class ManualBrokerScheduler : IBrokerCommands
    {
        private readonly IBrokerCommands _scheduling;

        public ManualBrokerScheduler(IBrokerCommands scheduling)
        {
            _scheduling = scheduling;
        }

        public bool WaitForMessages(TimeSpan timeout, CancellationToken token = default)
        {
            return _scheduling.WaitForMessages(timeout, token);
        }

        public void RemoveEmptyChannels()
        {
            _scheduling.RemoveEmptyChannels();
        }

        /// <summary>
        /// Executes a single round message sending.
        /// Returns the number of messages which has been delivered.
        /// </summary>
        public int CallSubscribers()
        {
            return _scheduling.CallSubscribers();
        }

        /// <summary>
        /// Executes all subscripbers until
        /// all queues are empty. Returns the number
        /// of messages which has been sent in total.
        /// </summary>
        public int Drain()
        {
            int result = 0;
            while (true)
            {
                int callsThisRound = _scheduling.CallSubscribers();
                result += callsThisRound;
                if (callsThisRound == 0)
                    break;
            }
            return result;
        }
    }
}
