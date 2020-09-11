using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Yandex.Cloud.Operation;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace VmManager.StateMachine
{
    public class DoWorkVmState : VmState
    {
        public override async Task<VmState> Handle(Context context)
        {
            Log.Logger.Information($"Perfrom VM Web Service call. Current state is {this.State} ");

            
            if (this.State == VmIstanceState.Running || this.State == VmIstanceState.Calculating)
            {
                // Запускаем обработку и ждем завершения процесса
                VmState newState = new DoWorkVmState() { State = VmIstanceState.Running };
                this.State = VmIstanceState.Calculating;
                string results = await RunTask(context);
                if (!string.IsNullOrEmpty(results))
                {
                    Log.Information($"Calculation results: {results}");
                    return new StoppingVmState() { State = VmIstanceState.Calculated };
                }
                else
                {
                    return await Task.FromResult<VmState>(this);
                }             
            }
            else if (this.State != VmIstanceState.Calculating)
            {
                VmIstanceState currentState = await this.LoadInstanceState(context);
            }
            // Breake pipeline 
            return await Task.FromResult<VmState>(null);
        }

        /// <summary>
        /// Perfor webservice call and return true if sucess
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<string> RunTask(Context context)
        {
           // Uri webSvcUri = new Uri(MakeWebServiceUrl(context));
           // 
           Log.Information($"Performing web serivce call to: {MakeWebServiceUrl(context)}"); 
            return "Success";
           // Yandex.Cloud.Compute.V1.StartInstanceRequest req = new Yandex.Cloud.Compute.V1.StartInstanceRequest() { InstanceId = context.InstanceId };
           // Operation result = await context.CloudSdk.Services.Compute.InstanceService.StartAsync(req);
           // Log.Information($"instance id starting operation result is {result.ToString()} ");
        }

        private string MakeWebServiceUrl(Context context)
        {
            return context.config.GetSection("CalculationSvc")["RestUrl"].Replace("{fqdn}", this.Fqdn);
        }
    }
}
