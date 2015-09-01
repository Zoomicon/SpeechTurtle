//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: SpeechGrammar_en.cs
//Version: 20150901

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
      var commands = new Choices();

      //FORWARD//
      commands.Add(new SemanticResultValue("forward", SpeechCommands.FORWARD));
      commands.Add(new SemanticResultValue("forwards", SpeechCommands.FORWARD));
      commands.Add(new SemanticResultValue("straight", SpeechCommands.FORWARD));

      //BACKWARD//
      commands.Add(new SemanticResultValue("back", SpeechCommands.BACKWARD));
      commands.Add(new SemanticResultValue("backward", SpeechCommands.BACKWARD));
      commands.Add(new SemanticResultValue("backwards", SpeechCommands.BACKWARD));

      //LEFT//
      commands.Add(new SemanticResultValue("turn left", SpeechCommands.LEFT));
      commands.Add(new SemanticResultValue("left", SpeechCommands.LEFT));

      //RIGHT//
      commands.Add(new SemanticResultValue("turn right", SpeechCommands.RIGHT));
      commands.Add(new SemanticResultValue("right", SpeechCommands.RIGHT));

      //PENDOWN//
      commands.Add(new SemanticResultValue("pen down", SpeechCommands.PENDOWN));
      commands.Add(new SemanticResultValue("start drawing", SpeechCommands.PENDOWN));
      commands.Add(new SemanticResultValue("draw", SpeechCommands.PENDOWN));

      //PENUP//
      commands.Add(new SemanticResultValue("pen up", SpeechCommands.PENUP));
      commands.Add(new SemanticResultValue("stop drawing", SpeechCommands.PENUP));
      commands.Add(new SemanticResultValue("don't draw", SpeechCommands.PENUP));
      commands.Add(new SemanticResultValue("do not draw", SpeechCommands.PENUP));

      //BIGGER//
      commands.Add(new SemanticResultValue("bigger", SpeechCommands.BIGGER));
      commands.Add(new SemanticResultValue("enlarge", SpeechCommands.BIGGER));

      //SMALLER//
      commands.Add(new SemanticResultValue("smaller", SpeechCommands.SMALLER));
      commands.Add(new SemanticResultValue("shrink", SpeechCommands.SMALLER));

      var gb = new GrammarBuilder { Culture = CultureInfo.GetCultureInfoByIetfLanguageTag("en") };
      gb.Append(commands);

      return new Grammar(gb);
    }

  }

}

