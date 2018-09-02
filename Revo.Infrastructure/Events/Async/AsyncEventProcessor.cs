﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Revo.Core.Core;
using Revo.DataAccess.Entities;
using Revo.Infrastructure.Jobs;

namespace Revo.Infrastructure.Events.Async
{
    public class AsyncEventProcessor : IAsyncEventProcessor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Func<IAsyncEventQueueBacklogWorker> asyncEventQueueBacklogWorkerFunc;
        private readonly IAsyncEventQueueManager asyncEventQueueManager;
        private readonly IJobScheduler jobScheduler;
        private readonly IAsyncEventPipelineConfiguration asyncEventPipelineConfiguration;

        public AsyncEventProcessor(Func<IAsyncEventQueueBacklogWorker> asyncEventQueueBacklogWorkerFunc,
            IAsyncEventQueueManager asyncEventQueueManager,
            IJobScheduler jobScheduler,
            IAsyncEventPipelineConfiguration asyncEventPipelineConfiguration)
        {
            this.asyncEventQueueBacklogWorkerFunc = asyncEventQueueBacklogWorkerFunc;
            this.asyncEventQueueManager = asyncEventQueueManager;
            this.jobScheduler = jobScheduler;
            this.asyncEventPipelineConfiguration = asyncEventPipelineConfiguration;
        }

        public async Task ProcessSynchronously(IReadOnlyCollection<IAsyncEventQueueRecord> eventsToProcess)
        {
            string[] queues = eventsToProcess.Select(x => x.QueueName).Distinct().ToArray();
            List<IAsyncEventQueueRecord> remainingEvents = eventsToProcess.ToList();

            int triesLeft = asyncEventPipelineConfiguration.SyncProcessAttemptCount;
            TimeSpan sleepTime = asyncEventPipelineConfiguration.SyncProcessRetryTimeout;
            while (triesLeft > 0)
            {
                triesLeft--;
                queues = await TryRunQueues(queues);
                if (queues.Length == 0)
                {
                    break;
                }

                remainingEvents = (await asyncEventQueueManager.FindQueuedEventsAsync(remainingEvents.Select(x => x.Id).ToArray())).ToList();
                if (remainingEvents.Count == 0)
                {
                    break;
                }

                if (triesLeft > 0)
                {
                    await Sleep.Current.SleepAsync(sleepTime);
                    sleepTime = TimeSpan.FromTicks(sleepTime.Ticks * asyncEventPipelineConfiguration.SyncProcessRetryTimeoutMultiplier);
                }
            }

            if (queues.Length > 0 && remainingEvents.Count > 0)
            {
                Logger.Error(
                    $"Not able to synchronously process all event queues, about to reschedule {remainingEvents.Count} events for later async processing in {asyncEventPipelineConfiguration.AsyncRescheduleDelayAfterSyncProcessFailure.TotalSeconds:0.##} seconds");
                await EnqueueForAsyncProcessingAsync(remainingEvents, asyncEventPipelineConfiguration.AsyncRescheduleDelayAfterSyncProcessFailure);
            }
        }

        public async Task EnqueueForAsyncProcessingAsync(IReadOnlyCollection<IAsyncEventQueueRecord> eventsToProcess, TimeSpan? timeDelay)
        {
            string[] queues = eventsToProcess.Select(x => x.QueueName).Distinct().ToArray();
            var jobs = queues.Select(x => new ProcessAsyncEventsJob(x, asyncEventPipelineConfiguration.AsyncProcessAttemptCount, asyncEventPipelineConfiguration.AsyncProcessRetryTimeout));

            foreach (ProcessAsyncEventsJob job in jobs)
            {
                await jobScheduler.EnqeueJobAsync(job, timeDelay);
            }
        }

        private async Task<string[]> TryRunQueues(string[] queues)
        {
            string[] finishedQueues = await Task.WhenAll(queues.Select(x =>
                Task.Factory.StartNewWithContext(async () =>
                {
                    try
                    {
                        var asyncEventQueueBacklogWorker = asyncEventQueueBacklogWorkerFunc();
                        await asyncEventQueueBacklogWorker.RunQueueBacklogAsync(x);
                        return x;
                    }
                    catch (AsyncEventProcessingSequenceException e)
                    {
                        Logger.Debug(e,
                            $"AsyncEventProcessingSequenceException occurred during synchronous queue processing");
                        return null; //can retry
                    }
                    catch (OptimisticConcurrencyException e)
                    {
                        Logger.Debug(e, $"OptimisticConcurrencyException occurred during synchronous queue processing");
                        return null; //can retry
                    }
                })
            ));

            return queues.Except(finishedQueues).ToArray();
        }
    }
}