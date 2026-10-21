using NUnit.Framework;

namespace AddrKit.Tests
{
    public class KeyMatcherTests
    {
        [Test]
        public void Distance_CountsEdits()
        {
            Assert.AreEqual(0, AddrKeyMatcher.Distance("abc", "abc"));
            Assert.AreEqual(1, AddrKeyMatcher.Distance("goblin", "gobln"));
            Assert.AreEqual(3, AddrKeyMatcher.Distance("abc", ""));
        }

        [Test]
        public void Closest_FindsTypo_AndIgnoresFarKeys()
        {
            var candidates = new[] { "Enemies/Goblin", "Enemies/Orc", "UI/Menu" };
            var result = AddrKeyMatcher.Closest("Enemies/Gobln", candidates);

            CollectionAssert.Contains(result, "Enemies/Goblin");
            CollectionAssert.DoesNotContain(result, "UI/Menu");
        }

        [Test]
        public void Closest_IsCaseInsensitive()
        {
            var result = AddrKeyMatcher.Closest("enemies/goblin", new[] { "Enemies/Goblin" });
            CollectionAssert.Contains(result, "Enemies/Goblin");
        }

        [Test]
        public void Closest_NeverSuggestsTheExactKey()
        {
            var result = AddrKeyMatcher.Closest("A", new[] { "A", "B" });
            CollectionAssert.DoesNotContain(result, "A");
        }
    }
}
