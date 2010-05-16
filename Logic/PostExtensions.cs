using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlinqOnStackOverflow
{
    public static class PostExtensions
    {
        public static bool IsDescriptive(this Post p)
        {
            int tagUsed = 0;
            bool enoughTagsUsed = false;
            var tags = ExtractTags(p);
            var words = p.Body.ExtractUniqueWords();

            foreach (String tag in tags)
            {
                foreach (String word in words)
                {
                    if (String.Compare(tag, word, true) == 0)
                        tagUsed++;
                }
            }

            if ((tags.Count - tagUsed) > ((double)tags.Count * 0.30))
            {
                enoughTagsUsed = true;
            }

            return enoughTagsUsed;
        }

        public static List<String> ExtractTags(this Post p)
        {
            var result = new List<String>();

            if (String.IsNullOrEmpty(p.Tags) || (p.Tags[0] != '<' && p.Tags[p.Tags.Length - 1] != '>'))
                return result;

            StringBuilder t = new StringBuilder();
            for (int i = 0; i < p.Tags.Length; i++)
            {
                if (p.Tags[i] == '<')
                    t = new StringBuilder();
                else if (p.Tags[i] == '>')
                    result.Add(t.ToString());
                else
                    t.Append(p.Tags[i]);

            }

            return result;
        }

        //linear processing of List<T> checking for duplicates
        public static List<String> ExtractUniqueWords(this String src)
        {
            var result = new List<String>();

            foreach (String s in src.GetWords())
            {
                if (!result.Contains(s))
                    result.Add(s);
            }

            return result;
        }

        //Split on space (really simple, don't care about symbols, etc)
        public static List<String> GetWords(this String src)
        {
            var result = new List<String>();

            return src.Split(' ').ToList();
        }

        public static bool ContainsProbableUserReferenceAndPraise(this String comment)
        {
            if (String.IsNullOrEmpty(comment)) return false;

            //reference to someone in the first fifth of the string)
            int quarter = (int)(comment.Length * 0.2);
            int firstAt = comment.Substring(0, quarter).IndexOf('@');

            //exists and not last character
            if (firstAt >= 0 && firstAt < comment.Length - 1)
            {
                //looking for "@a" not "@ "
                if (comment[firstAt + 1] != ' ')
                {
                    if (comment.Contains("nice") || comment.Contains("good") || comment.Contains("great"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static int CalcLetters(this String txt)
        {
            int c = txt.Length;

            if (c > 5000)
                throw new InvalidOperationException("more than 5k characters");

            return c;
        }
    }
}
