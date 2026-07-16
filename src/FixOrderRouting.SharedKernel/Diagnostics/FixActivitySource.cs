using System.Diagnostics;

namespace FixOrderRouting.SharedKernel.Diagnostics;

public static class FixActivitySource
{
    public static readonly ActivitySource Instance = new("FixOrderRouting.FIX", "1.0.0");

    public static Activity? StartSendNewOrderSingle(string clOrdId, string symbol, string side, long quantity, decimal price)
    {
        var activity = Instance.StartActivity("fix.send_new_order_single");
        if (activity != null)
        {
            activity.SetTag("fix.message_type", "NewOrderSingle");
            activity.SetTag("fix.cl_ord_id", clOrdId);
            activity.SetTag("fix.symbol", symbol);
            activity.SetTag("fix.side", side);
            activity.SetTag("fix.quantity", quantity);
            activity.SetTag("fix.price", price);
        }
        return activity;
    }

    public static Activity? StartReceiveNewOrderSingle(string clOrdId, string symbol)
    {
        var activity = Instance.StartActivity("fix.receive_new_order_single");
        if (activity != null)
        {
            activity.SetTag("fix.message_type", "NewOrderSingle");
            activity.SetTag("fix.cl_ord_id", clOrdId);
            activity.SetTag("fix.symbol", symbol);
        }
        return activity;
    }

    public static Activity? StartSendExecutionReport(string clOrdId, string status, string? rejection = null)
    {
        var activity = Instance.StartActivity("fix.send_execution_report");
        if (activity != null)
        {
            activity.SetTag("fix.message_type", "ExecutionReport");
            activity.SetTag("fix.cl_ord_id", clOrdId);
            activity.SetTag("fix.exec_status", status);
            if (rejection != null)
            {
                activity.SetTag("fix.rejection_reason", rejection);
            }
        }
        return activity;
    }

    public static Activity? StartReceiveExecutionReport(string clOrdId, string execType, string ordStatus)
    {
        var activity = Instance.StartActivity("fix.receive_execution_report");
        if (activity != null)
        {
            activity.SetTag("fix.message_type", "ExecutionReport");
            activity.SetTag("fix.cl_ord_id", clOrdId);
            activity.SetTag("fix.exec_type", execType);
            activity.SetTag("fix.ord_status", ordStatus);
        }
        return activity;
    }

    public static void RecordExposureValidation(Activity? activity, bool accepted, decimal exposure, decimal limit)
    {
        if (activity != null)
        {
            activity.SetTag("fix.exposure_check.accepted", accepted);
            activity.SetTag("fix.exposure_check.exposure", exposure);
            activity.SetTag("fix.exposure_check.limit", limit);
        }
    }
}
