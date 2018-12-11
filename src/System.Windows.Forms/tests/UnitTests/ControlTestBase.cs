// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        protected SendInput InputSimulator => new SendInput();

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

            try
            {
                // Queue an event to make sure we don't stall if the application was already idle
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                await Task.Yield();

                await idleCompletionSource.Task;
            }
            finally
            {
                Application.Idle -= HandleApplicationIdle;
            }

            void HandleApplicationIdle(object sender, EventArgs e)
            {
                idleCompletionSource.TrySetResult(default);
            }
        }
    }
}
