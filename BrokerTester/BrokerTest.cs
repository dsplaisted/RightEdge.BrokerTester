﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightEdge.Common;

abstract class BrokerTest
{
    public MySymbolScript SymbolScript { get; }

    protected BrokerTest(MySymbolScript symbolScript)
    {
        SymbolScript = symbolScript;
    }

    public abstract Task StartTest();
    public abstract void ProcessEvent(object @event);

    //  Default to use the class name as the test name, but allow overriding it
    public virtual string TestName => this.GetType().FullName;
}

class NewTickEvent
{
    public TickData Tick { get; }
    public BarData PartialBar { get; }

    public NewTickEvent(TickData tick, BarData partialBar)
    {
        Tick = tick;
        PartialBar = partialBar;
    }
}

class NewBarEvent
{
    public BarData Bar { get; }

    public NewBarEvent(BarData bar)
    {
        Bar = bar;
    }
}

class OrderFilledEvent
{
    public Position Position { get; }
    public Trade Trade { get; }

    public OrderFilledEvent(Position position, Trade trade)
    {
        Position = position;
        Trade = trade;
    }
}

class OrderCancelledEvent
{
    public Position Position { get; }
    public Order Order { get; }
    public string Information { get; }

    public OrderCancelledEvent(Position position, Order order, string information)
    {
        Position = position;
        Order = order;
        Information = information;
    }
}