// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

namespace System.Windows.Forms.Tests
{
    public class DateTimePickerTests : ControlTestBase
    {
        public DateTimePickerTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void DateTimePicker_Constructor()
        {
            var dtp = new DateTimePicker();

            Assert.NotNull(dtp);
            Assert.Equal(DateTimePickerFormat.Long, dtp.Format);
        }
    }
}
