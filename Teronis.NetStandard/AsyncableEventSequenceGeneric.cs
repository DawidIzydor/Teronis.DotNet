﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Teronis
{
    /// <summary>
    /// This class can coordinate the invocation order of async events.
    /// <para/>
    /// In begin of the event method you may use one of the following methods:
    /// [<see cref="RegisterDependency(KeyType)"/> or <see cref="RegisterDependency(KeyType, out TaskCompletionSource)"/>], and [<see cref="TryAwaitDependency(KeyType[])"/>].
    /// <para/>
    /// After the event handler invocation:
    /// [<see cref="FinishDependenciesAsync"/>].
    /// </summary>
    /// <typeparam name="KeyType"></typeparam>
    public class AsyncableEventSequence<KeyType> : IDisposable
    {
        public AsyncableEventSequenceStatus Status { get; private set; }
        public IEqualityComparer<KeyType> EqualityComparer { get; protected set; }
        public bool IsDisposed { get; private set; }

        private Dictionary<KeyType, List<TaskCompletionSource>> tcsDependencies;
        private TaskCompletionSource tcsRegistrationPhaseEnd;
        private SemaphoreSlim finishDependenciesAsyncLocker;
        private Task finishDependenciesTask;

        public AsyncableEventSequence(IEqualityComparer<KeyType> equalityComparer)
        {
            Status = AsyncableEventSequenceStatus.Created;
            EqualityComparer = equalityComparer ?? throw new ArgumentException(nameof(equalityComparer));
            tcsDependencies = new Dictionary<KeyType, List<TaskCompletionSource>>(EqualityComparer);
            tcsRegistrationPhaseEnd = new TaskCompletionSource();
            finishDependenciesAsyncLocker = new SemaphoreSlim(1, 1);
        }

        public AsyncableEventSequence()
            : this(EqualityComparer<KeyType>.Default) { }

        /// <summary>
        /// Checks the dispose status by checking the <see cref="IsDisposed"/> object, if it is true means that object
        /// has been disposed and throw ObjectDisposedException
        /// </summary>
        private void checkDispose()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(null, "Object has been already disposed");
        }

        public TaskCompletionSource RegisterDependency(KeyType key)
        {
            if (Status != AsyncableEventSequenceStatus.Created)
                throw new InvalidOperationException("Depenendecies are already awaited or are being awaited");

            if (!tcsDependencies.TryGetValue(key, out var tcsList)) {
                tcsList = new List<TaskCompletionSource>();
                tcsDependencies.Add(key, tcsList);
            }

            var tcs = new TaskCompletionSource();
            tcsList.Add(tcs);
            return tcs;
        }

        public void RegisterDependency(KeyType key, out TaskCompletionSource rcs)
            => rcs = RegisterDependency(key);

        private IEnumerable<TaskCompletionSource> getAllTaskCompletionSources()
            => tcsDependencies.Values.SelectMany(x => x);

        /// <summary>
        /// This method guarantees, that all dependencies are finished before true gets returned. 
        /// If one of the awaiting dependencies are failing false gets returned. Even when none
        /// keys are provided, this function awaits the task, that gets finished when 
        /// </summary>
        public async Task<bool> TryAwaitDependency(params KeyType[] keys)
        {
            if (Status == AsyncableEventSequenceStatus.Finished)
                return true;
            else if (Status == AsyncableEventSequenceStatus.Canceled)
                return false;
            else {
                checkDispose();
                await tcsRegistrationPhaseEnd.Task;
                IEnumerable<Task> awaitableDependencies;

                if (!(keys == null || keys.Length == 0)) {
                    // These are all dependencies that are referenced to passed keys
                    awaitableDependencies = from tcs in (from key in keys
                                                         where tcsDependencies.ContainsKey(key)
                                                         select tcsDependencies[key]).SelectMany(x => x)
                                            select tcs.Task;

                    // Finally we want to add them to the awaitable tasks
                    //awaitableTasks.AddRange(awaitableDependencies);
                } else
                    awaitableDependencies = Enumerable.Empty<Task>();

                try {
                    await Task.WhenAll(awaitableDependencies).ConfigureAwait(false);
                    return true;
                } catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// This method awaits all registered dependencies and throws the first occuring exception.
        /// You may call this after the event handler invocation.
        /// </summary>
        /// <exception cref="TaskCanceledException">Thrown when one of the dependency get canceled</exception>
        /// /// <exception cref="InvalidOperationException">Thrown when this function was already called</exception>
        public async Task FinishDependenciesAsync()
        {
            await finishDependenciesAsyncLocker.WaitAsync();

            if (Status == AsyncableEventSequenceStatus.Created) {
                // We want to finish the registration phase, after all invoked event handler may have registered their dependencies
                tcsRegistrationPhaseEnd.SetResult();

                var tasks = getAllTaskCompletionSources()
                    .Select(x => x.Task);

                finishDependenciesTask = Task.WhenAll(tasks);
                Status = AsyncableEventSequenceStatus.Running;
                finishDependenciesAsyncLocker.Release();

                try {
                    // Then we await all dependencies
                    await finishDependenciesTask;
                    Status = AsyncableEventSequenceStatus.Finished;
                } catch {
                    // Try to cancel all dependencies
                    foreach (var tcs in getAllTaskCompletionSources())
                        tcs.TrySetCanceled();

                    Status = AsyncableEventSequenceStatus.Canceled;
                    throw;
                } finally {
                    Dispose();
                }
            } else {
                finishDependenciesAsyncLocker.Release();
                await finishDependenciesTask;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed) {
                if (disposing) {
                    finishDependenciesAsyncLocker.Dispose();
                    finishDependenciesAsyncLocker = null;
                }

                EqualityComparer = null;
                tcsDependencies = null;
                tcsRegistrationPhaseEnd = null;
                IsDisposed = true;
            }
        }

        ~AsyncableEventSequence() 
            => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}