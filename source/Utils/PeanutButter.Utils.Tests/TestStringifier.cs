using System;
using System.Globalization;
using System.Xml.Linq;
using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace PeanutButter.Utils.Tests
{
    [TestFixture]
    public class TestStringifier
    {
        [TestCase("foo", "\"foo\"")]
        [TestCase(1, "1")]
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        [TestCase(null, "null")]
        public void Stringify_GivenPrimitive_ShouldReturnExpected(object value, string expected)
        {
            //--------------- Arrange -------------------

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = value.Stringify();

            //--------------- Assert -----------------------
            Expect(result).To.Equal(expected);
        }

        private static readonly Tuple<object, string>[] ComplexSource =
        {
            Tuple.Create(new {foo = 1} as object, @"
{
  foo: 1
}"),
            Tuple.Create(new {foo = new {bar = 1}} as object, @"
{
  foo: {
    bar: 1
  }
}")
        };

        [TestCaseSource(nameof(ComplexSource))]
        public void Stringify_GivenObject_ShouldReturnExpected(Tuple<object, string> data)
        {
            //--------------- Arrange -------------------

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = data.Item1.Stringify();

            //--------------- Assert -----------------------
            Expect(result).To.Equal(data.Item2.TrimStart().Replace("\r", ""));
        }

        [Test]
        public void CollectionsOfCollections()
        {
            // Arrange
            // Pre-Assert
            // Act
            Console.WriteLine(new[]
            {
                new[] {1, 2},
                new[] {5, 6, 7}
            }.Stringify());
            // Assert
        }

        [Test]
        public void Stringifying_PropertiesOfTypeShort()
        {
            // Arrange
            var obj = new {x = short.MaxValue};
            var expected = (@"{
  x: " + short.MaxValue + @"
}").Replace("\r", "");
            // Pre-Assert
            // Act
            var result = obj.Stringify();
            // Assert
            Expect(result).To.Equal(expected);
        }

        [Test]
        public void ShouldInclude_Kind_ForDateTime()
        {
            // Arrange
            var src = GetRandomDate();
            var local = new DateTime(src.Year, src.Month, src.Day, src.Hour, src.Minute, src.Second, DateTimeKind.Local);
            var utc = new DateTime(src.Year, src.Month, src.Day, src.Hour, src.Minute, src.Second, DateTimeKind.Utc);
            var unspecified = new DateTime(src.Year, src.Month, src.Day, src.Hour, src.Minute, src.Second, DateTimeKind.Unspecified);
            var expectedPre = src.ToString(CultureInfo.InvariantCulture);
            // Pre-Assert
            // Act
            var localResult = local.Stringify();
            var utcResult = utc.Stringify();
            var unspecifiedResult = unspecified.Stringify();
            // Assert
            Expect(localResult).To.Start.With(expectedPre)
                .And.End.With(" (Local)");
            Expect(utcResult).To.Start.With(expectedPre)
                .And.End.With(" (Utc)");
            Expect(unspecifiedResult).To.Start.With(expectedPre)
                .And.End.With(" (Unspecified)");
        }

        [Test]
        public void ShouldOutputXmlForXDocument()
        {
            // Arrange
            var raw = "<doc><node>1</node></doc>";
            var doc = XDocument.Parse(raw);
            // Pre-assert
            // Act
            var result = doc.Stringify();
            // Assert
            Expect(result.RegexReplace("\\s", "")).To.Equal(raw);
        }
    }
}