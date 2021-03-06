﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Yandex.Cloud.Operation;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;

namespace VmManager.StateMachine
{
    public class DoWorkVmState : VmState
    {
 
        internal DoWorkVmState(VmState previous) : base(previous){ }

        public override async Task<VmState> Handle(Context context)
        {
            Log.Logger.Information($"Perfrom VM Web Service call. Current state is {this.State} ");

            
            if (this.State == VmIstanceState.Running || this.State == VmIstanceState.Calculating)
            {
                // Запускаем обработку и ждем завершения процесса
                VmState newState = new DoWorkVmState(this) { State = VmIstanceState.Running };
                this.State = VmIstanceState.Calculating;
                 string results = await RunTask(context);
                if (!string.IsNullOrEmpty(results))
                {
                    Log.Information($"Calculation results: {results}");
                    return new StoppingVmState(this) { State = VmIstanceState.Calculated };
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
            string sUrl = MakeWebServiceUrl(context);
            Log.Information($"Performing web serivce call to: {sUrl}");
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync(new Uri(sUrl));
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
                else
                {
                    Log.Error($"Http response code {response.StatusCode}. Retrying....");                   
                }
            }catch(Exception ex)
            {
                Log.Error(ex, $"Http request error {ex.Message}. Retrying....");
            }
                return null;
        }

        private string MakeWebServiceUrl(Context context)
        {
            return this.ReplaceMacro(context.config.GetSection("CalculationSvc")["RestUrl"], this);
          
        }
    }
}
