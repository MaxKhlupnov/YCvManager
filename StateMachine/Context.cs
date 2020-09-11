using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Yandex.Cloud.Endpoint;
using Yandex.Cloud.Resourcemanager.V1;
using Yandex.Cloud;
using Yandex.Cloud.Credentials;
using Yandex.Cloud.Compute.V1;
using Serilog;
using System.Collections.Generic;
using System.Text;

namespace VmManager.StateMachine
{
    public class Context
    {
        /// Connectivity to Yandex Cloud control plane API     
        internal Sdk CloudSdk { get; set; }

        // VM instance state
        internal VmState InstanceState { get; set; }
        internal string InstanceId { get; set; }

        //raise timeout error if state not changed after this amount of seconds 
        internal int StateTimeout { get; set; }

        internal IConfiguration config {get; private set;}

        internal int StateRefreshTime { get; set; }

        public Context(IConfiguration configuration, VmState initialState)
        {
            this.config = configuration;

            if (configuration.GetSection("Yandex") == null || string.IsNullOrEmpty(configuration.GetSection("Yandex")["oAuth"]))
            {
                Log.Fatal("Please specify oAuth token in configuration file");
                throw new ArgumentNullException("Yandex->oAuth token is not set");
            }
            this.CloudSdk = new Sdk(new OAuthCredentialsProvider(configuration.GetSection("Yandex")["oAuth"]));

            if (string.IsNullOrEmpty(configuration.GetSection("Yandex")["InstanceId"]))
            {
                Log.Fatal("Please specify VM InstanceId in configuration file");
                throw new ArgumentNullException("Yandex->InstanceId is not set");
            }

            this.InstanceId = configuration.GetSection("Yandex")["InstanceId"];

            int refreshTime = 0;
            if (int.TryParse(configuration.GetSection("StateMachine")["StateRefreshTime"], out refreshTime))
            {               
                this.StateRefreshTime = refreshTime ;
            }
            else
            {
                this.StateRefreshTime = 5;  // Default - 5 sec.
            }
            Log.Information($"Set state refresh interval  {this.StateRefreshTime} sec.");

            int stateTimeout = 6000;//raise timeout error if state not changed after this amount of seconds  = 0;
            if (int.TryParse(configuration.GetSection("StateMachine")["StateTimeout"], out stateTimeout))
            {               
                this.StateTimeout = stateTimeout;
            }
            else
            {
                this.StateTimeout = 600;  // Default - 600 sec.
            }
            Log.Information($"Set state timeout at {this.StateTimeout} sec.");

            this.InstanceState = initialState;
        }

        private Context() {
            throw new NotSupportedException("Specify Instance id in constructor");
        }

        /**
         * Perform next request
         */
        public async Task<VmState> Request()
        {
            if (this.InstanceState != null)
            {
                VmIstanceState prevState = this.InstanceState.State;
                while (prevState == this.InstanceState.State) //Wait for state change
                {
                    this.InstanceState = await this.InstanceState.Handle(this);

                    if (this.InstanceState == null)
                    {
                        break;
                    }

                    System.Threading.Thread.Sleep(this.StateRefreshTime * 1000); // sleep before next check

                    // TODO: Add state timeout check; return TimeOutState for timeouts
                }
                return this.InstanceState;
            }
            else
            {
                Log.Information("No more states in pipeline");
                return await Task.FromResult<VmState>(null);
            }
        } 
       
    }
}
