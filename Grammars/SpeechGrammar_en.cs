//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: SpeechGrammar_en.cs
//Version: 20150910

using System.Globalization;

#if USE_MICROSOFT_SPEECH
using Microsoft.Speech.Recognition;
#else
using System.Speech.Recognition;
#endif

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

      //CLOSE//
      commands.Add(new SemanticResultValue("close", Commands.CLOSE));
      commands.Add(new SemanticResultValue("exit", Commands.CLOSE));

      //FORWARD//
      commands.Add(new SemanticResultValue("forward", Commands.FORWARD));
      commands.Add(new SemanticResultValue("forwards", Commands.FORWARD));
      commands.Add(new SemanticResultValue("straight", Commands.FORWARD));

      //BACKWARD//
      commands.Add(new SemanticResultValue("back", Commands.BACK));
      commands.Add(new SemanticResultValue("backward", Commands.BACK));
      commands.Add(new SemanticResultValue("backwards", Commands.BACK));

      //LEFT//
      commands.Add(new SemanticResultValue("turn left", Commands.LEFT));
      commands.Add(new SemanticResultValue("left", Commands.LEFT));

      //RIGHT//
      commands.Add(new SemanticResultValue("turn right", Commands.RIGHT));
      commands.Add(new SemanticResultValue("right", Commands.RIGHT));

      //PENDOWN//
      commands.Add(new SemanticResultValue("pen down", Commands.PENDOWN));
      commands.Add(new SemanticResultValue("start drawing", Commands.PENDOWN));
      commands.Add(new SemanticResultValue("draw", Commands.PENDOWN));

      //PENUP//
      commands.Add(new SemanticResultValue("pen up", Commands.PENUP));
      commands.Add(new SemanticResultValue("stop drawing", Commands.PENUP));
      commands.Add(new SemanticResultValue("don't draw", Commands.PENUP));
      commands.Add(new SemanticResultValue("do not draw", Commands.PENUP));

      //BIGGER//
      commands.Add(new SemanticResultValue("bigger", Commands.BIGGER));
      commands.Add(new SemanticResultValue("enlarge", Commands.BIGGER));

      //SMALLER//
      commands.Add(new SemanticResultValue("smaller", Commands.SMALLER));
      commands.Add(new SemanticResultValue("shrink", Commands.SMALLER));

      //COLORS//
      commands.Add(new SemanticResultValue("colors", Commands.COLORS));
      commands.Add(new SemanticResultValue("color", Commands.COLORS));

      var gb = new GrammarBuilder { Culture = CultureInfo.GetCultureInfoByIetfLanguageTag("en") };
      gb.Append(commands);

      return new Grammar(gb);
    }

  }

}

