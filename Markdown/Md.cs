using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System;
using FluentAssertions;

namespace Markdown
{
    public class Translotor
    {

        public static List<string> PutLineInList(string markdown, ItemMarkdown itemMarkdown)
        {
            var transformedMarkdown = new List<string>() {markdown};
            transformedMarkdown = SelectTags(transformedMarkdown, itemMarkdown.Separator);
            if (itemMarkdown.BlockedSeparator == null)
                return transformedMarkdown;
            foreach (var separator in itemMarkdown.BlockedSeparator)
            {
                transformedMarkdown = SelectTags(transformedMarkdown, separator, itemMarkdown.Separator);
            }
            return transformedMarkdown;
        }

        public static List<string> SelectTags(List<string> markdown, string separator, string markdownSeparator = null)
        {
            var transformedMarkdown = new List<string>();
            foreach (var segment in markdown)
            {
                var transformSegamen = segment;
                if (!transformSegamen.Contains(separator) || segment == markdownSeparator)
                {
                    transformedMarkdown.Add(transformSegamen);
                    continue;
                }
                var indexFirstTag = transformSegamen.IndexOf(separator);
                while (indexFirstTag != -1)
                {
                    transformedMarkdown.Add(transformSegamen.Substring(0, indexFirstTag));
                    transformedMarkdown.Add(separator);
                    transformSegamen = transformSegamen.Substring(indexFirstTag + separator.Length);
                    indexFirstTag = transformSegamen.IndexOf(separator);
                }
                if (transformSegamen.Length != 0)
                {
                    transformedMarkdown.Add(transformSegamen);
                }
            }
            return transformedMarkdown.Where(segment => segment.Length != 0).ToList();
        }

    }


    public class ItemMarkdown
    {
        public string OpeningTag { get; }
        public string EndingTag { get; }
        public string Separator { get; }
        public List<string> BlockedSeparator { get; }

        public ItemMarkdown( string openingTag, string endingTag, string separator, List<string> blocedSeparator = null)
        {
            OpeningTag = openingTag;
            EndingTag = endingTag;
            Separator = separator;
            BlockedSeparator = blocedSeparator;
        }
    }

	public class Md
	{

	    private List<ItemMarkdown> ItemsMarkdown = new List<ItemMarkdown>()
	    {
	        new ItemMarkdown("<strong>", "</strong>", "__", new List<string>(){ "_"}),
	        new ItemMarkdown("<em>", "</em>", "_")
	    };

        public string RenderToHtml(string markdown)
        {
            var changedMarkdown = markdown;
            foreach (var itemMarkdown in ItemsMarkdown)
            {
                var transformedMarkdown = Translotor.PutLineInList(changedMarkdown, itemMarkdown);
                changedMarkdown = Tagging(transformedMarkdown, itemMarkdown);
            }
	        if (changedMarkdown.Length == 0)
	            return markdown;

            return DeleteShiled(changedMarkdown);
	    }

	    private string DeleteShiled(string changedMarkdown)
	    {
	        var lineWithoutShiled = new StringBuilder(changedMarkdown);
	        for (int i = 0; i < lineWithoutShiled.Length - 1; i++)
	        {
	            if (lineWithoutShiled[i] == '\\' && lineWithoutShiled[i+1] == '_')
	            {
	                lineWithoutShiled.Remove(i, 1);
	            }

	        }
	        return lineWithoutShiled.ToString();
	    }

	    private string Tagging(List<string> transformedMarkdown, ItemMarkdown itemMarkdown)
	    {
            var indexOpeningTags = new Stack<int>();
	        var isBlocked = false;
	        for (int i = 0; i < transformedMarkdown.Count; i++)
	        {
	            if ( transformedMarkdown[i] == itemMarkdown.Separator && IsTag(transformedMarkdown, i))
	            {
	                if (isBlocked)
	                {
	                    transformedMarkdown[i] = Blocked(itemMarkdown);
                        continue;
                    }
	                if (isOpeningTag(transformedMarkdown, i))
	                {
	                    indexOpeningTags.Push(i);
                        continue;
	                }
	                if (isEndingTag(transformedMarkdown, i))
	                {
	                    if (indexOpeningTags.Count == 0)
	                        continue;
	                    transformedMarkdown = Replace(transformedMarkdown, itemMarkdown, indexOpeningTags.Pop(), i);
	                }
	            }
	            if (itemMarkdown.BlockedSeparator!=null && itemMarkdown.BlockedSeparator.Contains(transformedMarkdown[i]) 
                    && IsTag(transformedMarkdown, i))
	            {
	                isBlocked = !isBlocked;
	            }
	        }
	        return ListToString(transformedMarkdown);
	    }

	    private string ListToString(List<string> transformedMarkdown)
	    {
	        var taggingLine = new StringBuilder(transformedMarkdown.Count);
	        for (var i = 0; i < transformedMarkdown.Count; i++)
	        {
	            taggingLine.Append(transformedMarkdown[i]);
	        }
	        return taggingLine.ToString();
        }

	    private List<string> Replace(List<string> transformedMarkdown, ItemMarkdown itemMarkdown, 
            int indexOpenintTags, int indexEndingTag)
	    {
	        transformedMarkdown[indexOpenintTags] = itemMarkdown.OpeningTag;
	        transformedMarkdown[indexEndingTag] = itemMarkdown.EndingTag;
	        return transformedMarkdown;
	    }

	    private bool isEndingTag(List<string> transformedMarkdown, int index)
	    {
	        return index == transformedMarkdown.Count -1 || transformedMarkdown[index + 1].StartsWith(" ");
	    }

        private bool isOpeningTag(List<string> transformedMarkdown, int index)
        {
            return index == 0 || transformedMarkdown[index - 1].EndsWith(" ");
        }

        private bool IsTag(List<string> transformedMarkdown , int i)
        {
            if (i > 0 && transformedMarkdown[i - 1].EndsWith("\\"))
                return false;
            if (i == 0 && transformedMarkdown.Count == i + 1)
                return false;
            if (i == 0 && !transformedMarkdown[i + 1].StartsWith(" "))
                return true;
            if (i == 0 && transformedMarkdown[i + 1].StartsWith(" "))
                return false;
            if (i == transformedMarkdown.Count - 1 && !transformedMarkdown[i - 1].EndsWith(" "))
                return true;
            if (i == transformedMarkdown.Count - 1 && transformedMarkdown[i - 1].EndsWith(" "))
                return false;
            var isOpen = i > 0 && transformedMarkdown[i - 1].EndsWith(" ") &&
                          !transformedMarkdown[i + 1].StartsWith(" ");
            var isClose = i < transformedMarkdown.Count-1 && !transformedMarkdown[i - 1].EndsWith(" ") &&
                          transformedMarkdown[i + 1].StartsWith(" ");
            return isOpen || isClose;
        }

	    private string Blocked(ItemMarkdown itemMarkdown)
	    {
	        var blocedSeparator = "";
	        for (int i = 0; i < itemMarkdown.Separator.Length; i++)
	        {
	            blocedSeparator += '\\' + itemMarkdown.BlockedSeparator[0];
	        }
	        return blocedSeparator;
	    }

    }

	[TestFixture]
	public class Md_ShouldRender
	{
	    [TestCase("aaaa")]
        [TestCase(" aa")]
        [TestCase("aa ")]
        [TestCase(" aa ")]
	    public void MarkdownWithoutHandwriting(string markdown)
	    {
	        var md = new Md();
	        Assert.AreEqual(markdown, md.RenderToHtml(markdown));
        }

        [Test]
        public void SingleHandwriting()
        {
            var md = new Md();
            var markdown = "_aaaaaa_";
            var expected = "<em>" + markdown.Substring(1, markdown.Length - 2) + "</em>";

            var actual = md.RenderToHtml(markdown);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ManyHandwriting()
        {
            var md = new Md();
            var markdown = "_abc_ _abc_";
            var expected = "<em>abc</em> <em>abc</em>";
            var actual = md.RenderToHtml(markdown);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IncorrectHandwritting()
        {

            var md = new Md();
            var markdowm = "_ abc_";
            var expected = markdowm;

            var actual = md.RenderToHtml(markdowm);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("_abc _abc_", "_abc <em>abc</em>")]
        [TestCase("abc_ _abc_", "abc_ <em>abc</em>")]
        [TestCase("_abc_ abc_", "<em>abc</em> abc_")]
        [TestCase("_abc_ _abc", "<em>abc</em> _abc")]
        public void CorrectIncorrectHadnwritingInMarkdown(string markdown, string expected)
        {
            var md = new Md();
            Assert.AreEqual(expected, md.RenderToHtml(markdown));
        }

        [Test]
        public void NestingSequence()
        {
            var md = new Md();
            var markdown = "_a _a _a_ _ _";
            var expected = "_a _a <em>a</em> _ _";

            Assert.AreEqual(expected, md.RenderToHtml(markdown));
        }

        [TestCase("\\_aa\\_ ", "_aa_ ")]
        [TestCase("\\_aa\\_ \\_aa\\_", "_aa_ _aa_")]
        [TestCase("\\_aa", "_aa")]
        [TestCase("aa\\_", "aa_")]
        [TestCase("aa _\\_aaa\\__ as", "aa <em>_aaa_</em> as")]
        public void ShieldedHandwriting(string markdown, string expected)
        {
            var md = new Md();
            Assert.AreEqual(expected, md.RenderToHtml(markdown));
        }


	    [TestCase("\\_aaa _aaa_ \\_", "_aaa <em>aaa</em> _")]
	    [TestCase("_aaa \\_aaa\\_ aaa_", "<em>aaa _aaa_ aaa</em>")]
	    [TestCase("_a \\_\\_a\\_\\_ a_", "<em>a __a__ a</em>")]
	    public void LineWithDifferentHandwriting(string markdown, string expected)
	    {
	        var md = new Md();

	        Assert.AreEqual(expected, md.RenderToHtml(markdown));
	    }

        [TestCase("__aaaa__", "<strong>aaaa</strong>")]
        [TestCase("__aa__ aa __aa__", "<strong>aa</strong> aa <strong>aa</strong>")]
        [TestCase(" __a _aa_ __aa__", " __a <em>aa</em> <strong>aa</strong>")]
        [TestCase("\\__aaaa\\__", "__aaaa__")]
        [TestCase("\\_\\_aaa\\_\\_", "__aaa__")]
        public void DoubleHandwriting(string markdown, string expected)
        {
            var md = new Md();
            Assert.AreEqual(expected, md.RenderToHtml(markdown));
        }

        [TestCase("__aa _a_ aa__", "<strong>aa <em>a</em> aa</strong>")]
        [TestCase("_a __aa__ a_", "<em>a __aa__ a</em>")]
        [TestCase("_a __aa__ __aa__ a_", "<em>a __aa__ __aa__ a</em>")]
        [TestCase("__aa _a __aa__ a_ aa__", "<strong>aa <em>a __aa__ a</em> aa</strong>")]
        public void DifferentNestings(string markdown, string expected)
        {
            var md = new Md();
            Assert.AreEqual(expected, md.RenderToHtml(markdown));
        }

    }
}