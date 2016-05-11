#region Using statements
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RightEdge.Common;
using RightEdge.Common.ChartObjects;
using RightEdge.Indicators;
#endregion

#region System class
public class MySystem : MySystemBase
{
	public override void Startup()
	{
		// Perform initialization or set system wide options here

	}
}
#endregion

public class MySymbolScript : MySymbolScriptBase
{
    List<BrokerTest> _tests = new List<BrokerTest>();
    List<TestResult> _testResults = new List<TestResult>();

    BrokerTest CurrentTest => _tests.FirstOrDefault();
    Task _currentTestTask;

	public override void Startup()
	{
		//  Add tests here

	}

    void ProcessTests()
    {
        if (CurrentTest == null)
        {
            return;
        }
        if (_currentTestTask == null)
        {
            _currentTestTask = CurrentTest.StartTest();
        }
        if (_currentTestTask.IsCompleted)
        {
            TestResult result = new TestResult();
            result.Test = CurrentTest;
            if (_currentTestTask.IsCanceled)
            {
                result.Exception = new OperationCanceledException();
            }
            else if (_currentTestTask.IsFaulted)
            {
                result.Exception = _currentTestTask.Exception;
                while (result.Exception is AggregateException &&
                       ((AggregateException) result.Exception).InnerExceptions.Count == 1)
                {
                    result.Exception = ((AggregateException) result.Exception).InnerExceptions[0];
                }
            }

            _testResults.Add(result);
            _tests.RemoveAt(0);
            _currentTestTask = null;
            ReportResult(result);
        }
    }

    void ReportResult(TestResult result)
    {
        if (result.Passed)
        {
            OutputMessage("Test passed: " + result.TestName);
        }
        else
        {
            OutputError("Test failed: " + result.TestName + " - " + result.Exception.Message);
        }
    }

    public override void NewTick(BarData partialBar, TickData tick)
    {
        if (CurrentTest != null)
        {
            CurrentTest.ProcessEvent(new NewTickEvent(tick, partialBar));
        }

        ProcessTests();
    }

    public override void NewBar()
	{
        if (CurrentTest != null)
        {
            CurrentTest.ProcessEvent(new NewBarEvent(Bars.Current));
        }
	}

	public override void OrderFilled(Position position, Trade trade)
	{
	    if (CurrentTest != null)
	    {
	        CurrentTest.ProcessEvent(new OrderFilledEvent(position, trade));
	    }
	}

	public override void OrderCancelled(Position position, Order order, string information)
	{
	    if (CurrentTest != null)
	    {
	        CurrentTest.ProcessEvent(new OrderCancelledEvent(position, order, information));
	    }
	}
}

class TestResult
{
    public BrokerTest Test { get; set; }
    public string TestName => Test.TestName;
    public Exception Exception { get; set; }
    public bool Passed => Exception == null;
    
}