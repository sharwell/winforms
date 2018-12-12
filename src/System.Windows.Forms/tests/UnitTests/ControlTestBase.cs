// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;

namespace System.Windows.Forms.Tests
{
    public abstract class ControlTestBase : IAsyncLifetime, IDisposable
    {
        private const int SPI_GETCLIENTAREAANIMATION = 4162;
        private const int SPI_SETCLIENTAREAANIMATION = 4163;
        private const int SPIF_SENDCHANGE = 0x0002;

        private bool clientAreaAnimation;
        private DenyExecutionSynchronizationContext _denyExecutionSynchronizationContext;
        private JoinableTaskCollection _joinableTaskCollection;

        protected ControlTestBase(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;

            Application.EnableVisualStyles();

            // Disable animations for maximum test performance
            bool disabled = false;
            Assert.True(UnsafeNativeMethods.SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref clientAreaAnimation, 0));
            Assert.True(UnsafeNativeMethods.SystemParametersInfo(SPI_SETCLIENTAREAANIMATION, 0, ref disabled, SPIF_SENDCHANGE));
        }

        protected ITestOutputHelper TestOutputHelper { get; }

        protected JoinableTaskContext JoinableTaskContext { get; private set; }

        protected JoinableTaskFactory JoinableTaskFactory { get; private set; }

        protected SendInput InputSimulator => new SendInput(WaitForIdleAsync);

        public virtual Task InitializeAsync()
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                JoinableTaskContext = new JoinableTaskContext();
            }
            else
            {
                _denyExecutionSynchronizationContext = new DenyExecutionSynchronizationContext(SynchronizationContext.Current);
                JoinableTaskContext = new JoinableTaskContext(_denyExecutionSynchronizationContext.MainThread, _denyExecutionSynchronizationContext);
            }

            _joinableTaskCollection = JoinableTaskContext.CreateCollection();
            JoinableTaskFactory = JoinableTaskContext.CreateFactory(_joinableTaskCollection);
            return Task.CompletedTask;
        }

        public virtual async Task DisposeAsync()
        {
            await _joinableTaskCollection.JoinTillEmptyAsync();
            JoinableTaskContext = null;
            JoinableTaskFactory = null;
            if (_denyExecutionSynchronizationContext != null)
            {
                SynchronizationContext.SetSynchronizationContext(_denyExecutionSynchronizationContext.UnderlyingContext);
                _denyExecutionSynchronizationContext.ThrowIfSwitchOccurred();
            }
        }

        public virtual void Dispose()
        {
            Assert.True(UnsafeNativeMethods.SystemParametersInfo(SPI_SETCLIENTAREAANIMATION, 0, ref clientAreaAnimation, 0));
        }

        protected async Task WaitForIdleAsync()
        {
            var idleCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Application.Idle += HandleApplicationIdle;
            Application.LeaveThreadModal += HandleApplicationIdle;

            try
            {
                // Queue an event to make sure we don't stall if the application was already idle
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                await Task.Yield();

                if (Application.OpenForms.Count > 0)
                {
                    await idleCompletionSource.Task;
                }
            }
            finally
            {
                Application.Idle -= HandleApplicationIdle;
                Application.LeaveThreadModal -= HandleApplicationIdle;
            }

            void HandleApplicationIdle(object sender, EventArgs e)
            {
                idleCompletionSource.TrySetResult(default);
            }
        }

        protected async Task MoveMouseToControlAsync(Control control)
        {
            var rect = control.DisplayRectangle;
            var centerOfRect = new Point(rect.Left, rect.Top) + new Size(rect.Width / 2, rect.Height / 2);
            var centerOnScreen = control.PointToScreen(centerOfRect);
            await MoveMouseAsync(control.FindForm(), centerOnScreen);
        }

        protected async Task MoveMouseAsync(Form window, Point point)
        {
            TestOutputHelper.WriteLine($"Moving mouse to ({point.X}, {point.Y}).");
            int horizontalResolution = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int verticalResolution = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            var virtualPoint = new Point((int)Math.Round((65535.0 / horizontalResolution) * point.X), (int)Math.Round((65535.0 / verticalResolution) * point.Y));
            TestOutputHelper.WriteLine($"Screen resolution of ({horizontalResolution}, {verticalResolution}) translates mouse to ({virtualPoint.X}, {virtualPoint.Y}).");

            await InputSimulator.SendAsync(window, inputSimulator => inputSimulator.Mouse.MoveMouseTo(virtualPoint.X + 1, virtualPoint.Y + 1));

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

        protected async Task RunSingleControlTestAsync<T>(Func<Form, T, Task> testDriverAsync)
            where T : Control, new()
        {
            await RunFormAsync(
                () =>
                {
                    var form = new Form();
                    form.TopMost = true;

                    var control = new T();
                    form.Controls.Add(control);

                    return (form, control);
                },
                testDriverAsync);
        }

        protected async Task RunFormAsync<T>(Func<(Form dialog, T control)> createDialog, Func<Form, T, Task> testDriverAsync)
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

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            (dialog, control) = createDialog();
            dialog.Activated += (sender, e) => gate.TrySetResult(default);
            dialog.ShowDialog();

            await test.JoinAsync();
        }
    }
}
