using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [TestFixture]
    public class ConstantSetTest
    {
        [Test] public void TestCount()
        {
            Assert.AreEqual(0, new ConstantSet<string>().Count);
            Assert.AreEqual(1, new ConstantSet<string>(new[] { "WORD" }).Count);
            Assert.AreEqual(1, new ConstantSet<string>(new[] { "DUPLICATED", "DUPLICATED" }).Count);
            Assert.AreEqual(2, new ConstantSet<string>(new[] { "ONE", "TWO" }).Count);
        }
    }
}
