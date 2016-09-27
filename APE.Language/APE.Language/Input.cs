﻿//
//Copyright 2016 David Beales
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using APE.Communication;
using System.Windows.Forms;
using NM = APE.Native.NativeMethods;

namespace APE.Language
{
    internal static class Input
    {
        public static int m_Count = 0;
        private static Process m_Process = null;
        private static int m_ProcessId = 0;
        private static bool m_MouseDown = false;

        private static void WaitToBeVisibleAndEnabled(IntPtr handle)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (true)
            {
                if (timer.ElapsedMilliseconds > GUI.GetTimeOut())
                {
                    throw new Exception("Control is not enabled");
                }

                if (NM.IsWindowEnabled(handle))
                {
                    if (NM.IsWindowVisible(handle))
                    {
                        break;
                    }
                }
            }
            timer.Stop();
        }

        public static void SendKeys(GUIFocusableObject focusableObject, IntPtr Handle, string text)
        {
            WaitToBeVisibleAndEnabled(Handle);
            if (!WaitForInputIdle(Handle, GUI.m_APE.TimeOut))
            {
                throw new Exception("Window did not go idle within timeout");
            }
            if (!focusableObject.HasFocus)
            {
                focusableObject.SetFocus();
            }
            TimerResolution.SetMaxTimerResolution();
            System.Windows.Forms.SendKeys.SendWait(text);
            TimerResolution.UnsetMaxTimerResolution();
        }

        public static void MouseSingleClick(IntPtr ParentHandle, IntPtr Handle, int X, int Y, MouseButton Button, MouseKeyModifier Keys)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(Handle);
            if (!WaitForInputIdle(Handle, GUI.m_APE.TimeOut))
            {
                throw new Exception("Window did not go idle within timeout");
            }

            m_MouseDown = false;
            uint DoubleClickTimer = (uint)SystemInformation.DoubleClickTime;
            Block(ParentHandle, Handle);
            try
            {
                TimerResolution.SetMaxTimerResolution();
                NM.SetDoubleClickTime(1);
                
                ClickCommon(ParentHandle, Handle, X, Y);

                GUI.m_APE.AddFirstMessageAddMouseHook(Handle);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                hooked = true;

                MouseClick(Button, true, false, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                // Some controls don't like it if the mouse is released too quick (For instance Listview
                // group selecting) but rather than slowing all clicks down put specific code in the problematic
                // control to handle it (see SelectGroup in ListView.cs for an example)

                MouseClick(Button, false, true, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, false, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
            }
            catch
            {
                NM.SetDoubleClickTime(DoubleClickTimer);    //Reset double click timer
                Reset();                                    //Reset the mouse blocking
                throw;
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(Handle);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    NM.SetDoubleClickTime(DoubleClickTimer);
                    Unblock();
                }
            }
        }

        public static void MouseDoubleClick(IntPtr ParentHandle, IntPtr Handle, int X, int Y, MouseButton Button, MouseKeyModifier Keys)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(Handle);
            if (!WaitForInputIdle(Handle, GUI.m_APE.TimeOut))
            {
                throw new Exception("Window did not go idle within timeout");
            }

            m_MouseDown = false;
            try
            {
                Block(ParentHandle, Handle);
                
                TimerResolution.SetMaxTimerResolution();

                ClickCommon(ParentHandle, Handle, X, Y);

                GUI.m_APE.AddFirstMessageAddMouseHook(Handle);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                hooked = true;

                MouseClick(Button, true, false, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, false, true, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, false, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, true, false, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, true, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, false, true, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, false, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
            }
            catch
            {
                Reset();    //Reset the mouse blocking
                throw;
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(Handle);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                }
            }
        }

        public static void MouseTripleClick(IntPtr ParentHandle, IntPtr Handle, int X, int Y, MouseButton Button, MouseKeyModifier Keys)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(Handle);
            if (!WaitForInputIdle(Handle, GUI.m_APE.TimeOut))
            {
                throw new Exception("Window did not go idle within timeout");
            }

            m_MouseDown = false;
            try
            {
                Block(ParentHandle, Handle);
                
                TimerResolution.SetMaxTimerResolution();

                ClickCommon(ParentHandle, Handle, X, Y);

                GUI.m_APE.AddFirstMessageAddMouseHook(Handle);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                hooked = true;

                MouseClick(Button, true, false, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, false, true, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, false, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, true, false, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, false, true, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, false, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, true, false, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, true, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(Button, false, true, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, false, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
            }
            catch
            {
                Reset();    //Reset the mouse blocking
                throw;
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(Handle);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                }
            }
        }

        public static void MouseDown(IntPtr ParentHandle, IntPtr Handle, int X, int Y, MouseButton Button, MouseKeyModifier Keys)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(Handle);
            if (!WaitForInputIdle(Handle, GUI.m_APE.TimeOut))
            {
                throw new Exception("Window did not go idle within timeout");
            }

            Block(ParentHandle, Handle);
            try
            {
                TimerResolution.SetMaxTimerResolution();

                ClickCommon(ParentHandle, Handle, X, Y);

                GUI.m_APE.AddFirstMessageAddMouseHook(Handle);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                hooked = true;

                MouseClick(Button, true, false, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
            }
            catch
            {
                Reset();    //Reset the mouse blocking
                throw;
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(Handle);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                    m_MouseDown = true;
                }
            }
        }

        public static void MouseUp(IntPtr ParentHandle, IntPtr Handle, int x, int y, MouseButton Button, MouseKeyModifier Keys)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(Handle);
            Block(ParentHandle, Handle);
            try
            {
                NM.tagPoint thePoint = new NM.tagPoint();
                thePoint.x = x;
                thePoint.y = y;
                IntPtr TopLevelHandle = NM.ChildWindowFromPoint(NM.GetDesktopWindow(), thePoint);

                NM.tagRect WindowSize;
                NM.GetWindowRect(Handle, out WindowSize);

                thePoint.x = x + WindowSize.left;
                thePoint.y = y + WindowSize.top;
                IntPtr ChildHandle = NM.WindowFromPoint(thePoint);

                if (!WaitForInputIdle(ChildHandle, GUI.m_APE.TimeOut))
                {
                    throw new Exception("Window did not go idle within timeout");
                }

                IntPtr ActualParent;

                if (ParentHandle == IntPtr.Zero)
                {
                    ActualParent = Handle;
                }
                else
                {
                    ActualParent = ParentHandle;
                }

                TimerResolution.SetMaxTimerResolution();

                //TODO this looks wrong should use clickcommon only for this
                if (ChildHandle == ActualParent)
                {
                    ClickCommon(ParentHandle, Handle, x, y);
                }
                else
                {
                    MouseMove(Handle, x, y, false);
                }

                if (Handle == ActualParent)
                {
                    GUI.m_APE.AddFirstMessageAddMouseHook(Handle);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    hooked = true;
                }

                MouseClick(Button, false, true, 1, Keys.HasFlag(MouseKeyModifier.Control), Keys.HasFlag(MouseKeyModifier.Shift));

                if (Handle == ActualParent)
                {
                    GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)Button, false, true);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }
            }
            catch
            {
                Reset();    //Reset the mouse blocking
                throw;
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(Handle);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                    m_MouseDown = false;
                }
            }
        }

        public static bool ActiveWindow(IntPtr Parent)
        {
            NM.GUITHREADINFO CurrentGuiInfo = new NM.GUITHREADINFO();
            CurrentGuiInfo.cbSize = Marshal.SizeOf(CurrentGuiInfo);
            NM.GetGUIThreadInfo(0, ref CurrentGuiInfo);

            if (CurrentGuiInfo.hwndActive == Parent)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool HasFocus(IntPtr Parent, IntPtr Control)
        {
            NM.GUITHREADINFO CurrentGuiInfo = new NM.GUITHREADINFO();
            CurrentGuiInfo.cbSize = Marshal.SizeOf(CurrentGuiInfo);
            NM.GetGUIThreadInfo(0, ref CurrentGuiInfo);

            //Debug.Listeners[0].WriteLine("\t CurrentGuiInfo.hwndActive: " + CurrentGuiInfo.hwndActive.ToString());
            //Debug.Listeners[0].WriteLine("\t CurrentGuiInfo.hwndFocus: " + CurrentGuiInfo.hwndFocus.ToString());

            if (Parent == IntPtr.Zero)
            {
                if (CurrentGuiInfo.hwndActive == Control)
                {
                    return true;
                }
            }
            else
            {
                if (CurrentGuiInfo.hwndActive == Parent)
                {

                    if (CurrentGuiInfo.hwndFocus == Control)
                    {
                        return true;
                    }
                    else if (NM.IsChild(Control, CurrentGuiInfo.hwndFocus))
                    {
                        return true;
                    }
                }
                else
                {
                    if (CurrentGuiInfo.hwndActive == Control)
                    {

                        if (CurrentGuiInfo.hwndFocus == Control)
                        {
                            return true;
                        }
                        else if (NM.IsChild(Control, CurrentGuiInfo.hwndFocus))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static IntPtr GetFocus()
        {
            NM.GUITHREADINFO CurrentGuiInfo = new NM.GUITHREADINFO();
            CurrentGuiInfo.cbSize = Marshal.SizeOf(CurrentGuiInfo);
            NM.GetGUIThreadInfo(0, ref CurrentGuiInfo);

            return CurrentGuiInfo.hwndFocus;
        }

        public static void SetFocus(IntPtr Parent, IntPtr Control)
        {
            Stopwatch timer = Stopwatch.StartNew();
            IntPtr ActualParent;
            
            if (Parent == IntPtr.Zero)
            {
                ActualParent = Control;
            }
            else
            {
                ActualParent = Parent;
            }

            if (!HasFocus(IntPtr.Zero, ActualParent))
            {
                NM.BringWindowToTop(ActualParent);

                if (!NM.SetForegroundWindow(ActualParent))
                {
                    Debug.WriteLine("Falling back to Hotkey method for parent");
                    // Fall back to the Hotkey (which will have SetForegroundWindow permission)
                    GUI.m_ViewPort.Foreground = ActualParent;

                    while (GUI.m_ViewPort.Foreground != IntPtr.Zero)
                    {
                        // Sendkeys won't work so use keybd_event (TODO could also use SendInput)
                        NM.keybd_event(NM.VK_PAUSE, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_PAUSE, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);

                        if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                        {
                            timer.Stop();
                            throw new Exception("Viewport SetForegroundWindow appeared to not trigger");
                        }

                        Thread.Sleep(0);
                    }
                }

                while (!HasFocus(IntPtr.Zero, ActualParent))
                {
                    if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                    {
                        timer.Stop();
                        throw new Exception("Failed to set focus to the toplevel window");
                    }

                    Thread.Sleep(0);
                }
            }

            if (Parent != IntPtr.Zero)
            {
                if (!HasFocus(Parent, Control))
                {
                    if (!NM.SetForegroundWindow(Control))
                    {
                        Debug.WriteLine("Falling back to Hotkey method for child");
                        // Fall back to the Hotkey (which will have SetForegroundWindow permission)
                        GUI.m_ViewPort.Foreground = Control;

                        // Sendkeys won't work so use keybd_event (TODO could also use SendInput)
                        NM.keybd_event(NM.VK_PAUSE, 0x8f, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_PAUSE, 0x8f, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);

                        while (GUI.m_ViewPort.Foreground != IntPtr.Zero)
                        {
                            if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                            {
                                timer.Stop();
                                throw new Exception("SetForegroundWindow failed to set focus to the window");
                            }

                            Thread.Sleep(0);
                        }
                    }

                    while (!HasFocus(Parent, Control))
                    {
                        if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                        {
                            timer.Stop();
                            throw new Exception("Failed to set focus to the window");
                        }
                        
                        Thread.Sleep(0);
                    }
                    timer.Stop();
                }
            }

            return;
        }

        public static void ClickCommon(IntPtr Parent, IntPtr Handle, int X, int Y)
        {
            if (NM.IsWindowVisible(Handle))
            {
                if (NM.IsWindowEnabled(Handle))
                {
                    IntPtr ActualParent;

                    if (Parent == IntPtr.Zero)
                    {
                        ActualParent = Handle;
                    }
                    else
                    {
                        ActualParent = Parent;
                    }

                    if (!ActiveWindow(ActualParent))
                    {
                        SetFocus(Parent, Handle);
                    }
                    else
                    {
                        NM.BringWindowToTop(ActualParent);
                    }

                    NM.tagPoint thePoint = MouseMove(Handle, X, Y);
                    IntPtr WindowAtPoint = NM.WindowFromPoint(thePoint);

                    if (WindowAtPoint != Handle)
                    {
                        throw new Exception("Window is obscured");
                    }
                }
                else
                {
                    throw new Exception("Window is not enabled");
                }
            }
            else
            {
                throw new Exception("Window is not visible");
            }
        }

        public static void Block(IntPtr ParentHandle, IntPtr ControlHandle)
        {
            if (m_Count == 0)
            {
                if (GUI.IsElevatedAdmin)
                {
                    bool Return = NM.BlockInput(true);
                    if (!Return)
                    {
                        throw new Exception("Failed to block input");
                    }

                    Byte[] state = new byte[256];

                    //dummy call to make the GetKeyboardState accurate
                    NM.GetKeyState(0);

                    //get the current thread keyboard state
                    NM.GetKeyboardState(state);

                    //reset the global mouse states
                    if (m_MouseDown == false)
                    {
                        if ((state[NM.VK_LBUTTON] & 0x80) != 0)
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                        }

                        if ((state[NM.VK_MBUTTON] & 0x80) != 0)
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                        }

                        if ((state[NM.VK_RBUTTON] & 0x80) != 0)
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                        }

                        if ((state[NM.VK_XBUTTON1] & 0x80) != 0)
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_XUP, 0, 0, 0, new UIntPtr(1));
                        }

                        if ((state[NM.VK_XBUTTON2] & 0x80) != 0)
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_XUP, 0, 0, 0, new UIntPtr(2));
                        }
                    }

                    // 3 is VK_CANCEL
                    if ((state[NM.VK_CANCEL] & 0x80) != 0)
                    {
                        NM.keybd_event(NM.VK_CANCEL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_CANCEL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }

                    // 7+ is non-mouse (apart from 3) and 255 seems to break things so we do till 254
                    for (byte vk = 7; vk < unchecked((byte)255); vk++)
                    {
                        if ((state[vk] & 0x80) != 0)
                        {
                            NM.keybd_event(vk, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                            NM.keybd_event(vk, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                        }
                    }

                    //check for toggle keys
                    if ((state[NM.VK_CAPITAL] & 0x1) != 0)
                    {
                        NM.keybd_event(NM.VK_CAPITAL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_CAPITAL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }

                    if ((state[NM.VK_NUMLOCK] & 0x1) != 0)
                    {
                        NM.keybd_event(NM.VK_NUMLOCK, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_NUMLOCK, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }

                    if ((state[NM.VK_SCROLL] & 0x1) != 0)
                    {
                        NM.keybd_event(NM.VK_SCROLL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_SCROLL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }

                    //make sure any key / mouse presses are detected
                    Application.DoEvents();                    
                }
            }

            m_Count++;
        }

        public static void Unblock()
        {
            if (m_Count > 0)
            {
                m_Count--;
            }

            if (m_Count == 0)
            {
                Reset();
            }
        }

        public static void Reset()
        {
            if (GUI.IsElevatedAdmin)
            {
                bool Return = NM.BlockInput(false);
            }

            m_Count = 0;
        }

        public static bool WaitForInputIdle(IntPtr handle, uint timeoutMs)
        {
            int processId;
            int threadId = NM.GetWindowThreadProcessId(handle, out processId);

            if (threadId == 0)
            {
                throw new Exception("Failed to get thread for window");
            }

            if (processId != m_ProcessId)
            {
                m_ProcessId = processId;
                m_Process = Process.GetProcessById(processId);
            }
            else
            {
                m_Process.Refresh();
            }

            Stopwatch timer = Stopwatch.StartNew();
            int x = 0;
            while (true)
            {
                ProcessThreadCollection threadCollection = m_Process.Threads;

                for (int i = 0; i < threadCollection.Count; i++)
                {
                    if (threadCollection[i].Id == threadId)
                    {
                        if (threadCollection[i].ThreadState == System.Diagnostics.ThreadState.Wait)
                        {
                            if (threadCollection[i].WaitReason == ThreadWaitReason.UserRequest)
                            {
                                x++;
                                if (x == 2) //Wait for it to match twice in a row as sometimes the process will go idle then immediately go not idle
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                x = 0;
                            }
                        }
                        else
                        {
                            x = 0;
                        }
                        break;
                    }
                }

                if (timer.ElapsedMilliseconds > timeoutMs)
                {
                    return false;
                }

                Thread.Sleep(0);
                m_Process.Refresh();
            }
        }

        private const int MoveSize = 4;

        public static NM.tagPoint MouseMove(IntPtr Handle, int x, int y, bool PerformCheck = true)
        {
            NM.tagRect WindowRect;
            NM.GetWindowRect(Handle, out WindowRect);

            NM.tagRect ClientRect;
            NM.GetClientRect(Handle, out ClientRect);

            //TODO fix this as -1 might be a valid move,,, maybe 0 instead or...

            int xOffset;
            if (x == -1)
            {
                xOffset = ClientRect.right / 2;
            }
            else
            {
                xOffset = x;
            }

            int yOffset;
            if (y == -1)
            {
                yOffset = ClientRect.bottom / 2;
            }
            else
            {
                yOffset = y;
            }

            //Convert the window area to screen point
            NM.tagPoint thePoint;
            thePoint.x = WindowRect.left + xOffset;
            thePoint.y = WindowRect.top + yOffset;

            if (NM.MonitorFromPoint(thePoint, NM.MonitorOptions.MONITOR_DEFAULTTONULL) == null)
            {
                throw new Exception("coordinate appears to be offscreen");
            }

            if (PerformCheck)
            {
                Stopwatch timer = Stopwatch.StartNew();
                while (true)
                {
                    IntPtr ChildHandle;

                    thePoint.x = xOffset + WindowRect.left;
                    thePoint.y = yOffset + WindowRect.top;

                    ChildHandle = NM.WindowFromPoint(thePoint);

                    //Make sure we are inside the controls window area
                    if (Handle == ChildHandle)
                    {
                        break;
                    }
                    else
                    {
                        if (timer.ElapsedMilliseconds > GUI.GetTimeOut())
                        {
                            throw new Exception("Coordinates are not inside the controls area");
                        }
                    }
                }
            }

            //Get the current mouse location
            NM.tagPoint currentPoint;
            NM.GetCursorPos(out currentPoint);

            //X direction
            int DirectionX;
            if (currentPoint.x <= WindowRect.left + xOffset)
            {
                DirectionX = 1;     //right
            }
            else
            {
                DirectionX = -1;    //left
            }

            //Y direction
            int DirectionY;
            if (currentPoint.y <= WindowRect.top + yOffset)
            {
                DirectionY = 1;     //down
            }
            else
            {
                DirectionY = -1;    //up
            }

            int MoveX = currentPoint.x;
            int MoveY = currentPoint.y;

            while (MoveX != WindowRect.left + xOffset || MoveY != WindowRect.top + yOffset)
            {
                if (MoveX != WindowRect.left + xOffset)
                {
                    if (DirectionX == 1)
                    {
                        if (MoveX + MoveSize > WindowRect.left + xOffset)
                        {
                            MoveX = MoveX + 1;
                        }
                        else
                        {
                            MoveX = MoveX + MoveSize;
                        }
                    }
                    else
                    {
                        if (MoveX - MoveSize < WindowRect.left + xOffset)
                        {
                            MoveX = MoveX - 1;
                        }
                        else
                        {
                            MoveX = MoveX - MoveSize;
                        }
                    }
                }

                if (MoveY != WindowRect.top + yOffset)
                {
                    if (DirectionY == 1)
                    {
                        if (MoveY + MoveSize > WindowRect.top + yOffset)
                        {
                            MoveY = MoveY + 1;
                        }
                        else
                        {
                            MoveY = MoveY + MoveSize;
                        }
                    }
                    else
                    {
                        if (MoveY - MoveSize < WindowRect.top + yOffset)
                        {
                            MoveY = MoveY - 1;
                        }
                        else
                        {
                            MoveY = MoveY - MoveSize;
                        }
                    }
                }

                MoveMouse(MoveX, MoveY);
            }

            return thePoint;
        }

        private static void MoveMouse(int x, int y)
        {
            NM.INPUT[] MouseEvent = new NM.INPUT[1];
            MouseEvent[0].type = NM.INPUT_TYPE.INPUT_MOUSE;
            MouseEvent[0].U.mi = CreateMouseInput(x, y, 0, 0, NM.MOUSEEVENTF.ABSOLUTE | NM.MOUSEEVENTF.MOVE);
            NM.SendInput((uint)MouseEvent.Length, MouseEvent, Marshal.SizeOf(MouseEvent[0].GetType()));
        }

        private static NM.KEYBDINPUT CreateKeyboardInput(NM.VirtualKeyShort wVK, NM.ScanCodeShort wScan, NM.KEYEVENTF dwFlags, uint time, UIntPtr dwExtraInfo)
        {
            NM.KEYBDINPUT Result = new NM.KEYBDINPUT();

            Result.wVk = wVK;
            Result.wScan = wScan;
            Result.dwFlags = dwFlags;
            Result.time = time;
            Result.dwExtraInfo = dwExtraInfo;

            return Result;
        }

        private static NM.MOUSEINPUT CreateMouseInput(int x, int y, uint data, uint time, NM.MOUSEEVENTF flag)
        {
            NM.MOUSEINPUT Result = new NM.MOUSEINPUT();
            double dx = ((double)x * GetNormaliseFactor(Direction.HORIZONTAL));
            double dy = ((double)y * GetNormaliseFactor(Direction.VERTICAL));

            Result.dx = Convert.ToInt32(dx);
            Result.dy = Convert.ToInt32(dy);
            Result.mouseData = data;
            Result.time = time;
            Result.dwFlags = flag;

            //MessageBox.Show(dx.ToString() + " " + Result.dx.ToString() + " " + dy.ToString() + " " + Result.dy.ToString());
            return Result;
        }

        private enum Direction : int
        {
            VERTICAL = 1,
            HORIZONTAL = 0
        }

        private static double GetNormaliseFactor(Direction Axis)
        {
            switch (Axis)
            {
                case Direction.HORIZONTAL:
                    return (double)65535 / NM.GetSystemMetrics(NM.SystemMetric.SM_CXSCREEN);
                case Direction.VERTICAL:
                    return (double)65535 / NM.GetSystemMetrics(NM.SystemMetric.SM_CYSCREEN);
            }
            return 0;
        }

        public static void MouseClick(MouseButton Button, Boolean Down, Boolean Up, int Clicks, bool ControlKey, bool ShiftKey)
        {
            NM.INPUT[] inputEvent = null;
            int Events = 0;

            if (Clicks != 1)
            {
                if (!Up)
                {
                    throw new Exception("Can only single click when using MouseDown");
                }

                if (!Down)
                {
                    throw new Exception("Can only single click when using MouseUp");
                }
            }

            if (Down)
            {
                if (ControlKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.CONTROL, NM.ScanCodeShort.CONTROL, NM.KEYEVENTF.NONE, 0, UIntPtr.Zero);
                }

                if (ShiftKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.SHIFT, NM.ScanCodeShort.SHIFT, NM.KEYEVENTF.NONE, 0, UIntPtr.Zero);
                }
            }

            //TODO pass in the x and y and set the absolute flag so we are 100% clicking in the right place
            for (int i = 0; i < Clicks; i++)
            {
                if (Down)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.Length - 1].type = NM.INPUT_TYPE.INPUT_MOUSE;

                    switch (Button)
                    {
                        case MouseButton.Left:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.LEFTDOWN);
                            break;
                        case MouseButton.Right:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.RIGHTDOWN);
                            break;
                        case MouseButton.Middle:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.MIDDLEDOWN);
                            break;
                    }
                }

                if (Up)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_MOUSE;

                    switch (Button)
                    {
                        case MouseButton.Left:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.LEFTUP);
                            break;
                        case MouseButton.Right:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.RIGHTUP);
                            break;
                        case MouseButton.Middle:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.MIDDLEUP);
                            break;
                    }
                }
            }

            if (Up)
            {
                if (ControlKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.CONTROL, NM.ScanCodeShort.CONTROL, NM.KEYEVENTF.KEYUP, 0, UIntPtr.Zero);
                }

                if (ShiftKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.SHIFT, NM.ScanCodeShort.SHIFT, NM.KEYEVENTF.KEYUP, 0, UIntPtr.Zero);
                }
            }

            NM.SendInput((uint)inputEvent.Length, inputEvent, NM.INPUT.Size);
        }
    }

    static internal class TimerResolution
    {
        private static bool m_ResolutionSet = false;
        private static uint m_MaximumResolution = 0;

        static TimerResolution()
        {
            uint minimumResolution;
            uint maximumResolution;
            uint currentResolution;

            NM.NtQueryTimerResolution(out minimumResolution, out maximumResolution, out currentResolution);

            m_MaximumResolution = maximumResolution;
        }

        public static void SetMaxTimerResolution()
        {
            if (!m_ResolutionSet)
            {
                uint currentResolution;
                uint result = NM.NtSetTimerResolution(m_MaximumResolution, true, out currentResolution);

                switch (result)
                {
                    case NM.STATUS_SUCCESS:
                        m_ResolutionSet = true;
                        break;
                    default:
                        break;
                }
            }
        }

        public static void UnsetMaxTimerResolution()
        {
            if (m_ResolutionSet)
            {
                uint currentResolution;
                uint result = NM.NtSetTimerResolution(m_MaximumResolution, false, out currentResolution);

                switch (result)
                {
                    case NM.STATUS_SUCCESS:
                        m_ResolutionSet = false;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
