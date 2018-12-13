// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Threading.Tasks;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace System.Windows.Forms.Tests
{
    public class ButtonUI : ControlTestBase
    {
        public ButtonUI(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [WinFormsTheory]

        // These results close the window
        [InlineData(DialogResult.Abort)]
        [InlineData(DialogResult.Cancel)]
        [InlineData(DialogResult.Ignore)]
        [InlineData(DialogResult.No)]
        [InlineData(DialogResult.OK)]
        [InlineData(DialogResult.Retry)]
        [InlineData(DialogResult.Yes)]

        // This result leaves the window open
        [InlineData(DialogResult.None)]
        public async Task ClickDefaultButtonToCloseFormAsync(DialogResult dialogResult)
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                Assert.Equal(DialogResult.None, control.DialogResult);

                control.DialogResult = dialogResult;

                await MoveMouseToControlAsync(control);

                Assert.Equal(CloseReason.None, form.CloseReason);
                Assert.Equal(DialogResult.None, form.DialogResult);
                Assert.True(form.Visible);

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Mouse.LeftButtonClick());

                Assert.Equal(CloseReason.None, form.CloseReason);
                Assert.Equal(dialogResult, form.DialogResult);

                // The window will only still be visible for DialogResult.None
                Assert.Equal(dialogResult == DialogResult.None, form.Visible);
            });
        }

        [WinFormsFact]
        public async Task SpaceToClickFocusedButtonAsync()
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                control.DialogResult = DialogResult.OK;

                Assert.True(control.Focus());

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE));

                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.False(form.Visible);
            });
        }

        [WinFormsFact]
        public async Task EscapeDoesNotClickFocusedButtonAsync()
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                control.DialogResult = DialogResult.OK;

                Assert.True(control.Focus());

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE));

                Assert.Equal(DialogResult.None, form.DialogResult);
                Assert.True(form.Visible);
            });
        }

        [WinFormsFact]
        public async Task EscapeClicksCancelButtonAsync()
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                form.CancelButton = control;

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE));

                Assert.Equal(DialogResult.Cancel, form.DialogResult);
                Assert.False(form.Visible);
            });
        }

        [WinFormsFact]
        public async Task NoResizeOnWindowSizeWiderAsync()
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                var originalFormSize = form.DisplayRectangle.Size;
                var originalButtonPosition = control.DisplayRectangle;

                var mouseDragHandleOnForm = new Point(form.DisplayRectangle.Right, form.DisplayRectangle.Top + form.DisplayRectangle.Height / 2);
                await MoveMouseAsync(form, form.PointToScreen(mouseDragHandleOnForm));

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Mouse
                        .LeftButtonDown()
                        .MoveMouseBy(form.DisplayRectangle.Width, 0)
                        .LeftButtonUp());

                Assert.True(form.DisplayRectangle.Width > originalFormSize.Width);
                Assert.Equal(originalFormSize.Height, form.DisplayRectangle.Height);
                Assert.Equal(originalButtonPosition, control.DisplayRectangle);
            });
        }

        [WinFormsFact]
        public async Task NoResizeOnWindowSizeTallerAsync()
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                var originalFormSize = form.DisplayRectangle.Size;
                var originalButtonPosition = control.DisplayRectangle;

                var mouseDragHandleOnForm = new Point(form.DisplayRectangle.Left + form.DisplayRectangle.Width / 2, form.DisplayRectangle.Bottom + 1);
                await MoveMouseAsync(form, form.PointToScreen(mouseDragHandleOnForm));

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Mouse
                        .LeftButtonDown()
                        .MoveMouseBy(0, form.DisplayRectangle.Height)
                        .LeftButtonUp());

                Assert.True(form.DisplayRectangle.Height > originalFormSize.Height);
                Assert.Equal(originalFormSize.Width, form.DisplayRectangle.Width);
                Assert.Equal(originalButtonPosition, control.DisplayRectangle);
            });
        }

        [WinFormsFact]
        public async Task ResizeOnWindowSizeWiderAsync()
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                control.Anchor = AnchorStyles.Left | AnchorStyles.Right;

                var originalFormSize = form.DisplayRectangle.Size;
                var originalButtonPosition = control.DisplayRectangle;

                var mouseDragHandleOnForm = new Point(form.DisplayRectangle.Right, form.DisplayRectangle.Top + form.DisplayRectangle.Height / 2);
                await MoveMouseAsync(form, form.PointToScreen(mouseDragHandleOnForm));

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Mouse
                        .LeftButtonDown()
                        .MoveMouseBy(form.DisplayRectangle.Width, 0)
                        .LeftButtonUp());

                Assert.True(form.DisplayRectangle.Width > originalFormSize.Width);
                Assert.Equal(originalFormSize.Height, form.DisplayRectangle.Height);

                Assert.Equal(originalButtonPosition.Location, control.DisplayRectangle.Location);

                // Still anchored on right
                Assert.Equal(originalFormSize.Width - originalButtonPosition.Right, form.DisplayRectangle.Width - control.DisplayRectangle.Right);
            });
        }

        [WinFormsFact]
        public async Task ResizeOnWindowSizeTallerAsync()
        {
            await RunSingleControlTestAsync<Button>(async (form, control) =>
            {
                control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;

                var originalFormSize = form.DisplayRectangle.Size;
                var originalButtonPosition = control.DisplayRectangle;

                var mouseDragHandleOnForm = new Point(form.DisplayRectangle.Left + form.DisplayRectangle.Width / 2, form.DisplayRectangle.Bottom + 1);
                await MoveMouseAsync(form, form.PointToScreen(mouseDragHandleOnForm));

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Mouse
                        .LeftButtonDown()
                        .MoveMouseBy(0, form.DisplayRectangle.Height)
                        .LeftButtonUp());

                Assert.True(form.DisplayRectangle.Height > originalFormSize.Height);
                Assert.Equal(originalFormSize.Width, form.DisplayRectangle.Width);

                Assert.Equal(originalButtonPosition.Location, control.DisplayRectangle.Location);

                // Still anchored on bottom
                Assert.Equal(originalFormSize.Height - originalButtonPosition.Bottom, form.DisplayRectangle.Height - control.DisplayRectangle.Bottom);
            });
        }

        [WinFormsFact]
        public async Task DragAfterMouseDownAsync()
        {
            await RunControlPairTestAsync<Button>(async (form, controls) =>
            {
                (Button control1, Button control2) = controls;

                int control1ClickCount = 0;
                int control2ClickCount = 0;
                control1.Click += (sender, e) => control1ClickCount++;
                control2.Click += (sender, e) => control2ClickCount++;

                // Verify mouse press without moving causes a button click
                await MoveMouseToControlAsync(control1);
                await InputSimulator.SendAsync(form, inputSimulator => inputSimulator.Mouse.LeftButtonDown().LeftButtonUp());
                Assert.Equal(1, control1ClickCount);
                Assert.Equal(0, control2ClickCount);

                // Verify mouse press without moving causes a button click
                await MoveMouseToControlAsync(control2);
                await InputSimulator.SendAsync(form, inputSimulator => inputSimulator.Mouse.LeftButtonDown().LeftButtonUp());
                Assert.Equal(1, control1ClickCount);
                Assert.Equal(1, control2ClickCount);

                // Verify that mouse press and then drag off the control does not cause a button click of either button
                await MoveMouseToControlAsync(control1);

                Rectangle rect = control2.DisplayRectangle;
                Point centerOfRect = new Point(rect.Left, rect.Top) + new Size(rect.Width / 2, rect.Height / 2);
                Point centerOnScreen = control2.PointToScreen(centerOfRect);
                int horizontalResolution = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                int verticalResolution = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
                var virtualPoint = new Point((int)Math.Round((65535.0 / horizontalResolution) * centerOnScreen.X), (int)Math.Round((65535.0 / verticalResolution) * centerOnScreen.Y));

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Mouse
                        .LeftButtonDown()
                        .MoveMouseTo(virtualPoint.X + 1, virtualPoint.Y + 1)
                        .LeftButtonUp());

                ////Assert.False(control1.MouseIsOver);
                ////Assert.True(control2.MouseIsOver);

                Assert.Equal(1, control1ClickCount);
                Assert.Equal(1, control2ClickCount);

                // Verify that mouse press and then drag off the control and back causes a button click
                await MoveMouseToControlAsync(control1);

                Rectangle rect1 = control1.DisplayRectangle;
                Point centerOfRect1 = new Point(rect1.Left, rect1.Top) + new Size(rect1.Width / 2, rect1.Height / 2);
                Point centerOnScreen1 = control1.PointToScreen(centerOfRect1);
                int horizontalResolution1 = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                int verticalResolution1 = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
                var virtualPoint1 = new Point((int)Math.Round((65535.0 / horizontalResolution1) * centerOnScreen1.X), (int)Math.Round((65535.0 / verticalResolution1) * centerOnScreen1.Y));

                await InputSimulator.SendAsync(
                    form,
                    inputSimulator => inputSimulator.Mouse
                        .LeftButtonDown()
                        .MoveMouseTo(virtualPoint.X + 1, virtualPoint.Y + 1)
                        .MoveMouseTo(virtualPoint1.X + 1, virtualPoint1.Y + 1)
                        .LeftButtonUp());

                ////Assert.False(control1.MouseIsOver);
                ////Assert.True(control2.MouseIsOver);

                Assert.Equal(2, control1ClickCount);
                Assert.Equal(1, control2ClickCount);
            });
        }
    }
}
