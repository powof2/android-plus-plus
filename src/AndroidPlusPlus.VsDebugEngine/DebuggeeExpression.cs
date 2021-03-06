﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  // 
  // This class represents a succesfully parsed expression to the debugger. 
  // It is returned as a result of a successful call to IDebugExpressionContext2.ParseText
  // It allows the debugger to obtain the values of an expression in the debuggee. 
  // For the purposes of this sample, this means obtaining the values of locals and parameters from a stack frame.
  // 

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebuggeeExpression : IDebugExpression2
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected readonly DebugEngine m_debugEngine;

    protected readonly DebuggeeStackFrame m_stackFrame;

    protected readonly string m_expression;

    protected readonly uint m_radix;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeExpression (DebugEngine engine, DebuggeeStackFrame stackFrame, string expression, uint radix)
    {
      m_debugEngine = engine;

      m_stackFrame = stackFrame;

      m_expression = expression;

      m_radix = radix;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugExpression2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EvaluateAsync (enum_EVALFLAGS evaluateFlags, IDebugEventCallback2 eventCallback)
    {
      // 
      // Evaluate the expression asynchronously.
      // Method should return immediately after starting the expression evaluation, when completed successfully
      // a 'IDebugExpressionEvaluationCompleteEvent2' event should be sent to the provided IDebugEventCallback2 callback.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        ThreadPool.QueueUserWorkItem (delegate (object state)
        {
          try
          {
            IDebugProperty2 result;

            IDebugThread2 thread;

            LoggingUtils.RequireOk (m_stackFrame.GetThread (out thread));

            LoggingUtils.RequireOk (EvaluateSync (evaluateFlags, 0, eventCallback, out result));

            m_debugEngine.Broadcast (eventCallback, new DebugEngineEvent.ExpressionEvaluationComplete (this, result), m_debugEngine.Program, thread);
          }
          catch (Exception e)
          {
            LoggingUtils.HandleException (e);
          }
        });

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EvaluateSync (enum_EVALFLAGS evaluateFlags, uint timeout, IDebugEventCallback2 eventCallback, out IDebugProperty2 result)
    {
      // 
      // Evaluate the expression synchronously.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        CLangDebuggeeStackFrame stackFrame = m_stackFrame as CLangDebuggeeStackFrame;

        result = stackFrame.EvaluateCustomExpression (evaluateFlags, m_expression, m_radix);

        if (result == null)
        {
          return Constants.E_FAIL;
        }

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        result = null;

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Abort ()
    {
      // 
      // Cancel the asynchronous expression evaluation as started by a call to IDebugExpression2::EvaluateAsync.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
