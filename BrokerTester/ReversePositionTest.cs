using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightEdge.Common;

class ReversePositionTest : AsyncBrokerTest
{
    public int PositionSize { get; set; }

    public bool BrokerSupportsPositionReversal { get; set; }

    public ReversePositionTest(MySymbolScript symbolScript) : base(symbolScript)
    {
        PositionSize = 100;

        //  Set up PositionManager to allow position reversals if the broker supports them
        SymbolScript.PositionManager.PositionOverfilledAction = PositionAction.DoNothing;
    }


    protected override async Task RunTestAsync()
    {
        var position = SymbolScript.OpenPosition(PositionType.Long, OrderType.Market, 0, PositionSize);
        if (position.Error != null)
        {
            Assert.Fail(position.Error);
        }

        var order = position.Orders[0];

        await WaitForCompleteFillAsync(order);

        OrderSettings reverseOrderSettings = new OrderSettings()
        {
            OrderType = OrderType.Market,
            TransactionType = TransactionType.Sell,
            Size = PositionSize * 2
        };

        var reverseOrder = position.SubmitOrder(reverseOrderSettings);
        if (reverseOrder.Error != null)
        {
            Assert.Fail(reverseOrder.Error);
        }

        if (BrokerSupportsPositionReversal)
        {
            await WaitForCompleteFillAsync(reverseOrder);
            Assert.AreEqual(SymbolScript.OpenPositions.Count, 1, "Expect one position open after reversal");
            var reversePosition = SymbolScript.OpenPositions[0];
            Assert.AreEqual(reversePosition.Type, PositionType.Short);
            Assert.AreEqual(reversePosition.CurrentSize, PositionSize);
        }
        else
        {
            var cancelReason = await WaitForOrderCompletedAsync(reverseOrder);
            Assert.AreEqual(reverseOrder.OrderState, BrokerOrderState.Rejected);
        }

    }
}
