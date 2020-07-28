/******************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016 Bunny83
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 * 
 * This implements a simple threaded job queue. Simply derive a class from JobItem
 * and override the DoWork method.
 * 
 *****************************************************************************/

namespace B83.JobQueue
{
    using System;
    using System.Threading;
    using System.Collections.Generic;

    public abstract class JobItem
    {
        private volatile bool m_Abort = false;
        private volatile bool m_Started = false;
        private volatile bool m_DataReady = false;

        /// <summary>
        /// This is the actual job routine. override it in a concrete Job class
        /// </summary>
        protected abstract void DoWork();

        /// <summary>
        /// This is a callback which will be called from the main thread when
        /// the job has finised. Can be overridden.
        /// </summary>
        public virtual void OnFinished() { }

        public bool IsAborted { get { return m_Abort; } }
        public bool IsStarted { get { return m_Started; } }
        public bool IsDataReady { get { return m_DataReady; } }

        public void Execute()
        {
            m_Started = true;
            DoWork();
            m_DataReady = true;
        }

        public void AbortJob()
        {
            m_Abort = true;
        }

        public void ResetJobState()
        {
            m_Started = false;
            m_DataReady = false;
            m_Abort = false;
        }
    }


    public class JobQueue<T> : IDisposable where T : JobItem
    {
        private class ThreadItem
        {
            private Thread m_Thread;
            private AutoResetEvent m_Event;
            private volatile bool m_Abort = false;

            // simple linked list to manage active threaditems
            public ThreadItem NextActive = null;

            // the job item this thread is currently processing
            public T Data;

            public ThreadItem()
            {
                m_Event = new AutoResetEvent(false);
                m_Thread = new Thread(ThreadMainLoop);
                m_Thread.Start();
            }

            private void ThreadMainLoop()
            {
                while (true)
                {
                    if (m_Abort)
                        return;
                    m_Event.WaitOne();
                    if (m_Abort)
                        return;
                    Data.Execute();
                }
            }

            public void StartJob(T aJob)
            {
                aJob.ResetJobState();
                Data = aJob;
                // signal the thread to start working.
                m_Event.Set();
            }

            public void Abort()
            {
                m_Abort = true;
                if (Data != null)
                    Data.AbortJob();
                // signal the thread so it can finish itself.
                m_Event.Set();
            }

            public void Reset()
            {
                Data = null;
            }
        }
        // internal thread pool
        private Stack<ThreadItem> m_Threads = new Stack<ThreadItem>();
        private Queue<T> m_NewJobs = new Queue<T>();
        private volatile bool m_NewJobsAdded = false;
        private Queue<T> m_Jobs = new Queue<T>();
        // start of the linked list of active threads
        private ThreadItem m_Active = null;

        public event Action<T> OnJobFinished;

        public JobQueue(int aThreadCount)
        {
            if (aThreadCount < 1)
                aThreadCount = 1;
            for (int i = 0; i < aThreadCount; i++)
                m_Threads.Push(new ThreadItem());
        }

        public void AddJob(T aJob)
        {
            if (m_Jobs == null)
                throw new System.InvalidOperationException("AddJob not allowed. JobQueue has already been shutdown");
            if (aJob != null)
            {
                m_Jobs.Enqueue(aJob);
                ProcessJobQueue();
            }
        }

        public void AddJobFromOtherThreads(T aJob)
        {
            lock (m_NewJobs)
            {
                if (m_Jobs == null)
                    throw new System.InvalidOperationException("AddJob not allowed. JobQueue has already been shutdown");
                m_NewJobs.Enqueue(aJob);
                m_NewJobsAdded = true;
            }
        }

        public int CountActiveJobs()
        {
            int count = 0;
            for (var thread = m_Active; thread != null; thread = thread.NextActive)
                count++;
            return count;
        }

        private void CheckActiveJobs()
        {
            ThreadItem thread = m_Active;
            ThreadItem last = null;
            while (thread != null)
            {
                ThreadItem next = thread.NextActive;
                T job = thread.Data;
                if (job.IsAborted)
                {
                    if (last == null)
                        m_Active = next;
                    else
                        last.NextActive = next;
                    thread.NextActive = null;

                    thread.Reset();
                    m_Threads.Push(thread);
                }
                else if (thread.Data.IsDataReady)
                {
                    job.OnFinished();
                    if (OnJobFinished != null)
                        OnJobFinished(job);

                    if (last == null)
                        m_Active = next;
                    else
                        last.NextActive = next;
                    thread.NextActive = null;

                    thread.Reset();
                    m_Threads.Push(thread);
                }
                else
                    last = thread;
                thread = next;
            }
        }

        private void ProcessJobQueue()
        {
            if (m_NewJobsAdded)
            {
                lock (m_NewJobs)
                {
                    while (m_NewJobs.Count > 0)
                        AddJob(m_NewJobs.Dequeue());
                    m_NewJobsAdded = false;
                }
            }
            while (m_Jobs.Count > 0 && m_Threads.Count > 0)
            {
                var job = m_Jobs.Dequeue();
                if (!job.IsAborted)
                {
                    var thread = m_Threads.Pop();
                    thread.StartJob(job);
                    // add thread to the linked list of active threads
                    thread.NextActive = m_Active;
                    m_Active = thread;
                }
            }
        }

        public void Update()
        {
            CheckActiveJobs();
            ProcessJobQueue();
        }

        public void ShutdownQueue()
        {
            for (var thread = m_Active; thread != null; thread = thread.NextActive)
                thread.Abort();
            while (m_Threads.Count > 0)
                m_Threads.Pop().Abort();
            while (m_Jobs.Count > 0)
                m_Jobs.Dequeue().AbortJob();
            m_Jobs = null;
            m_Active = null;
            m_Threads = null;
        }

        public void Dispose()
        {
            ShutdownQueue();
        }
    }
}