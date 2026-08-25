using System.Linq;
using Amethystra.UI.Text;

namespace Amethystra.Test.UI.Text;

[TestClass]
public sealed class EmojiSegmenterTest
{
    [TestMethod]
    public void Split_ReturnsEmpty_WhenTextIsNullOrEmpty()
    {
        Assert.HasCount(0, EmojiSegmenter.Split(null));
        Assert.HasCount(0, EmojiSegmenter.Split(""));
    }

    [TestMethod]
    public void Split_ReturnsSingleTextSegment_ForPlainText()
    {
        var segments = EmojiSegmenter.Split("00:30 コハル EX");

        Assert.HasCount(1, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Text, segments[0].Kind);
        Assert.AreEqual("00:30 コハル EX", segments[0].Text);
    }

    [TestMethod]
    public void Split_ReturnsSingleEmojiSegment_ForSingleEmoji()
    {
        var segments = EmojiSegmenter.Split("🚩");

        Assert.HasCount(1, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[0].Kind);
        Assert.AreEqual("🚩", segments[0].Text);
    }

    [TestMethod]
    public void Split_SeparatesEmojiAndText()
    {
        var segments = EmojiSegmenter.Split("🚩の時");

        Assert.HasCount(2, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[0].Kind);
        Assert.AreEqual("🚩", segments[0].Text);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Text, segments[1].Kind);
        Assert.AreEqual("の時", segments[1].Text);
    }

    [TestMethod]
    public void Split_KeepsZwjSequenceAsSingleEmoji()
    {
        var segments = EmojiSegmenter.Split("👨‍👩‍👧");

        Assert.HasCount(1, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[0].Kind);
        Assert.AreEqual("👨‍👩‍👧", segments[0].Text);
    }

    [TestMethod]
    public void Split_KeepsKeycapSequenceAsSingleEmoji()
    {
        var segments = EmojiSegmenter.Split("1️⃣");

        Assert.HasCount(1, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[0].Kind);
        Assert.AreEqual("1️⃣", segments[0].Text);
    }

    [TestMethod]
    public void Split_KeepsRegionalIndicatorPairAsSingleEmoji()
    {
        var segments = EmojiSegmenter.Split("🇯🇵");

        Assert.HasCount(1, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[0].Kind);
        Assert.AreEqual("🇯🇵", segments[0].Text);
    }

    // IME 変換中のドキュメントには対にならないサロゲートが一時的に現れることがある。
    // 🚩 (U+1F6A9) の変換確定時に上位サロゲート単体で Split が呼ばれ、
    // Rune.GetRuneAt の ArgumentException でクラッシュした事例のリグレッションテスト。
    [TestMethod]
    public void Split_TreatsLoneHighSurrogateAsText()
    {
        var segments = EmojiSegmenter.Split("\uD83D");

        Assert.HasCount(1, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Text, segments[0].Kind);
        Assert.AreEqual("\uD83D", segments[0].Text);
    }

    [TestMethod]
    public void Split_TreatsLoneLowSurrogateAsText()
    {
        var segments = EmojiSegmenter.Split("\uDEA9");

        Assert.HasCount(1, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Text, segments[0].Kind);
        Assert.AreEqual("\uDEA9", segments[0].Text);
    }

    [TestMethod]
    public void Split_TreatsLoneSurrogateBetweenTextAndEmojiAsText()
    {
        var segments = EmojiSegmenter.Split("あ\uD83D🚩");

        Assert.HasCount(2, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Text, segments[0].Kind);
        Assert.AreEqual("あ\uD83D", segments[0].Text);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[1].Kind);
        Assert.AreEqual("🚩", segments[1].Text);
    }

    [TestMethod]
    public void Split_EndsEmojiCluster_AtLoneSurrogate()
    {
        var segments = EmojiSegmenter.Split("🚩\uD83D");

        Assert.HasCount(2, segments);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[0].Kind);
        Assert.AreEqual("🚩", segments[0].Text);
        Assert.AreEqual(EmojiSegmenter.SegmentKind.Text, segments[1].Kind);
        Assert.AreEqual("\uD83D", segments[1].Text);
    }

    [TestMethod]
    public void Split_EndsZwjSequence_AtLoneSurrogate()
    {
        var segments = EmojiSegmenter.Split("👨\u200D\uD83D");

        Assert.AreEqual(EmojiSegmenter.SegmentKind.Emoji, segments[0].Kind);
        Assert.AreEqual("👨", segments[0].Text);
        Assert.IsTrue(segments.Skip(1).All(x => x.Kind == EmojiSegmenter.SegmentKind.Text));
        Assert.AreEqual("\u200D\uD83D", string.Concat(segments.Skip(1).Select(x => x.Text)));
    }

    [TestMethod]
    public void Split_RoundTripsOriginalText()
    {
        const string text = "00:30 🚩の時 コハル 1️⃣ EX\uD83D";

        var segments = EmojiSegmenter.Split(text);

        Assert.AreEqual(text, string.Concat(segments.Select(x => x.Text)));
    }
}
