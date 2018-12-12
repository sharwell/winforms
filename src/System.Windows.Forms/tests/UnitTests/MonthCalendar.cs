// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;

namespace System.Windows.Forms.Tests
{
    public class MonthCalendarTests : ControlTestBase
    {
        public MonthCalendarTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void MonthCalendar_Constructor()
        {
            var mc = new MonthCalendar();

            Assert.NotNull(mc);
            Assert.True(mc.TabStop);
        }

        [WinFormsFact]
        public void ClickToNextMonth()
        {
            RunMonthCalendarTest(async (window, control) =>
            {
                control.TodayDate = new DateTime(2018, 12, 8);
                control.SetDate(new DateTime(2018, 12, 8));

                Assert.Equal(new DateTime(2018, 12, 1), control.GetDisplayRange(visible: true).Start);

                // Find the position of the 'Next' button
                var rect = GetCalendarGridRect(control, NativeMethods.MCGIP_NEXT);

                // Move the mouse to the center of the 'Next' button
                var centerOfRect = new Point(rect.Left, rect.Top) + new Size(rect.Width / 2, rect.Height / 2);
                var centerOnScreen = control.PointToScreen(centerOfRect);
                await MoveMouseAsync(window, centerOnScreen);

                TaskCompletionSource<VoidResult> dateChanged = new TaskCompletionSource<VoidResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                control.DateChanged += (sender, e) => dateChanged.TrySetResult(default);

                await InputSimulator.SendAsync(
                    window,
                    inputSimulator => inputSimulator.Mouse.LeftButtonClick());

                await dateChanged.Task;

                // Verify that the next month is selected
                Assert.Equal(new DateTime(2019, 1, 1), control.GetDisplayRange(visible: true).Start);
            });
        }

        [WinFormsFact]
        public void ClickToPreviousMonth()
        {
            RunMonthCalendarTest(async (window, control) =>
            {
                control.TodayDate = new DateTime(2018, 12, 8);
                control.SetDate(new DateTime(2018, 12, 8));

                Assert.Equal(new DateTime(2018, 12, 1), control.GetDisplayRange(visible: true).Start);

                // Find the position of the 'Previous' button
                var rect = GetCalendarGridRect(control, NativeMethods.MCGIP_PREV);

                // Move the mouse to the center of the 'Previous' button
                var centerOfRect = new Point(rect.Left, rect.Top) + new Size(rect.Width / 2, rect.Height / 2);
                var centerOnScreen = control.PointToScreen(centerOfRect);
                await MoveMouseAsync(window, centerOnScreen);

                TaskCompletionSource<VoidResult> dateChanged = new TaskCompletionSource<VoidResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                control.DateChanged += (sender, e) => dateChanged.TrySetResult(default);

                await InputSimulator.SendAsync(
                    window,
                    inputSimulator => inputSimulator.Mouse.LeftButtonClick());

                await dateChanged.Task;

                // Verify that the previous month is selected
                Assert.Equal(new DateTime(2018, 11, 1), control.GetDisplayRange(visible: true).Start);
            });
        }

        private async Task MoveMouseAsync(Form window, Point point)
        {
            TestOutputHelper.WriteLine($"Moving mouse to ({point.X}, {point.Y}).");
            int horizontalResolution = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int verticalResolution = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            var virtualPoint = new Point((int)Math.Round((65535.0 / horizontalResolution) * point.X), (int)Math.Round((65535.0 / verticalResolution) * point.Y));
            TestOutputHelper.WriteLine($"Screen resolution of ({horizontalResolution}, {verticalResolution}) translates mouse to ({virtualPoint.X}, {virtualPoint.Y}).");

            await InputSimulator.SendAsync(window, inputSimulator => inputSimulator.Mouse.MoveMouseTo(virtualPoint.X + 1, virtualPoint.Y + 1));
            await WaitForIdleAsync();

            // ⚠ The call to GetCursorPos is required for correct behavior.
            var actualPoint = new NativeMethods.POINT();
            if (!UnsafeNativeMethods.GetCursorPos(actualPoint))
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            if (actualPoint.x != point.X || actualPoint.y != point.Y)
            {
                // Wait and try again
                await Task.Delay(15);
                if (!UnsafeNativeMethods.GetCursorPos(actualPoint))
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            Assert.Equal(point, new Point(actualPoint.x, actualPoint.y));
        }

        private static Rectangle GetCalendarGridRect(MonthCalendar control, uint part)
        {
            NativeMethods.MCGRIDINFO result = default;
            result.cbSize = Marshal.SizeOf<NativeMethods.MCGRIDINFO>();
            result.dwPart = part;
            result.dwFlags = NativeMethods.MCGIF_RECT;

            Assert.NotEqual(IntPtr.Zero, UnsafeNativeMethods.SendMessage(new HandleRef(control, control.Handle), NativeMethods.MCM_GETCALENDARGRIDINFO, 0, ref result));
            var rect = Rectangle.FromLTRB(result.rc.left, result.rc.top, result.rc.right, result.rc.bottom);
            return rect;
        }

        private void RunMonthCalendarTest(Func<Form, MonthCalendar, Task> testDriverAsync)
        {
            RunForm(
                () =>
                {
                    var form = new Form();
                    form.TopMost = true;

                    var control = new MonthCalendar();
                    control.Location = new Point(5, 5);
                    control.Name = "MyControl";
                    form.Controls.Add(control);

                    return (form, control);
                },
                testDriverAsync);
        }

        private void RunForm<T>(Func<(Form dialog, T control)> showDialog, Func<Form, T, Task> testDriverAsync)
            where T : Control
        {
            Form dialog = null;
            T control = null;

            TaskCompletionSource<VoidResult> gate = new TaskCompletionSource<VoidResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            JoinableTask test = JoinableTaskFactory.RunAsync(async () =>
            {
                await gate.Task;
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                await WaitForIdleAsync();
                try
                {
                    await testDriverAsync(dialog, control);
                }
                finally
                {
                    dialog.Close();
                }
            });

            (dialog, control) = showDialog();
            dialog.Activated += (sender, e) => gate.TrySetResult(default);
            dialog.ShowDialog();

            test.Join();
        }

        private struct VoidResult
        {
        }
    }
}
