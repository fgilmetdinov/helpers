using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Noname.Client.Helpers
{
    /// <summary>
    /// Вспомогательные утилиты
    /// </summary>
    public class ExecUtils
    {
        /// <summary>
        /// Параллельная обработка коллекции
        /// </summary>
        /// <typeparam name="T">тип коллекции</typeparam>
        /// <param name="data">коллекция</param>
        /// <param name="threadCount">количество потоков</param>
        /// <param name="cancellationToken"></param>
        /// <param name="action">действие для каждой коллекции</param>
        /// <returns></returns>
        public static async Task ParallelExecAsync<T>(IEnumerable<T> data, 
            Func<T, CancellationToken, Task> action, int threadCount, CancellationToken cancellationToken)
        {
            var queue = new ConcurrentQueue<T>(data);
            if (queue.Count == 0)
            {
                return;
            }

            var taskCount = Math.Min(threadCount, queue.Count);
            var tasks = new Task[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    while (queue.TryDequeue(out var item))
                    {
                        await action(item, cancellationToken);
                    }
                }, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }
    }
}
