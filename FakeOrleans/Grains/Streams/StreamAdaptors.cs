﻿using FakeOrleans.Grains;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans.Streams
{
    

    public class GrainStreamProviderAdaptor : IStreamProvider, IProvider
    {
        public string Name { get; private set; }
        public bool IsRewindable { get; } = false;

        readonly Activation_New _ctx;
        
        public GrainStreamProviderAdaptor(Activation_New ctx, string providerName) {
            _ctx = ctx;
            Name = providerName;
        }
        
        public IAsyncStream<T> GetStream<T>(Guid streamId, string streamNamespace) 
        {
            var key = new StreamKey(Name, streamNamespace, streamId);
            
            var stream = _ctx.Fixture.Streams.GetStream(key);
                        
            return new GrainStreamClient<T>(_ctx, stream);
        }

        #region IProvider

        Task IProvider.Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
            => Task.CompletedTask;

        Task IProvider.Close()
            => Task.CompletedTask;

        #endregion
    }

    
    class StreamProviderManagerAdaptor : IStreamProviderManager
    {
        readonly Activation_New _ctx;

        public StreamProviderManagerAdaptor(Activation_New ctx) {
            _ctx = ctx;
        }

        public IProvider GetProvider(string name)
            => new GrainStreamProviderAdaptor(_ctx, name);


        public IEnumerable<IStreamProvider> GetStreamProviders() {

            //StreamRegistry needs a dictionary of streamproviders, implicitly created

            throw new NotImplementedException(); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }
    }
    

}
