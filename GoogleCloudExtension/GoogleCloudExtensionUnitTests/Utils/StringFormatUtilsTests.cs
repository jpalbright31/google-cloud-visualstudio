﻿// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class StringFormatUtilsTests
    {
        [TestMethod]
        public void BasicFormattingTests()
        {
            // Ensures that the basic formatting works. The actual details of how the number is converted
            // into a human readable size are implemented by the Win32 API StrFormatByteSizeW. This test ensures
            // that we can call into the API and get a valid result.
            Assert.AreEqual("10 bytes", StringFormatUtils.FormatByteSize(10));
            Assert.AreEqual("1.00 KB", StringFormatUtils.FormatByteSize(1024));
            Assert.AreEqual("1.00 MB", StringFormatUtils.FormatByteSize(1024 * 1024));
            Assert.AreEqual("1.00 GB", StringFormatUtils.FormatByteSize(1024 * 1024 * 1024));
        }
    }
}
