// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace System.Windows.Forms.Tests
{
    public class DateTimePickerTests : ControlTestBase
    {
        [Fact]
        public void DateTimePicker_Constructor()
        {
            var dtp = new DateTimePicker();

            Assert.NotNull(dtp);
            Assert.Equal(DateTimePickerFormat.Long, dtp.Format);
        }

        [WinFormsFact]
        public void ClickToNextMonth()
        {
            RunMonthCalendarTest(async control =>
            {
                control.TodayDate = new DateTime(2018, 12, 8);
                control.SetDate(new DateTime(2018, 12, 8));

                await Task.Delay(TimeSpan.FromSeconds(20));

                var sa = new NativeMethods.SYSTEMTIMEARRAY();
                var rc = UnsafeNativeMethods.SendMessage(new HandleRef(control, control.Handle), NativeMethods.MCM_GETMONTHRANGE, 0, ref sa);

                rc = UnsafeNativeMethods.SendMessage(new HandleRef(control, control.Handle), NativeMethods.MCM_GETCALENDARBORDER, 0, 0);

                rc = UnsafeNativeMethods.SendMessage(new HandleRef(control, control.Handle), NativeMethods.MCM_GETCALENDARCOUNT, 0, 0);

                // Find the position of the 'Next' button
                var gridInfo = GetCalendarGridInfo(control);
                var rect = Rectangle.FromLTRB(gridInfo.rc.left, gridInfo.rc.top, gridInfo.rc.right, gridInfo.rc.bottom);

                Assert.Equal("", rect.ToString());
            });
        }

        private static MCGRIDINFO GetCalendarGridInfo(MonthCalendar control)
        {
            MCGRIDINFO result = default;
            result.cbSize = Marshal.SizeOf<MCGRIDINFO>();
            result.dwPart = MCGIP_NEXT;
            result.dwFlags = MCGIF_RECT;
            var partOffset = Marshal.OffsetOf<MCGRIDINFO>(nameof(MCGRIDINFO.dwPart));
            var flagsOffset = Marshal.OffsetOf<MCGRIDINFO>(nameof(MCGRIDINFO.dwFlags));

            IntPtr rc = SendMessage(new HandleRef(control, control.HandleInternal), MCM_GETCALENDARGRIDINFO, 0, ref result);
            if (rc != IntPtr.Zero)
            {
                return result;
            }

            return result;
        }

        private const int MCM_FIRST = 0x1000;

        /// <summary>
        /// Gets information about a calendar grid.
        /// </summary>
        private const int MCM_GETCALENDARGRIDINFO = MCM_FIRST + 24;

        /// <summary>
        /// The entire calendar control, which may include up to 12 calendars.
        /// </summary>
        private const int MCGIP_CALENDARCONTROL = 0;

        /// <summary>
        /// The next button.
        /// </summary>
        private const int MCGIP_NEXT = 1;

        /// <summary>
        /// The previous button.
        /// </summary>
        private const int MCGIP_PREV = 2;

        /// <summary>
        /// The footer.
        /// </summary>
        private const int MCGIP_FOOTER = 3;

        /// <summary>
        /// One specific calendar. Used with <see cref="MCGRIDINFO.iCalendar"/> and <see cref="MCGRIDINFO.pszName"/>.
        /// </summary>
        private const int MCGIP_CALENDAR = 4;

        /// <summary>
        /// Calendar header. Used with <see cref="MCGRIDINFO.iCalendar"/> and <see cref="MCGRIDINFO.pszName"/>.
        /// </summary>
        private const int MCGIP_CALENDARHEADER = 5;

        /// <summary>
        /// Calendar body. Used with <see cref="MCGRIDINFO.iCalendar"/>.
        /// </summary>
        private const int MCGIP_CALENDARBODY = 6;

        /// <summary>
        /// A given calendar row. Used with <see cref="MCGRIDINFO.iCalendar"/> and <see cref="MCGRIDINFO.iRow"/>.
        /// </summary>
        private const int MCGIP_CALENDARROW = 7;

        /// <summary>
        /// A given calendar cell. Used with <see cref="MCGRIDINFO.iCalendar"/>, <see cref="MCGRIDINFO.iRow"/>,
        /// <see cref="MCGRIDINFO.iCol"/>, <see cref="MCGRIDINFO.bSelected"/>, and <see cref="MCGRIDINFO.pszName"/>.
        /// </summary>
        private const int MCGIP_CALENDARCELL = 8;

        /// <summary>
        /// <see cref="MCGRIDINFO.stStart"/> and <see cref="MCGRIDINFO.stEnd"/>.
        /// </summary>
        private const int MCGIF_DATE = 0x0001;

        /// <summary>
        /// <see cref="MCGRIDINFO.rc"/>.
        /// </summary>
        private const int MCGIF_RECT = 0x0002;

        /// <summary>
        /// <see cref="MCGRIDINFO.pszName"/>.
        /// </summary>
        private const int MCGIF_NAME = 0x0004;

        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, ref MCGRIDINFO lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MCGRIDINFO
        {
            /// <summary>
            /// Size of this structure, in bytes.
            /// </summary>
            public int cbSize;

            /// <summary>
            /// The part of the calendar control for which information is being requested.
            /// </summary>
            public uint dwPart;

            /// <summary>
            /// Indicates what information is to be filled in.
            /// </summary>
            public uint dwFlags;

            /// <summary>
            /// If <see cref="dwPart"/> is <see cref="MCGIP_CALENDAR"/>, <see cref="MCGIP_CALENDARHEADER"/>,
            /// <see cref="MCGIP_CALENDARBODY"/>, <see cref="MCGIP_CALENDARROW"/>, or <see cref="MCGIP_CALENDARCELL"/>,
            /// this member specifies the index of the calendar for which to retrieve information. For those parts, this
            /// must be a valid value even if there is only one calendar that is currently in the control.
            /// </summary>
            public int iCalendar;

            /// <summary>
            /// If <see cref="dwPart"/> is <see cref="MCGIP_CALENDARROW"/>, specifies the row for which to return
            /// information.
            /// </summary>
            public int iRow;
            public int iCol;
            public int bSelected;
            public NativeMethods.SYSTEMTIME stStart;
            public NativeMethods.SYSTEMTIME stEnd;
            public NativeMethods.RECT rc;
            public IntPtr pszName;
            public UIntPtr cchName;
        }

        private void RunMonthCalendarTest(Func<MonthCalendar, Task> testDriverAsync)
        {
            RunForm(
                () =>
                {
                    var form = new Form();
                    var control = new MonthCalendar();
                    control.Location = new Point(5, 5);
                    control.Name = "MyControl";
                    form.Controls.Add(control);

                    var button = new Button();
                    button.Location = new Point(5, 150);
                    form.Controls.Add(button);
                    button.Click += delegate
                    {
                        var gridInfo = GetCalendarGridInfo(control);
                        var info = Rectangle.FromLTRB(gridInfo.rc.left, gridInfo.rc.top, gridInfo.rc.right, gridInfo.rc.bottom);
                        Assert.Equal("", info.ToString());
                    };

                    return (form, control);
                },
                testDriverAsync);
        }

        private void RunForm<T>(Func<(Form dialog, T control)> showDialog, Func<T, Task> testDriverAsync)
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
                    await testDriverAsync(control);
                }
                finally
                {
                    dialog.Close();
                }
            });

            (dialog, control) = showDialog();
            gate.SetResult(default);
            dialog.ShowDialog();

            test.Join();
        }

        private struct VoidResult
        {
        }
    }
}
