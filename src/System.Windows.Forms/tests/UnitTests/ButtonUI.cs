// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
