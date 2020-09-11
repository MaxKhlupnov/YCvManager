using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Yandex.Cloud.Compute.V1;
using Serilog;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;

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

        public string[] IPs { get; set; }

        public string PrivateIp
        {
            get
            {
                if (this.IPs != null && this.IPs.Length > 0)
                    return this.IPs[0];
                else
                    return null;
            }
        }

        public string PubllicIp
        {
            get
            {
                if (this.IPs != null && this.IPs.Length > 1)
                    return this.IPs[this.IPs.Length];
                else
                    return null;
            }
        }
        /// <summary>
        /// Handle state
        /// </summary>
        /// <param name="context"> context </param>
        /// <returns>new state</returns>
        public abstract Task<VmState> Handle(Context context);


        protected VmState(VmIstanceState state)
        {
            this.State = state;
        }

        protected VmState(VmState parentState)
        {
            this.State = parentState.State;
            this.Fqdn = parentState.Fqdn;
            this.IPs = parentState.IPs;
        }
        /**
         * Load Instance state from the cloud 
         */
        protected async Task<VmIstanceState> LoadInstanceState(Context context)
        {

            Log.Information($"Loading instance {context.InstanceId}  vm state");
            Yandex.Cloud.Compute.V1.GetInstanceRequest req = new Yandex.Cloud.Compute.V1.GetInstanceRequest() { InstanceId = context.InstanceId };
            Instance vm = await context.CloudSdk.Services.Compute.InstanceService.GetAsync(req);

            VmIstanceState state = Enum.Parse<VmIstanceState>(vm.Status.ToString());
            this.Fqdn = vm.Fqdn;

            List<string> ipList = new List<string>();
            foreach (NetworkInterface iface in vm.NetworkInterfaces){
                ipList.Add(iface.PrimaryV4Address.Address);
            }
            if (ipList.Count > 0)
                IPs = ipList.ToArray();

            Log.Information($"Instance {context.InstanceId} state is {state.ToString()}");

            return state;
        }

        /// <summary>
        /// Fill temaplte based on 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        protected string ReplaceMacro(string value, VmState state)
        {
            return Regex.Replace(value, @"{(?<exp>[^}]+)}", match => {
                var p = Expression.Parameter(typeof(VmState), "state");
                var e = DynamicExpressionParser.ParseLambda(new[] { p }, null, match.Groups["exp"].Value);
                return (e.Compile().DynamicInvoke(state) ?? "").ToString();
            });
        }

    }
    
}
