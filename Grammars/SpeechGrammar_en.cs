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
      directions.Add(new SemanticResultValue("forward", SpeechCommands.FORWARD));
      directions.Add(new SemanticResultValue("forwards", SpeechCommands.FORWARD));
      directions.Add(new SemanticResultValue("straight", SpeechCommands.FORWARD));

      //BACKWARD//
      directions.Add(new SemanticResultValue("back", SpeechCommands.BACKWARD));
      directions.Add(new SemanticResultValue("backward", SpeechCommands.BACKWARD));
      directions.Add(new SemanticResultValue("backwards", SpeechCommands.BACKWARD));

      //LEFT//
      directions.Add(new SemanticResultValue("turn left", SpeechCommands.LEFT));
      directions.Add(new SemanticResultValue("left", SpeechCommands.LEFT));

      //RIGHT//
      directions.Add(new SemanticResultValue("turn right", SpeechCommands.RIGHT));
      directions.Add(new SemanticResultValue("right", SpeechCommands.RIGHT));

      //PENUP//
      directions.Add(new SemanticResultValue("pen up", SpeechCommands.PENUP));
      directions.Add(new SemanticResultValue("start drawing", SpeechCommands.PENUP));
      directions.Add(new SemanticResultValue("draw", SpeechCommands.PENUP));

      //PENDOWN//
      directions.Add(new SemanticResultValue("pen down", SpeechCommands.PENDOWN));
      directions.Add(new SemanticResultValue("stop drawing", SpeechCommands.PENDOWN));
      directions.Add(new SemanticResultValue("don't draw", SpeechCommands.PENDOWN));
      directions.Add(new SemanticResultValue("do not draw", SpeechCommands.PENDOWN));

      //BIGGER//
      directions.Add(new SemanticResultValue("bigger", SpeechCommands.BIGGER));
      directions.Add(new SemanticResultValue("enlarge", SpeechCommands.BIGGER));

      //SMALLER//
      directions.Add(new SemanticResultValue("smaller", SpeechCommands.SMALLER));
      directions.Add(new SemanticResultValue("shrink", SpeechCommands.SMALLER));

      var gb = new GrammarBuilder { Culture = CultureInfo.GetCultureInfoByIetfLanguageTag("en") };
      gb.Append(directions);

      return new Grammar(gb);
    }

  }

}

