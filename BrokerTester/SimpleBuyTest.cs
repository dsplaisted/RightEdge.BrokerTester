using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightEdge.Common;

class SimpleBuyTest : BrokerTest
{
    TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

    public int PositionSize { get; set; } = 100;

    public SimpleBuyTest(MySymbolScript symbolScript) : base(symbolScript)
    {
    }

    public override Task StartTest()
    {
        try
        {
            var position = SymbolScript.OpenPosition(PositionType.Long, OrderType.Market, 0, PositionSize);
            if (position.Error != null)
            {
                Assert.Fail(position.Error);
            }
        }
        catch (Exception ex)
        {
            _taskCompletionSource.SetException(ex);
        }


        return _taskCompletionSource.Task;
    }

    public override void ProcessEvent(object @event)
    {
        try
        {
            if (@event is OrderFilledEvent)
            {
                var orderFilled = (OrderFilledEvent) @event;
                if (orderFilled.Trade.Order.OrderState == BrokerOrderState.Filled)
                {
                    Assert.AreEqual(orderFilled.Trade.Order.Fills.Sum(f => f.Quantity), PositionSize, "Total fill size");
                    _taskCompletionSource.SetResult(true);
                }
                else
                {
                    //  If we got a fill event and the state is not Filled, the only other state we except is PartiallyFilled
                    Assert.AreEqual(orderFilled.Trade.Order.OrderState, BrokerOrderState.PartiallyFilled, "Order state");
                }
            }
            else if (@event is OrderCancelledEvent)
            {
                Assert.Fail("Order canceled: " + ((OrderCancelledEvent) @event).Information);
            }
        }
        catch (Exception ex)
        {
            _taskCompletionSource.SetException(ex);
        }
    }
}
