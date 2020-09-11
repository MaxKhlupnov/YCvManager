using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Yandex.Cloud.Operation;
using Serilog;

namespace VmManager.StateMachine
{
    public class StoppingVmState : VmState
    {
        internal StoppingVmState(VmState previous) : base(previous) { }
        public override async Task<VmState> Handle(Context context)
        {
            Log.Logger.Information($"Handle stopping vm state for {context.InstanceId}. Initial state is {this.State} ");

            

            if (this.State == VmIstanceState.Calculated || this.State == VmIstanceState.Running)
            {
                await StopVm(context);
                this.State = VmIstanceState.Stopping;
            }
            else if (this.State == VmIstanceState.Stopped)
            {
                // We done - cancel pipeline              
              return   await Task.FromResult<VmState>(null);
            }
            else
            {
                this.State = await this.LoadInstanceState(context);
            }

            return await Task.FromResult<VmState>(this);
        }

        private async Task StopVm(Context context)
        {
            Log.Information($"Stopping instance {context.InstanceId} ");
            Yandex.Cloud.Compute.V1.StopInstanceRequest req = new Yandex.Cloud.Compute.V1.StopInstanceRequest() { InstanceId = context.InstanceId };
            Operation result = await context.CloudSdk.Services.Compute.InstanceService.StopAsync(req);
            Log.Information($"instance id stop operation result is {result.ToString()} ");
        }

    }
}
