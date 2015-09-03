//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: Commands.cs
//Version: 20150903

using System.Collections.Generic;
using System.Windows.Input;

namespace SpeechTurtle
{
  public static class Commands
  {

    #region --- Constants ---

    public const string CLOSE = "CLOSE";
    //
    public const string FORWARD = "FORWARD";
    public const string BACK = "BACK";
    //
    public const string LEFT = "LEFT";
    public const string RIGHT = "RIGHT";
    //
    public const string PENDOWN = "PENDOWN";
    public const string PENUP = "PENUP";
    //
    public const string BIGGER = "BIGGER";
    public const string SMALLER = "SMALLER";
    //
    public const string COLORS = "COLORS";

    public static readonly Dictionary<Key, string> CommandShortcuts = new Dictionary<Key, string>()
    {
      {Key.O, CLOSE},
      {Key.F, FORWARD},
      {Key.B, BACK},
      {Key.L, LEFT},
      {Key.R, RIGHT},
      {Key.D, PENDOWN},
      {Key.U, PENUP},
      {Key.G, BIGGER},
      {Key.S, SMALLER},
      {Key.C, COLORS}
    };

    #endregion

  }

}

