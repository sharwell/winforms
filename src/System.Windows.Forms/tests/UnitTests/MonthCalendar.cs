// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Runtime.InteropServices;
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
            RunSingleControlTest(testDriverAsync);
        }
    }
}
