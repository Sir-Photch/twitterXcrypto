using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using twitterXcrypto.data;

namespace twitterXcrypto_tests.data;

[TestClass]
public class ExtensionsTests
{
    [TestMethod]
    public void SanitizeTest()
    {
        string sanitized = "\"f'oo\"b'ar".Sanitize();

        Assert.AreEqual("foobar", sanitized);
    }

    [TestMethod]
    public void DateTimeOffsetToSqlTest()
    {
        DateTimeOffset dto = new(new DateTime(2000, 1, 7, 1, 30, 0), TimeSpan.Zero);
        string sqlEd = dto.ToSql();

        Assert.AreEqual("2000-01-07 01:30:00", sqlEd);
    }
}
