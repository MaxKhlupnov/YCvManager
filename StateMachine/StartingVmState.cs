using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yandex.Cloud.Operation;
using Serilog;
using System.Text;

namespace VmManager.StateMachine
{
    public class StartingVmState : VmState
    {
        public StartingVmState()
        {
            this.State = VmIstanceState.Unspecified;
        }
        public override async Task<VmState> Handle(Context context)
        {
            Log.Logger.Information($"Handle starting vm state for {context.InstanceId}. Initial state is {this.State} ");
         
            this.State = await this.LoadInstanceState(context);

            if (this.State == VmIstanceState.Running)
            {
                return new DoWorkVmState() { State = VmIstanceState.Running };               
            }
            else if (this.State == VmIstanceState.Stopped)
            {
                await StartVm(context);
            }
            
            return await Task.FromResult<VmState>(this);
        }

        private async Task StartVm(Context context)
        {
            Log.Information($"Starting instance {context.InstanceId} ");
            Yandex.Cloud.Compute.V1.StartInstanceRequest req = new Yandex.Cloud.Compute.V1.StartInstanceRequest() { InstanceId = context.InstanceId };
            Operation result = await context.CloudSdk.Services.Compute.InstanceService.StartAsync(req);
            Log.Information($"instance id starting operation result is {result.ToString()} ");
        }
    }
}
