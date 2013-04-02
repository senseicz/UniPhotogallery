using System.Linq;

namespace UniPhotoGallery.Globals
{
    public class SEO
    {
        public static string ConvertTextForSEOURL(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
            {
                return string.Empty;
            }

            var charsForRepl = new [] { 'á', 'č', 'ď', 'é', 'ě', 'í', 'ň', 'ó', 'ř', 'š', 'ť', 'ú', 'ů', 'ý', 'ž', ' ' };
            var charsToRepl = new [] { 'a', 'c', 'd', 'e', 'e', 'i', 'n', 'o', 'r', 's', 't', 'u', 'u', 'y', 'z', '-' };
            var charsNotAllowed = new [] { '"', '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '=', '+', '[', ']', '{', '}', '|', '<', '>', ',', '.', '/', '?', '\'', '\\', ':', '\r', '\n' };

            inputText = inputText.ToLower();

            var retText = "";

            for (var i = 0; i < inputText.Length; i++)
            {
                bool wasReplace = false;
                bool doSkip = charsNotAllowed.Any(c => inputText[i] == c);

                for (var j = 0; j < charsForRepl.Length; j++)
                {
                    if (inputText[i] == charsForRepl[j])
                    {
                        retText = retText + charsToRepl[j];
                        wasReplace = true;
                        break;
                    }
                }

                if (!doSkip && !wasReplace)
                {
                    retText = retText + inputText[i];
                }
            }

            return retText;
        }

        public static string ConvertTextForSEOURL(string inputText, char spaceCharacterReplacement)
        {
            var retText = ConvertTextForSEOURL(inputText);
            return retText.Replace('-', spaceCharacterReplacement);
        }
    }
}


