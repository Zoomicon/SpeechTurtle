//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: SpeechUtils.cs
//Version: 20150828

  using Microsoft.Speech.Recognition;
using System.IO;
using System.Text;

namespace SpeechTurtle
{
  public static class SpeechUtils
  {

    /// <summary>
    /// Create a grammar from grammar definition XML (resource) file.
    /// </summary>
    /// <returns>Grammar</returns>
    public static Grammar CreateGrammarFromXML()
    {
      using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.SpeechGrammar_en)))
        return new Grammar(memoryStream);
    }

  }
}
