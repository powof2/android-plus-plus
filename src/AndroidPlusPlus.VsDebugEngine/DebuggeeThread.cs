﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.VisualStudio.Debugger.Interop;

using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebuggeeThread : IDebugThread2, IDebugThread100
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class Enumerator : DebugEnumerator<IDebugThread2, IEnumDebugThreads2>, IEnumDebugThreads2
    {
      public Enumerator (List<IDebugThread2> threads)
        : base (threads)
      {
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Specified here to avoid referencing EnvDTE90 assembly.
    public enum enum_THREADCATEGORY
    {
      THREADCATEGORY_Worker = 0,
      THREADCATEGORY_UI = (THREADCATEGORY_Worker + 1),
      THREADCATEGORY_Main = (THREADCATEGORY_UI + 1),
      THREADCATEGORY_RPC = (THREADCATEGORY_Main + 1),
      THREADCATEGORY_Unknown = (THREADCATEGORY_RPC + 1)
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected readonly DebuggeeProgram m_debugProgram;

    protected readonly uint m_threadId;

    protected string m_threadName;

    protected string m_threadDisplayName;

    protected bool m_threadRunning;

    protected uint m_threadSuspendCount;

    protected uint m_threadFlags;

    protected List<DebuggeeStackFrame> m_threadStackFrames;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeThread (DebuggeeProgram program, uint threadId, string threadName)
    {
      m_debugProgram = program;

      m_threadId = threadId;

      m_threadName = threadName;

      if (string.IsNullOrEmpty (threadName))
      {
        m_threadName = "Thread-" + m_threadId;
      }

      m_threadDisplayName = m_threadName;

      m_threadRunning = true;

      m_threadSuspendCount = 0;

      m_threadStackFrames = new List<DebuggeeStackFrame> ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetRunning (bool isRunning)
    {
      LoggingUtils.PrintFunction ();

      m_threadRunning = isRunning;

      if (!isRunning)
      {
        ++m_threadSuspendCount;

        // 
        // Invalidate the current stack-trace.
        // 

        lock (m_threadStackFrames)
        {
          foreach (DebuggeeStackFrame frame in m_threadStackFrames)
          {
            frame.Delete ();
          }

          m_threadStackFrames.Clear ();
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void Refresh ()
    {
      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual List<DebuggeeStackFrame> StackTrace ()
    {
      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugThread2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int CanSetNextStatement (IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
    {
      // 
      // Determines whether the next statement can be set to the given stack frame and code context.
      // 

      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.S_OK; 
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumFrameInfo (enum_FRAMEINFO_FLAGS requestedFields, uint radix, out IEnumDebugFrameInfo2 enumDebugFrame)
    {
      // 
      // Retrieves a list of the stack frames for this thread.
      // For the sample engine, enumerating the stack frames requires walking the callstack in the debuggee for this thread
      // and converting that to an implementation of IEnumDebugFrameInfo2. 
      // Real engines will most likely want to cache this information to avoid recomputing it each time it is asked for,
      // and or construct it on demand instead of walking the entire stack.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        StackTrace ();

        DebuggeeStackFrame [] stackFrames = m_threadStackFrames.ToArray ();

        FRAMEINFO [] frameInfo = new FRAMEINFO [stackFrames.Length];

        for (int i = 0; i < stackFrames.Length; ++i)
        {
          LoggingUtils.RequireOk (stackFrames [i].SetFrameInfo (requestedFields, radix, ref frameInfo [i]));
        }

        enumDebugFrame = new DebuggeeStackFrame.Enumerator (frameInfo);

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        enumDebugFrame = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetName (out string threadName)
    {
      // 
      // Get the name of the thread.
      // 

      LoggingUtils.PrintFunction ();

      threadName = m_threadName;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetProgram (out IDebugProgram2 program)
    {
      LoggingUtils.PrintFunction ();

      program = m_debugProgram;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetThreadId (out uint threadId)
    {
      LoggingUtils.PrintFunction ();

      threadId = m_threadId;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetThreadProperties (enum_THREADPROPERTY_FIELDS requestedFields, THREADPROPERTIES [] propertiesArray)
    {
      // 
      // Gets properties that describe a thread.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        if ((requestedFields & enum_THREADPROPERTY_FIELDS.TPF_ID) != 0)
        {
          propertiesArray [0].dwThreadId = m_threadId;

          propertiesArray [0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_ID;
        }

        if ((requestedFields & enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT) != 0)
        {
          propertiesArray [0].dwSuspendCount = m_threadSuspendCount;

          propertiesArray [0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT;
        }

        if ((requestedFields & enum_THREADPROPERTY_FIELDS.TPF_STATE) != 0)
        {
          if (m_threadRunning)
          {
            propertiesArray [0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
          }
          else
          {
            propertiesArray [0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_STOPPED;
          }

          propertiesArray [0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_STATE;
        }

        if ((requestedFields & enum_THREADPROPERTY_FIELDS.TPF_PRIORITY) != 0)
        {
          propertiesArray [0].bstrPriority = "<unknown priority>";

          propertiesArray [0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_PRIORITY;
        }

        if ((requestedFields & enum_THREADPROPERTY_FIELDS.TPF_NAME) != 0)
        {
          propertiesArray [0].bstrName = m_threadName;

          propertiesArray [0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_NAME;
        }

        if ((requestedFields & enum_THREADPROPERTY_FIELDS.TPF_LOCATION) != 0)
        {
          // 
          // The thread location (usually the topmost stack frame), typically expressed as the name of the method where execution is currently halted.
          // 

          propertiesArray [0].bstrLocation = "[External Code]";

          StackTrace ();

          DebuggeeStackFrame [] stackFrames = m_threadStackFrames.ToArray ();

          foreach (DebuggeeStackFrame stackFrame in stackFrames)
          {
            FRAMEINFO frameInfo = new FRAMEINFO ();

            LoggingUtils.RequireOk (stackFrame.SetFrameInfo (enum_FRAMEINFO_FLAGS.FIF_FUNCNAME, 0, ref frameInfo));

            if (!string.IsNullOrEmpty (frameInfo.m_bstrFuncName))
            {
              propertiesArray [0].bstrLocation = frameInfo.m_bstrFuncName;

              break;
            }
          }

          propertiesArray [0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_LOCATION;
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int Resume (out uint suspendCount)
    {
      // 
      // Resume a thread.
      // This is called when the user chooses "Unfreeze" from the threads window when a thread has previously been frozen.
      // 

      LoggingUtils.PrintFunction ();

      suspendCount = --m_threadSuspendCount;

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int Suspend (out uint suspendCount)
    {
      // 
      // Suspend a thread.
      // This is called when the user chooses "Freeze" from the threads window.
      // 

      LoggingUtils.PrintFunction ();

      suspendCount = ++m_threadSuspendCount;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int SetNextStatement (IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
    {
      // 
      // Sets the next statement to the given stack frame and code context.
      // 

      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetLogicalThread (IDebugStackFrame2 stackFrame, out IDebugLogicalThread2 logicalThread)
    {
      // 
      // Gets the logical thread associated with this physical thread. Not implemented.
      // 

      LoggingUtils.PrintFunction ();

      logicalThread = null;

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int SetThreadName (string name)
    {
      // 
      // Sets the name of the thread. Not implemented.
      // 

      LoggingUtils.PrintFunction ();

      m_threadName = name;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugThread100 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    int IDebugThread100.GetThreadDisplayName (out string name)
    {
      LoggingUtils.PrintFunction ();

      name = m_threadDisplayName;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    int IDebugThread100.SetThreadDisplayName (string name)
    {
      LoggingUtils.PrintFunction ();

      m_threadDisplayName = name;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    int IDebugThread100.CanDoFuncEval ()
    {
      // 
      // Returns whether this thread can be used to do function/property evaluation.
      // 

      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    int IDebugThread100.GetFlags (out uint flags)
    {
      // 
      // Get flags. Not implemented.
      // 

      LoggingUtils.PrintFunction ();

      flags = m_threadFlags;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    int IDebugThread100.SetFlags (uint flags)
    {
      // 
      // Set flags. 
      // 

      LoggingUtils.PrintFunction ();

      m_threadFlags = flags;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    int IDebugThread100.GetThreadProperties100 (uint requestedFields, THREADPROPERTIES100 [] propertiesArray)
    {
      // 
      // Gets properties that describe a thread.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        // 
        // 9.0 (2008) thread properties.
        // 

        THREADPROPERTIES [] threadProperties9 = new THREADPROPERTIES [1];

        enum_THREADPROPERTY_FIELDS requestedFields90 = (enum_THREADPROPERTY_FIELDS)0x3f;// (requestedFields & 0x3f);

        LoggingUtils.RequireOk (GetThreadProperties (requestedFields90, threadProperties9));

        propertiesArray [0].bstrLocation = threadProperties9 [0].bstrLocation;

        propertiesArray [0].bstrName = threadProperties9 [0].bstrName;

        propertiesArray [0].bstrPriority = threadProperties9 [0].bstrPriority;

        propertiesArray [0].dwSuspendCount = threadProperties9 [0].dwSuspendCount;

        propertiesArray [0].dwThreadId = threadProperties9 [0].dwThreadId;

        propertiesArray [0].dwThreadState = threadProperties9 [0].dwThreadState;

        propertiesArray [0].dwFields |= (uint)threadProperties9 [0].dwFields;

        // 
        // 10.0 (2010) thread properties.
        // 

        if ((requestedFields & (uint) enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME) != 0)
        {
          propertiesArray [0].bstrDisplayName = m_threadDisplayName;

          propertiesArray [0].dwFields |= (uint) enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME;

          propertiesArray [0].DisplayNamePriority = (uint) enum_DISPLAY_NAME_PRIORITY100.DISPLAY_NAM_PRI_DEFAULT_100;

          propertiesArray [0].dwFields |= (uint) enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME_PRIORITY;
        }

        if ((requestedFields & (uint) enum_THREADPROPERTY_FIELDS100.TPF100_CATEGORY) != 0)
        {
          if (m_threadId == 1)
          {
            propertiesArray [0].dwThreadCategory = (uint) enum_THREADCATEGORY.THREADCATEGORY_Main;
          }
          else
          {
            propertiesArray [0].dwThreadCategory = (uint) enum_THREADCATEGORY.THREADCATEGORY_Worker;
          }

          propertiesArray [0].dwFields |= (uint) enum_THREADPROPERTY_FIELDS100.TPF100_CATEGORY;
        }

        if ((requestedFields & (uint) enum_THREADPROPERTY_FIELDS100.TPF100_AFFINITY) != 0)
        {
          propertiesArray [0].AffinityMask = 0;

          propertiesArray [0].dwFields |= (uint) enum_THREADPROPERTY_FIELDS100.TPF100_AFFINITY;
        }

        if ((requestedFields & (uint) enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY_ID) != 0)
        {
          propertiesArray [0].priorityId = 0;

          propertiesArray [0].dwFields |= (uint) enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY_ID;
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
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
