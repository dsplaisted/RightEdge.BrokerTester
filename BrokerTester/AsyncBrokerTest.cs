using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightEdge.Common;

abstract class AsyncBrokerTest : BrokerTest
{
    TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
    List<WaitingEvent> _waitingEvents = new List<WaitingEvent>();

    public AsyncBrokerTest(MySymbolScript symbolScript) : base(symbolScript)
    {
    }

    protected abstract Task RunTestAsync();

    public override Task StartTest()
    {
        return RunTestAsync();
    }

    public override void ProcessEvent(object @event)
    {
        //  Create copy of waiting events list because processing them may mean adding to the list
        foreach (var waitingEvent in _waitingEvents.ToList())
        {
            waitingEvent.ProcessEvent(@event);
        }
        _waitingEvents.RemoveAll(waitingEvent => waitingEvent.Task.IsCompleted);
    }

    public Task<T> WaitForAsync<T>()
    {
        var waitingEvent = new WaitingEvent<T>(@event => @event is T, @event => (T) @event);
        _waitingEvents.Add(waitingEvent);
        return waitingEvent.TaskOfT;
    }

    public Task<object> WaitForAnyAsync(params Type[] types)
    {
        var waitingEvent = new WaitingEvent<object>(
            @event => types.Any(t => t.IsInstanceOfType(@event)),
            @event => @event);
        _waitingEvents.Add(waitingEvent);
        return waitingEvent.TaskOfT;
    }

    //  Waits for an order to be "completed" - ie either cancelled or completely filled
    //  If order is filled successfully, result will be null, otherwise it will be the "information"
    //  about why the order was cancelled
    public async Task<string> WaitForOrderCompletedAsync(Order order)
    {
        while (order.OrderState == BrokerOrderState.Submitted || order.OrderState == BrokerOrderState.PartiallyFilled)
        {
            var result = await WaitForAnyAsync(typeof (OrderFilledEvent), typeof (OrderCancelledEvent));
            if (result is OrderCancelledEvent)
            {
                var orderCancelled = (OrderCancelledEvent)result;
                if (orderCancelled.Order == order)
                {
                    return orderCancelled.Information ?? "Unexpected cancel";
                }
            }
        }

        Assert.AreEqual(order.OrderState, BrokerOrderState.Filled);
        Assert.AreEqual(order.Fills.Sum(f => f.Quantity), order.Size, "Total fill size");
        return null;
    }

    public async Task WaitForCompleteFillAsync(Order order)
    {
        var cancelReason = await WaitForOrderCompletedAsync(order);
        if (cancelReason != null)
        {
            Assert.Fail("Unexpected cancel: " + cancelReason);
        }
    }

    abstract class WaitingEvent
    {
        public abstract Task Task { get; }
        public abstract void ProcessEvent(object @event);
    }

    class WaitingEvent<T> : WaitingEvent
    {
        readonly TaskCompletionSource<T> _taskCompletionSource;
        readonly Func<object, bool> _matcher;
        readonly Func<object, T> _resultGetter;

        public override Task Task { get { return _taskCompletionSource.Task; } }
        public Task<T> TaskOfT { get { return _taskCompletionSource.Task; } }

        public WaitingEvent(Func<object, bool> matcher, Func<object, T> resultGetter)
        {
            _taskCompletionSource = new TaskCompletionSource<T>();

            _matcher = matcher;
            _resultGetter = resultGetter;
        }

        public override void ProcessEvent(object @event)
        {
            if (_matcher(@event))
            {
                _taskCompletionSource.SetResult(_resultGetter(@event));
            }
        }
    }
}
