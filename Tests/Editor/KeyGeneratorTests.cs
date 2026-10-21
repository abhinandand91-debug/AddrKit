using AddrKit.Editor;
using NUnit.Framework;

namespace AddrKit.Tests
{
    public class KeyGeneratorTests
    {
        [TestCase("Goblin", "Goblin")]
        [TestCase("a-b c", "a_b_c")]
        [TestCase("1st", "_1st")]
        [TestCase("class", "@class")]
        [TestCase("", "_")]
        public void ToIdentifier_ProducesValidNames(string input, string expected)
            => Assert.AreEqual(expected, KeyGenerator.ToIdentifier(input));

        [TestCase("enemy", "Enemy")]
        [TestCase("level-1", "Level1")]
        [TestCase("boss_fight", "BossFight")]
        public void ToPascalCase_Works(string input, string expected)
            => Assert.AreEqual(expected, KeyGenerator.ToPascalCase(input));

        [Test]
        public void BuildSource_NestsByFolder()
        {
            string source = KeyGenerator.BuildSource(new[] { "Enemies/Goblin", "Enemies/Orc", "Hero" }, new string[0], "Game");

            StringAssert.Contains("namespace Game", source);
            StringAssert.Contains("public static class Enemies", source);
            StringAssert.Contains("public const string Goblin = \"Enemies/Goblin\";", source);
            StringAssert.Contains("public const string Hero = \"Hero\";", source);
        }

        [Test]
        public void BuildSource_EmptyNamespace_OmitsNamespaceBlock()
        {
            string source = KeyGenerator.BuildSource(new[] { "A" }, new string[0], "");
            StringAssert.DoesNotContain("namespace", source);
        }

        [Test]
        public void BuildSource_AddressThatIsAlsoAFolder_KeepsBoth()
        {
            string source = KeyGenerator.BuildSource(new[] { "Hero", "Hero/Idle" }, new string[0], "");

            StringAssert.Contains("public const string Hero_ = \"Hero\";", source);
            StringAssert.Contains("public static class Hero", source);
            StringAssert.Contains("public const string Idle = \"Hero/Idle\";", source);
        }

        [Test]
        public void BuildSource_NameClashes_GetNumericSuffix()
        {
            string source = KeyGenerator.BuildSource(new[] { "a-b", "a_b" }, new string[0], "");
            StringAssert.Contains("a_b =", source);
            StringAssert.Contains("a_b2 =", source);
        }

        [Test]
        public void BuildSource_ChildNamedLikeParent_IsRenamed()
        {
            string source = KeyGenerator.BuildSource(new[] { "Keys/Thing" }, new string[0], "");
            StringAssert.Contains("public static class Keys2", source);
        }

        [Test]
        public void BuildSource_Labels_ArePascalCased()
        {
            string source = KeyGenerator.BuildSource(new string[0], new[] { "level-1" }, "");
            StringAssert.Contains("public const string Level1 = \"level-1\";", source);
        }

        [Test]
        public void BuildSource_EscapesQuotes()
        {
            string source = KeyGenerator.BuildSource(new[] { "a\"b" }, new string[0], "");
            StringAssert.Contains("\"a\\\"b\"", source);
        }
    }
}
