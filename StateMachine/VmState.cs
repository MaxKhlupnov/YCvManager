using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Yandex.Cloud.Compute.V1;
using Serilog;


namespace VmManager.StateMachine
{

    public enum VmIstanceState
    {
        Unspecified = 0,
        // Instance is waiting for resources to be allocated.
        Provisioning = 1,
        // Instance is running normally.
        Running = 2,

        Stopping = 3,
        Stopped = 4,
        Starting = 5,
        Restarting = 6,
        Updating = 7,
        Error = 8,
        Crashed = 9,
        Deleting = 10,
        // Instance in a Web Service Call
        Calculating = 11,
        // Web Service Call compleated
        Calculated = 12
    }

    /***
    * Virtual machine states
    */   
    public abstract class VmState
    {
        public VmIstanceState State { get; set; }
        public string Fqdn { get; set; }

        public string[] PublicIp { get; set; }
        /// <summary>
        /// Handle state
        /// </summary>
        /// <param name="context"> context </param>
        /// <returns>new state</returns>
        public abstract Task<VmState> Handle(Context context);

        /**
         * Load Instance state from the cloud 
         */
        protected async Task<VmIstanceState> LoadInstanceState(Context context)
        {

            Log.Information($"Loading instance {context.InstanceId}  vm state");
            Yandex.Cloud.Compute.V1.GetInstanceRequest req = new Yandex.Cloud.Compute.V1.GetInstanceRequest() { InstanceId = context.InstanceId };
            Instance vm = await context.CloudSdk.Services.Compute.InstanceService.GetAsync(req);

            VmIstanceState state = Enum.Parse<VmIstanceState>(vm.Status.ToString());
            this.Fqdn = vm.Fqdn
                ;
            List<string> ipList = new List<string>();
            foreach (NetworkInterface iface in vm.NetworkInterfaces){
                ipList.Add(iface.PrimaryV4Address.Address);
            }
            if (ipList.Count > 0)
                PublicIp = ipList.ToArray();

            Log.Information($"Instance {context.InstanceId} state is {state.ToString()}");

            return state;
        }

    }
    
}
