﻿using MockOrleans;
using NSubstitute;
using NUnit.Framework;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{

    [TestFixture]
    public class CompletionTests
    {
        
        [Test]
        public async Task CompletionSucceedsWhenAllTasksRunViaFixtureScheduler() 
        {
            var fx = new MockFixture(Substitute.For<IServiceProvider>());            
            fx.Types.Map<IBranchingExecutor, BranchingExecutor>();
            
            var grain = fx.GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());
            
            var task = grain.Execute(3, 8);                                          
            
            await fx.Scheduler.CloseWhenIdle();
            
            Assert.That(task.IsCompleted, Is.True);
        }
                

        [Test]
        public async Task SchedulerClosesImmediatelyIfEmpty() 
        {
            var scheduler = new FixtureScheduler();
            
            await scheduler.CloseWhenIdle();

            Assert.That(scheduler.IsOpen, Is.False);
        }
        

        [Test]
        public async Task CompletionRespectsRequestsWhenSomeTasksRunElsewhere() 
        {
            var fx = new MockFixture(Substitute.For<IServiceProvider>());
            fx.Types.Map<IBranchingExecutor, BranchingExecutor>();

            var grain = fx.GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());

            var t = grain.Execute(2, 8, 50); //includes Task.Delay, creating gaps in which fixture scheduler will temporarily quieten, before real completion
            
            await fx.Requests.WhenIdle();   //to be packaged into complete function
            await fx.Scheduler.CloseWhenIdle();

            Assert.That(t.IsCompleted, Is.True);
        }





        //executes a tree of async calls
        public interface IBranchingExecutor : IGrainWithGuidKey 
        {
            Task Execute(int branching, int depth, int delay = 0);
        }


        public class BranchingExecutor : Grain, IBranchingExecutor
        {
            public async Task Execute(int branching, int depth, int delay) 
            {
                if(delay > 0) await Task.Delay(delay);
                
                if(depth > 0) {
                    await Task.WhenAll(Enumerable.Range(0, branching)
                                                .Select(async _ => {
                                                    var next = GrainFactory.GetGrain<IBranchingExecutor>(Guid.NewGuid());
                                                    await next.Execute(branching, depth - 1, delay);
                                                }));
                }
            }
            
        }

        

    }
}
