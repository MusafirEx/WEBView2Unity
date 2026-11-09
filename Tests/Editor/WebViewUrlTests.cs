using MusafirEx.WEBView2Unity;
using NUnit.Framework;

namespace MusafirEx.WEBView2Unity.Tests
{
    public class WebViewUrlTests
    {
        [TestCase("example.com", "https://example.com/")]
        [TestCase("https://example.com/a", "https://example.com/a")]
        [TestCase("http://localhost:3000", "http://localhost:3000/")]
        public void NormalizeValidHttpUrls(string input, string expected)
        {
            string normalized, error;
            Assert.IsTrue(WebViewUrl.TryNormalize(input, out normalized, out error), error);
            Assert.AreEqual(expected, normalized);
        }

        [TestCase("javascript:alert(1)")]
        [TestCase("file:///C:/Windows/")]
        [TestCase("data:text/html,abc")]
        [TestCase(" ")]
        public void RejectUnsupportedUrls(string input)
        {
            string normalized, error;
            Assert.IsFalse(WebViewUrl.TryNormalize(input, out normalized, out error));
            Assert.IsNotEmpty(error);
        }
    }
}
