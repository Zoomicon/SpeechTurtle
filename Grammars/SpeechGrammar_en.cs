//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: SpeechGrammar_en.cs
//Version: 20150828

using Microsoft.Speech.Recognition;
using System.Globalization;

namespace SpeechTurtle
{
  public static class SpeechGrammar_en
  {

    /// <summary>
    /// Create grammar programmatically rather than from a grammar file.
    /// </summary>
    public static Grammar Create()
    {
      var directions = new Choices();

      //FORWARD//
      directions.Add(new SemanticResultValue("forward", "FORWARD"));
      directions.Add(new SemanticResultValue("forwards", "FORWARD"));
      directions.Add(new SemanticResultValue("straight", "FORWARD"));

      //BACKWARD//
      directions.Add(new SemanticResultValue("back", "BACKWARD"));
      directions.Add(new SemanticResultValue("backward", "BACKWARD"));
      directions.Add(new SemanticResultValue("backwards", "BACKWARD"));

      //LEFT//
      directions.Add(new SemanticResultValue("turn left", "LEFT"));

      //RIGHT//
      directions.Add(new SemanticResultValue("turn right", "RIGHT"));

      var gb = new GrammarBuilder { Culture = CultureInfo.GetCultureInfoByIetfLanguageTag("en") };
      gb.Append(directions);

      return new Grammar(gb);
    }

  }

}

