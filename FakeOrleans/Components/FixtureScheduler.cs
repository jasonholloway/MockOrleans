﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeOrleans
{    

    public class FixtureScheduler : TaskScheduler
    {
        ExceptionSink _exceptionSink;

        object _sync = new object();
        int _taskCount = 0;
        TaskCompletionSource<bool> _tsOnIdle = new TaskCompletionSource<bool>();



        public FixtureScheduler(ExceptionSink exceptionSink = null) {
            _exceptionSink = exceptionSink;
        }


        protected override IEnumerable<Task> GetScheduledTasks() {
            return Enumerable.Empty<Task>();
        }


        
        protected override void QueueTask(Task task) 
        {
            lock(_sync) {
                _taskCount++;
            }
            
            try {
                ThreadPool.QueueUserWorkItem(_ => {
                    try {
                        TryExecuteTask(task); //No exceptions thrown, as always packaged into task...

                        if(task.IsFaulted) {
                            _exceptionSink?.Add(task.Exception);
                        }
                    }
                    finally {
                        DecrementTaskCount();
                    }
                });
            }
            catch(NotSupportedException) {
                DecrementTaskCount();
                throw;
            }
        }


        void DecrementTaskCount() 
        {
            TaskCompletionSource<bool> ts = null;
            
            lock(_sync) {
                _taskCount--;

                if(_taskCount == 0) {
                    ts = _tsOnIdle;
                    _tsOnIdle = new TaskCompletionSource<bool>();
                }
            }

            ts?.SetResult(true);
        }


        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            return false;
        }
        



        public Task WhenIdle() 
        {
            lock(_sync) {
                return _taskCount == 0
                        ? Task.CompletedTask
                        : _tsOnIdle.Task;
            }
        }
    
    

    }
}
