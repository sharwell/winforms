// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace System.Windows.Forms.Tests
{
    public class SendInput
    {
        internal async Task SendAsync(Form window, params object[] keys)
        {
            await SendAsync(window, inputSimulator =>
            {
                foreach (var key in keys)
                {
                    switch (key)
                    {
                    case string str:
                        var text = str.Replace("\r\n", "\r").Replace("\n", "\r");
                        int index = 0;
                        while (index < text.Length)
                        {
                            if (text[index] == '\r')
                            {
                                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                                index++;
                            }
                            else
                            {
                                int nextIndex = text.IndexOf('\r', index);
                                if (nextIndex == -1)
                                {
                                    nextIndex = text.Length;
                                }

                                inputSimulator.Keyboard.TextEntry(text.Substring(index, nextIndex - index));
                                index = nextIndex;
                            }
                        }

                        break;

                    case char c:
                        inputSimulator.Keyboard.TextEntry(c);
                        break;

                    case VirtualKeyCode virtualKeyCode:
                        inputSimulator.Keyboard.KeyPress(virtualKeyCode);
                        break;

                    case null:
                        throw new ArgumentNullException(nameof(keys));

                    default:
                        throw new ArgumentException($"Unexpected type encountered: {key.GetType()}", nameof(keys));
                    }
                }
            });
        }

        internal async Task SendAsync(Form window, Action<InputSimulator> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            var foregroundWindow = IntPtr.Zero;

            try
            {
                var foreground = GetForegroundWindow();
                SetForegroundWindow(window.Handle);

                await Task.Run(() => actions(new InputSimulator()));
            }
            finally
            {
                if (foregroundWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(foregroundWindow);
                }
            }
        }

        private static bool AttachThreadInput(int idAttach, int idAttachTo)
        {
            var success = UnsafeNativeMethods.AttachThreadInput(idAttach, idAttachTo, true);
            if (!success)
            {
                var hresult = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hresult);
            }

            return success;
        }

        private static bool DetachThreadInput(int idAttach, int idAttachTo)
        {
            var success = UnsafeNativeMethods.AttachThreadInput(idAttach, idAttachTo, false);
            if (!success)
            {
                var hresult = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hresult);
            }

            return success;
        }

        private static IntPtr GetForegroundWindow()
        {
            // Attempt to get the foreground window in a loop, as the NativeMethods function can return IntPtr.Zero
            // in certain circumstances, such as when a window is losing activation.
            var foregroundWindow = IntPtr.Zero;

            do
            {
                foregroundWindow = UnsafeNativeMethods.GetForegroundWindow();
            }
            while (foregroundWindow == IntPtr.Zero);

            return foregroundWindow;
        }

        private static void SetForegroundWindow(IntPtr window, bool skipAttachingThread = false)
        {
            var foregroundWindow = GetForegroundWindow();

            if (window == foregroundWindow)
            {
                // Make the window a top-most window so it will appear above any existing top-most windows
                SafeNativeMethods.SetWindowPos(new HandleRef(null, window), new HandleRef(null, (IntPtr)NativeMethods.HWND_TOPMOST), 0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE);

                // Move the window into the foreground as it may not have been achieved by the 'SetWindowPos' call
                var success = UnsafeNativeMethods.SetForegroundWindow(new HandleRef(null, window));
                if (!success)
                {
                    throw new InvalidOperationException("Setting the foreground window failed.");
                }

                // Ensure the window is 'Active' as it may not have been achieved by 'SetForegroundWindow'
                UnsafeNativeMethods.SetActiveWindow(new HandleRef(null, window));

                // Give the window the keyboard focus as it may not have been achieved by 'SetActiveWindow'
                UnsafeNativeMethods.SetFocus(new HandleRef(null, window));

                // Remove the 'Top-Most' qualification from the window
                SafeNativeMethods.SetWindowPos(new HandleRef(null, window), new HandleRef(null, (IntPtr)NativeMethods.HWND_NOTOPMOST), 0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE);

                return;
            }

            throw new NotSupportedException();
            //var activeThreadId = SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, foregroundWindow), out _);
            //var currentThreadId = SafeNativeMethods.GetCurrentThreadId();

            //var threadInputsAttached = false;

            //try
            //{
            //    // No need to re-attach threads in case when VS initializaed an UI thread for a debugged application.
            //    if (!skipAttachingThread)
            //    {
            //        // Attach the thread inputs so that 'SetActiveWindow' and 'SetFocus' work
            //        threadInputsAttached = AttachThreadInput(currentThreadId, activeThreadId);
            //    }

            //    // Make the window a top-most window so it will appear above any existing top-most windows
            //    SafeNativeMethods.SetWindowPos(new HandleRef(null, window), new HandleRef(null, (IntPtr)NativeMethods.HWND_TOPMOST), 0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE);

            //    // Move the window into the foreground as it may not have been achieved by the 'SetWindowPos' call
            //    var success = UnsafeNativeMethods.SetForegroundWindow(new HandleRef(null, window));
            //    if (!success)
            //    {
            //        throw new InvalidOperationException("Setting the foreground window failed.");
            //    }

            //    // Ensure the window is 'Active' as it may not have been achieved by 'SetForegroundWindow'
            //    UnsafeNativeMethods.SetActiveWindow(new HandleRef(null, window));

            //    // Give the window the keyboard focus as it may not have been achieved by 'SetActiveWindow'
            //    UnsafeNativeMethods.SetFocus(new HandleRef(null, window));

            //    // Remove the 'Top-Most' qualification from the window
            //    SafeNativeMethods.SetWindowPos(new HandleRef(null, window), new HandleRef(null, (IntPtr)NativeMethods.HWND_NOTOPMOST), 0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE);
            //}
            //finally
            //{
            //    if (threadInputsAttached)
            //    {
            //        // Finally, detach the thread inputs from eachother
            //        DetachThreadInput(currentThreadId, activeThreadId);
            //    }
            //}
        }
    }
}
