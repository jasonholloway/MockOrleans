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

        
        //respecting requests - there needs to be a fixture-wide tracking of requests then
        //when they're quiet, no more should be forthcoming, unless timers and reminders etc are working,
        //which they shouldn't be, under normal circumstances.

        //and this works with reentrancy too - there should be a request registry that keeps a count
        //but such a count also determines grain idleness...




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

            var t = grain.Execute(2, 8, 50); //includes Task.Delay, creating gaps in which fixture scheduler will temporarily quiten, before real completion
            
            await fx.Requests.WhenIdle();   //this should be packaged into complete funciton surely
            await fx.Scheduler.CloseWhenIdle();

            Assert.That(t.IsCompleted, Is.True);
        }





        //simulates a tree of async calls
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
