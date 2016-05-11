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

    BrokerTest _currentTest;
    Task _currentTestTask;

	public override void Startup()
	{
		//  Add tests

	    _tests.Add(new ReversePositionTest(this) {BrokerSupportsPositionReversal = true});

	    _tests.Add(new SimpleBuyTest(this));

        //  This test should fail
        _tests.Add(new SimpleBuyTest(this) { PositionSize = -100});

        _tests.Add(new SimpleBuyTest(this));
	}

    void ProcessTests()
    {
        if (_currentTest == null && _tests.Count == 0)
        {
            return;
        }

        if (_currentTest == null)
        {
            //  Wait for positions from any previous test to be closed before starting the next test
            if (OpenPositions.Count == 0 && PendingPositions.Count == 0)
            {
                _currentTest = _tests[0];
                _tests.RemoveAt(0);
                OutputMessage("Starting test: " + _currentTest.TestName);
                _currentTestTask = _currentTest.StartTest();
            }
        }

        if (_currentTest != null)
        {
            if (_currentTestTask.IsCompleted)
            {
                TestResult result = new TestResult();
                result.Test = _currentTest;
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
                _currentTest = null;
                _currentTestTask = null;
                ReportResult(result);

                //  Close any positions left open by the test
                PositionManager.CloseAllPositions(Symbol);
            }
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
        if (_currentTest != null)
        {
            _currentTest.ProcessEvent(new NewTickEvent(tick, partialBar));
        }

        ProcessTests();
    }

    public override void NewBar()
	{
        if (_currentTest != null)
        {
            _currentTest.ProcessEvent(new NewBarEvent(Bars.Current));
        }
	}

	public override void OrderFilled(Position position, Trade trade)
	{
	    if (_currentTest != null)
	    {
	        _currentTest.ProcessEvent(new OrderFilledEvent(position, trade));
	    }
	}

	public override void OrderCancelled(Position position, Order order, string information)
	{
	    if (_currentTest != null)
	    {
	        _currentTest.ProcessEvent(new OrderCancelledEvent(position, order, information));
	    }
	}
}

class TestResult
{
    public BrokerTest Test { get; set; }
    public string TestName { get { return Test.TestName; } }
    public Exception Exception { get; set; }
    public bool Passed { get { return Exception == null; } }
    
}
