using System;
using System.Collections.Generic;
using System.Threading;

public class Parallel : IDisposable
{
    #region WorkerThread class
    /// <summary>
    /// Background thread definition.
    /// </summary>
    private sealed class WorkerThread : IDisposable
    {
        private Thread thread;
        private AutoResetEvent taskWaiting;
        private ManualResetEvent threadIdle;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerThread"/> class.
        /// </summary>
        public WorkerThread()
        {
            this.taskWaiting = new AutoResetEvent(false);
            this.threadIdle = new ManualResetEvent(true);
        }

        /// <summary>
        /// Wait for thread termination and close events.
        /// </summary>
        public void Terminate()
        {
            this.taskWaiting.Set();
            this.thread.Join();

            this.taskWaiting.Close();
            this.threadIdle.Close();
        }

        #region IDisposable
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">if set to <c>true</c>, dispose managed resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (this.taskWaiting != null)
                {
                    this.taskWaiting.Close();
                    this.taskWaiting = null;
                }
                if (this.threadIdle != null)
                {
                    this.threadIdle.Close();
                    this.threadIdle = null;
                }
            }
            // free native resources
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// Gets or sets the thread.
        /// </summary>
        /// <value>The thread.</value>
        public Thread Thread
        {
            get { return this.thread; }
            set { this.thread = value; }
        }

        /// <summary>
        /// Gets the task waiting message event.
        /// </summary>
        /// <value>The task waiting.</value>
        public AutoResetEvent TaskWaiting
        {
            get { return this.taskWaiting; }
        }

        /// <summary>
        /// Gets the thread idle message event.
        /// </summary>
        /// <value>The thread idle.</value>
        public ManualResetEvent ThreadIdle
        {
            get { return this.threadIdle; }
        }
    }
    #endregion

    #region ParallelFor class
    /// <summary>
    /// Parallel For state class.
    /// </summary>
    public sealed class ParallelFor : IDisposable
    {
        /// <summary>
        /// Single instance of parallelFor class for singleton pattern
        /// </summary>
        private volatile static ParallelFor instance = null;

        /// <summary>
        /// Delegate defining For loop body.
        /// </summary>
        /// <param name="index">Loop index.</param>
        public delegate void ForLoopDelegate(int index);

        /// <summary>
        /// For-loop body
        /// </summary>
        public ForLoopDelegate LoopFunction;

        /// <summary>
        /// Current loop index
        /// </summary>
        private int currentJobIndex;

        /// <summary>
        /// Stop loop index
        /// </summary>
        private int stopIndex;

        /// <summary>
        /// Number of threads to utilise
        /// </summary>
        private int threadCount = System.Environment.ProcessorCount;

        /// <summary>
        /// The worker threads.
        /// </summary>
        private List<WorkerThread> workerThreads;

        /// <summary>
        /// Runs the For loop.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="stop">The stop.</param>
        /// <param name="loopBody">The loop body.</param>
        public void DoFor(int start, int stop, ForLoopDelegate loopBody)
        {
            this.currentJobIndex = start - 1;
            this.stopIndex = stop;
            this.LoopFunction = loopBody;

            // Signal waiting task to all threads and mark them not idle.
            for (int i = 0; i < this.threadCount; i++)
            {
                WorkerThread workerThread = workerThreads[i];
                workerThread.ThreadIdle.Reset();
                workerThread.TaskWaiting.Set();
            }

            // Wait until all threads become idle
            for (int i = 0; i < this.threadCount; i++)
            {
                WorkerThread workerThread = workerThreads[i];
                workerThread.ThreadIdle.WaitOne();
            }
        }

        /// <summary>
        /// Get instance of the ParallelFor class for singleton pattern and
        /// update the number of threads if appropriate.
        /// </summary>
        /// <param name="threadCount">The thread count.</param>
        /// <returns></returns>
        public static ParallelFor GetInstance(int threadCount)
        {

            if (instance == null)
            {
                instance = new ParallelFor();
                instance.threadCount = threadCount;
                instance.Initialize();
            }
            else
            {
                // Ensure we have the correct number of threads.
                if (instance.workerThreads.Count != threadCount)
                {
                    instance.Terminate();
                    instance.threadCount = threadCount;
                    instance.Initialize();
                }
            }
            return instance;
        }

        #region Private methods
        private void Initialize()
        {
            this.workerThreads = new List<WorkerThread>();

            for (int i = 0; i < this.threadCount; i++)
            {
                WorkerThread workerThread = new WorkerThread();
                workerThread.Thread = new Thread(new ParameterizedThreadStart(RunWorkerThread));
                workerThread.Thread.Name = "worker " + i;
                workerThreads.Add(workerThread);

                workerThread.Thread.IsBackground = true;
                workerThread.Thread.Start(i);
            }
        }

        private void Terminate()
        {
            // Finish thread by setting null loop body and signaling about available task
            LoopFunction = null;
            int workerThreadCount = this.workerThreads.Count;
            for (int i = 0; i < workerThreadCount; i++)
            {
                this.workerThreads[i].Terminate();
            }
        }

        private void RunWorkerThread(object threadIndex)
        {
            WorkerThread workerThread = workerThreads[(int)threadIndex];
            int localJobIndex = 0;

            while (true)
            {
                // Wait for a task.
                workerThread.TaskWaiting.WaitOne();

                // Exit if task is empty.
                if (LoopFunction == null)
                {
                    return;
                }

                localJobIndex = Interlocked.Increment(ref currentJobIndex);

                while (localJobIndex < stopIndex)
                {
                    ////Console.WriteLine("Thread " + threadIndex + " of " + workerThreads.Count + " running task " + localJobIndex);
                    LoopFunction(localJobIndex);
                    localJobIndex = Interlocked.Increment(ref currentJobIndex);
                }

                // Signal that thread is idle.
                workerThread.ThreadIdle.Set();
            }
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">if set to <c>true</c>, dispose managed resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                foreach (WorkerThread worker in this.workerThreads)
                {
                    worker.Dispose();
                }
                this.workerThreads.Clear();
            }
            // free native resources
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    #endregion

    #region ParallelForEach class
    /// <summary>
    /// ParallelForEach state class.
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    public sealed class ParallelForEach<T> : IDisposable
    {
        /// <summary>
        /// Single instance of parallelFor class for singleton pattern
        /// </summary>
        private volatile static ParallelForEach<T> instance = null;

        /// <summary>
        /// Delegate defining Foreach loop body.
        /// </summary>
        /// <param name="item">Loop item.</param>
        public delegate void ForEachLoopDelegate(T item);

        /// <summary>
        /// Foreach-loop body
        /// </summary>
        public ForEachLoopDelegate LoopFunction;

        /// <summary>
        /// Enumerator for the source IEnumerable.
        /// </summary>
        private IEnumerator<T> enumerator;

        /// <summary>
        /// Number of threads to utilise
        /// </summary>
        private int threadCount = System.Environment.ProcessorCount;

        /// <summary>
        /// The worker threads.
        /// </summary>
        private List<WorkerThread> workerThreads;

        /// <summary>
        /// Runs the ForEach loop.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="loopBody">The loop body.</param>
        public void DoForEach(IEnumerable<T> items, ForEachLoopDelegate loopBody)
        {
            this.enumerator = items.GetEnumerator();
            this.LoopFunction = loopBody;

            // Signal waiting task to all threads and mark them not idle.
            for (int i = 0; i < this.threadCount; i++)
            {
                WorkerThread workerThread = workerThreads[i];
                workerThread.ThreadIdle.Reset();
                workerThread.TaskWaiting.Set();
            }

            // Wait until all threads become idle
            for (int i = 0; i < this.threadCount; i++)
            {
                WorkerThread workerThread = workerThreads[i];
                workerThread.ThreadIdle.WaitOne();
            }
        }

        /// <summary>
        /// Get instance of the ParallelFor class for singleton pattern and
        /// update the number of threads if appropriate.
        /// </summary>
        /// <param name="threadCount">The thread count.</param>
        /// <returns></returns>
        public static ParallelForEach<T> GetInstance(int threadCount)
        {

            if (instance == null)
            {
                instance = new ParallelForEach<T>();
                instance.threadCount = threadCount;
                instance.Initialize();
            }
            else
            {
                // Ensure we have the correct number of threads.
                if (instance.workerThreads.Count != threadCount)
                {
                    instance.Terminate();
                    instance.threadCount = threadCount;
                    instance.Initialize();
                }
            }
            return instance;
        }

        #region Private methods
        private void Initialize()
        {
            this.workerThreads = new List<WorkerThread>();

            for (int i = 0; i < this.threadCount; i++)
            {
                WorkerThread workerThread = new WorkerThread();
                workerThread.Thread = new Thread(new ParameterizedThreadStart(RunWorkerThread));
                workerThread.Thread.Name = "worker " + i;
                workerThreads.Add(workerThread);

                workerThread.Thread.IsBackground = true;
                workerThread.Thread.Start(i);
            }
        }

        private void Terminate()
        {
            // Finish thread by setting null loop body and signaling about available task
            LoopFunction = null;
            int workerThreadCount = this.workerThreads.Count;
            for (int i = 0; i < workerThreadCount; i++)
            {
                this.workerThreads[i].Terminate();
            }
        }

        private void RunWorkerThread(object threadIndex)
        {
            WorkerThread workerThread = workerThreads[(int)threadIndex];

            while (true)
            {
                // Wait for a task.
                workerThread.TaskWaiting.WaitOne();

                // Exit if task is empty.
                if (LoopFunction == null)
                {
                    return;
                }

                bool didMoveNext;
                T localItem = default(T);
                lock (this.enumerator)
                {
                    didMoveNext = enumerator.MoveNext();
                    if (didMoveNext)
                    {
                        localItem = enumerator.Current;
                    }
                }

                while (didMoveNext == true)
                {
                    ////Console.WriteLine("Thread " + threadIndex + " of " + workerThreads.Count + " running task " + localJobIndex);
                    LoopFunction(localItem);
                    lock (this.enumerator)
                    {
                        didMoveNext = enumerator.MoveNext();
                        if (didMoveNext)
                        {
                            localItem = enumerator.Current;
                        }
                    }
                }

                // Signal that thread is idle.
                workerThread.ThreadIdle.Set();
            }
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">if set to <c>true</c>, dispose managed resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                this.enumerator.Dispose();
                foreach (WorkerThread worker in this.workerThreads)
                {
                    worker.Dispose();
                }
                this.workerThreads.Clear();
            }
            // free native resources
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    #endregion

    #region Class variables
    /// <summary>
    /// Object for thread locking
    /// </summary>
    private static object lockObject = new Object();

    /// <summary>
    /// Number of threads to utilise
    /// </summary>
    private static int threadCount = System.Environment.ProcessorCount;
    #endregion

    #region Constructor
    /// <summary>
    /// Prevents a default instance of the <see cref="Parallel"/> class from being created.
    /// </summary>
    private Parallel()
    {
        // Do nothing.
    }
    #endregion

    #region IDisposable
    /// <summary>
    /// Disposes resources.
    /// </summary>
    /// <param name="disposing">if set to <c>true</c>, dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // dispose managed resources
            // SAA TODO: Should we dispose the ParallelFor and ParallelForEach instances?
        }
        // free native resources
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Public methods
    /// <summary>
    /// Executes a parallel For loop.
    /// </summary>
    /// <param name="start">Loop start index.</param>
    /// <param name="stop">Loop stop index.</param>
    /// <param name="loopBody">Loop body.</param>
    /// <remarks>The method is used to parallelise for loop by running iterations across
    /// several threads.
    /// Example usage:
    /// <code>
    /// for ( int i = 0; i &lt; 10; i++ )
    /// {
    ///   System.Diagnostics.Debug.WriteLine( "i = " + i );
    /// }
    /// </code>
    /// can be replaced by:
    /// <code>
    /// Parallel.For( 0, 10, delegate( int i )
    /// {
    ///   System.Diagnostics.Debug.WriteLine( "i = " + i );
    /// } );
    /// </code>
    /// If <c>Parallel.ThreadCount</c> is exactly <c>1</c>, no threads are spawned.
    /// </remarks>
    public static void For(int start, int stop, ParallelFor.ForLoopDelegate loopBody)
    {
        if (Parallel.threadCount == 1)
        {
            for (int i = start; i < stop; i++)
            {
                loopBody(i);
            }
        }
        else
        {
            lock (lockObject)
            {
                ParallelFor parallel = ParallelFor.GetInstance(threadCount);
                parallel.DoFor(start, stop, loopBody);
            }
        }
    }

    /// <summary>
    /// Executes a parallel Foreach loop.
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="items">Loop items.</param>
    /// <param name="loopBody">Loop body.</param>
    /// <remarks>The method is used to parallelise for loop by running iterations across
    /// several threads.
    /// Example usage:
    /// <code>
    /// foreach ( Molecule molecule in molecules )
    /// {
    /// System.Diagnostics.Debug.WriteLine( "molecule.Title = " + molecule.Title );
    /// }
    /// </code>
    /// can be replaced by:
    /// <code>
    /// Parallel.ForEach{Molecule}( molecules, delegate( Molecule molecule )
    /// {
    /// System.Diagnostics.Debug.WriteLine( "molecule.Title = " + molecule.Title );
    /// } );
    /// </code>
    /// If <c>Parallel.ThreadCount</c> is exactly <c>1</c>, no threads are spawned.
    /// </remarks>
    public static void ForEach<T>(IEnumerable<T> items, ParallelForEach<T>.ForEachLoopDelegate loopBody)
    {
        if (Parallel.threadCount == 1)
        {
            foreach (T item in items)
            {
                loopBody(item);
            }
        }
        else
        {
            lock (lockObject)
            {
                ParallelForEach<T> parallel = ParallelForEach<T>.GetInstance(threadCount);
                parallel.DoForEach(items, loopBody);
            }
        }
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the number of threads used for parallel computations.
    /// </summary>
    /// <value>The threads count.</value>
    /// <remarks>
    /// By default the property is number of CPUs, i.e.,
    /// <see cref="System.Environment.ProcessorCount"/>. Setting the
    /// property to zero also causes it to be reset to this value.
    /// </remarks>
    public static int ThreadsCount
    {
        get
        {
            return Parallel.threadCount;
        }
        set
        {
            lock (lockObject)
            {
                Parallel.threadCount = value == 0 ? System.Environment.ProcessorCount : value;
            }
        }
    }
    #endregion
}

