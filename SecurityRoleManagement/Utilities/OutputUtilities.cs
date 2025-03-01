using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace SecurityRoleManagement.Utilities
{
    public static class OutputUtilities
    {
        public static void SetOutputParameters(
            Dictionary<string, object> outputParameters,
            IPluginExecutionContext context,
            ITracingService tracer
        )
        {
            tracer.Trace("Start: " + nameof(SetOutputParameters) + DateTime.Now.ToString());
            foreach (var parameter in outputParameters)
            {
                tracer.Trace($"Setting output parameter: {parameter.Key}");
                tracer.Trace($"Value: {parameter.Value}");
                context.OutputParameters.AddOrUpdateIfNotNull(parameter.Key, parameter.Value);
            }
            tracer.Trace("End: " + nameof(SetOutputParameters) + DateTime.Now.ToString());
        }
    }
}
