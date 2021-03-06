﻿// Copyright 2018 Google Inc. All Rights Reserved.
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace TestingHelpers
{
    public static class MsTestExtensions
    {
        public static void IsEmpty(this CollectionAssert that, IEnumerable<object> enumerable) =>
            Assert.IsFalse(enumerable.Any(), "Collection is not empty");

        public static AllAssert All(this CollectionAssert that, IEnumerable<object> enumerable) =>
            new AllAssert(enumerable);

        public class AllAssert
        {
            private readonly IEnumerable<object> _enumerable;

            public AllAssert(IEnumerable<object> enumerable)
            {
                _enumerable = enumerable;
            }

            public void AreEqualTo(object value)
            {
                foreach (object o in _enumerable)
                {
                    Assert.AreEqual(value, o);
                }
            }
        }
    }
}
