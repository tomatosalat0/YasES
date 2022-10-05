using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageBus.Messaging.Tests.UnitTests
{
    [TestClass]
    public class TopicNameTests
    {
        [TestMethod]
        public void TopicNameCanNotBeNull()
        {
            Assert.ThrowsException<ArgumentException>(() => new TopicName(null));
        }

        [TestMethod]
        public void TopicNameCanNotBeEmpty()
        {
            Assert.ThrowsException<ArgumentException>(() => new TopicName(string.Empty));
        }

        [TestMethod]
        public void TopicNameCanNotBeWhitespace()
        {
            Assert.ThrowsException<ArgumentException>(() => new TopicName(" "));
        }

        [TestMethod]
        public void TopicBuilderDoesNotBuildEmpty()
        {
            Assert.ThrowsException<ValidationException>(() => TopicName.Build(Array.Empty<string>()));
        }

        [TestMethod]
        public void TopicBuilderDoesNotAllowEmptyItems()
        {
            Assert.ThrowsException<ValidationException>(() => TopicName.Build("Test", ""));
        }

        [TestMethod]
        public void TopicBuilderDoesNotAllowSlashes()
        {
            Assert.ThrowsException<ValidationException>(() => TopicName.Build("Test/abc"));
        }

        [TestMethod]
        public void TopicBuilderDoesNotAllowWhitespaceCharacter()
        {
            Assert.ThrowsException<ValidationException>(() => TopicName.Build("test\tt"));
            Assert.ThrowsException<ValidationException>(() => TopicName.Build("test\nt"));
            Assert.ThrowsException<ValidationException>(() => TopicName.Build("test\rt"));
            Assert.ThrowsException<ValidationException>(() => TopicName.Build("test t"));
        }

        [TestMethod]
        public void TopicNameIsCaseSensitive()
        {
            TopicName t1 = TopicName.Build("t1");
            TopicName T1 = TopicName.Build("T1");
            TopicName t1_copy = TopicName.Build("t1");

            Assert.IsTrue(t1 == t1_copy);
            Assert.IsFalse(t1 == T1);

            Assert.IsFalse(t1 != t1_copy);
            Assert.IsTrue(t1 != T1);
        }

        [TestMethod]
        public void TopicNameContainsSections()
        {
            TopicName t = TopicName.Build("test");
            Assert.AreEqual("test", t.ToString());
        }

        [TestMethod]
        public void TopicNameEqualsCheck()
        {
            TopicName t1 = TopicName.Build("test");
            object t2 = TopicName.Build("test");

            Assert.IsFalse(t1.Equals(typeof(Assert)));
            Assert.IsTrue(t1.Equals("test"));
            Assert.IsTrue(t1.Equals(t2));
        }

        [TestMethod]
        public void TopicNameDoesNotAllowAnywhitespace()
        {
            Assert.ThrowsException<ValidationException>(() => new TopicName("Test abc"));
        }
    }
}
