using AddrKit.Editor;
using NUnit.Framework;

namespace AddrKit.Tests
{
    public class RuleApplierTests
    {
        const string Folder = "Assets/Game/Enemies";
        const string Asset = "Assets/Game/Enemies/Boss/Goblin.prefab";

        [Test]
        public void RelativePath_DropsFolderAndExtension()
            => Assert.AreEqual("Boss/Goblin", RuleApplier.ComputeAddress(AddressMode.RelativePath, "", Folder, Asset));

        [Test]
        public void FileName_UsesOnlyTheName()
            => Assert.AreEqual("Goblin", RuleApplier.ComputeAddress(AddressMode.FileName, "", Folder, Asset));

        [Test]
        public void FullPath_KeepsEverything()
            => Assert.AreEqual(Asset, RuleApplier.ComputeAddress(AddressMode.FullPath, "", Folder, Asset));

        [TestCase("Enemies")]
        [TestCase("Enemies/")]
        public void Prefix_GetsExactlyOneSlash(string prefix)
            => Assert.AreEqual("Enemies/Goblin", RuleApplier.ComputeAddress(AddressMode.FileName, prefix, Folder, Asset));

        [TestCase("prefab,png", "Assets/a.prefab", true)]
        [TestCase("prefab,png", "Assets/a.PNG", true)]
        [TestCase(".prefab", "Assets/a.prefab", true)]
        [TestCase("prefab", "Assets/a.png", false)]
        [TestCase("", "Assets/a.png", true)]
        [TestCase("", "Assets/a.cs", false)]
        [TestCase("cs", "Assets/a.cs", false)]
        public void ExtensionAllowed_FiltersCorrectly(string list, string path, bool expected)
            => Assert.AreEqual(expected, RuleApplier.ExtensionAllowed(list, path));
    }
}
