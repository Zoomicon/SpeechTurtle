//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: MainWindows.xaml.cs
//Version: 20151207

//Credits:
// based on sample "SpeechBasics-WPF" for C# (https://msdn.microsoft.com/en-us/library/hh855387.aspx)
// from Microsoft Kinect SDK 1.8 (http://www.microsoft.com/en-us/download/details.aspx?id=40278)

//TODO: Add functionality to Record and name sequences of commands in order to repeat later using their name (find some way to allow recursion down to some max level though)
//TODO: Add history and UNDO/REDO commands (and possibly also display the history like a log with the current position and back being black and the undone stuff that can be redone being Grey) [see History window of Paint.net for example]
//TODO: Apart from saying a color to change pen color, add extra more verbose command to change pen/foreground and background colors (as phrases). See https://msdn.microsoft.com/en-us/library/system.speech.recognition.choices(v=vs.110).aspx on how to do it
//TODO: see open issues section at http://SpeechTurtle.codeplex.com

using SpeechTurtle.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SpeechLib.Recognition;

#if USE_MICROSOFT_SPEECH
using Microsoft.Speech.Recognition;
#else
using System.Speech.Recognition;
#endif

using SpeechLib.Synthesis;
using SpeechLib.Recognition.KinectV1;
using SpeechLib.Models;
using System.Threading;

namespace SpeechTurtle
{

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
      Justification = "In a full-fledged application, the SpeechRecognition object should be properly disposed. Will implement in the future")]
  public partial class MainWindow : Window
  {

    #region --- Constants ---

    /// <summary>
    /// Speech utterance confidence below which we treat speech as if it hadn't been heard.
    /// </summary>
    const double ConfidenceThreshold = 0.8; //use higher values to require more accurate recognition

    /// <summary>
    /// Scaling factor for BIGGER / SMALLER commands.
    /// </summary>
    const double ScaleFactor = 1.5;

    /// <summary>
    /// Map between each direction and the displacement unit it represents.
    /// </summary>
    private static readonly Dictionary<Direction, Point> Displacements = new Dictionary<Direction, Point>
    {
      { Direction.Up, new Point { X = 0, Y = -1 } },
      { Direction.Right, new Point { X = 1, Y = 0 } },
      { Direction.Down, new Point { X = 0, Y = 1 } },
      { Direction.Left, new Point { X = -1, Y = 0 } }
    };

    #endregion

    #region --- Fields ---

    /// <summary>
    /// Speech synthesis .
    /// </summary>
    private ISpeechSynthesis speechSynthesis;

    /// <summary>
    /// Speech recognition  (using audio data from Kinect if connected).
    /// </summary>
    private ISpeechRecognition speechRecognition;

    /// <summary>
    /// Current direction where turtle is facing.
    /// </summary>
    private Direction curDirection = Direction.Up;

    /// <summary>
    /// How much turtle should move forwards or backwards each time.
    /// </summary>
    private double displacementAmount = 50; //assuming initial turtle scale is 1

    /// <summary>
    /// How thick a line the turtle draws when pen is down.
    /// </summary>
    private double penThickness = 10; //assuming initial turtle scale is 1

    /// <summary>
    /// Keeps pen state (down=drawing).
    /// </summary>
    private bool penIsDown = true;

    /// <summary>
    /// Keeps pen color (used when pen is down to draw).
    /// </summary>
    private Color penColor = Colors.Black;

    /// <summary>
    /// Keeps pen brush (used when pen is down to draw).
    /// </summary>
    private Brush penBrush = new SolidColorBrush(Colors.Black);

    #endregion

    #region --- Initialization ---

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
      ShowPenColor();
    }

    /// <summary>
    /// Execute initialization tasks.
    /// </summary>
    protected void Init()
    {
      // This requires that a Kinect is connected at the time of app startup.
      // To make the app robust against plug/unplug,
      // Microsoft recommends using KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
      if (KinectV1Utils.StartKinectSensor() == null)
      {
        statusBarText.Text = Properties.Resources.NoKinectReady;
        imgKinect.Visibility = Visibility.Hidden;
      }
      else
      {
        statusBarText.Text = Properties.Resources.KinectReady;
        imgKinect.Visibility = Visibility.Visible;
      }

      speechSynthesis = new SpeechSynthesis();

      speechRecognition = new SpeechRecognitionKinectV1(); //will fallback to same engine used by SpeechRecognition class automatically if it can't find Kinect V1 sensor

      speechRecognition.LoadGrammar(Properties.Resources.SpeechGrammar_en, "Main"); //could use SpeechGrammar_en.Create() to generate the grammar programmatically instead of loading it from an XML (resource) file
      speechRecognition.LoadGrammar(SpeechRecognitionUtils.CreateGrammarFromNames(ColorUtils.GetKnownColorNames(), "en", "Colors"));

      //setup recognition event handlers
      speechRecognition.Recognized += SpeechRecognition_Recognized;
      speechRecognition.NotRecognized += SpeechRecognition_NotRecognized;

      // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model.
      // This will prevent recognition accuracy from degrading over time.
      //// speechRecognition.AcousticModelAdaptation = false;

      speechRecognition.Start(); //start speech recognition (set to keep on firing speech recognition events, not just once)
    }

    #endregion

    #region --- Cleanup ---

    /// <summary>
    /// Execute cleanup tasks.
    /// </summary>
    protected void Cleanup()
    {
      if (speechRecognition != null)
      {
        speechRecognition.Recognized -= SpeechRecognition_Recognized;
        speechRecognition.NotRecognized -= SpeechRecognition_NotRecognized;
        speechRecognition.Stop();
        (speechRecognition as IDisposable)?.Dispose();
      }

      speechSynthesis = null;
    }

    #endregion

    #region --- Properties ---

    /// <summary>
    /// Returns whether pen is up or down (=drawing).
    /// </summary>
    public bool PenIsDown
    {
      get { return penIsDown; }
      set
      {
        penIsDown = value;
        ShowPenColor();
      }
    }

    /// <summary>
    /// Returns the pen color.
    /// </summary>
    public Color PenColor
    {
      get { return penColor; }
      set
      {
        penColor = value;
        penBrush = new SolidColorBrush(penColor);
        ShowPenColor();
      }
    }

    /// <summary>
    /// Returns the current turtle direction.
    /// </summary>
    public Direction CurrentDirection
    {
      get { return curDirection; }
      set
      {
        curDirection = value;
        turtleRotation.Angle = (int)curDirection;
      }
    }

    /// <summary>
    /// Returns the current turtle position.
    /// </summary>
    public Point TurtlePosition
    {
      get { return new Point(turtleTranslation.X, turtleTranslation.Y); }
      set
      {
        Point lastPos = TurtlePosition;
        turtleTranslation.X = value.X;
        turtleTranslation.Y = value.Y;
        if (PenIsDown)
        {
          Point newPos = TurtlePosition;
          Line line = new Line()
          {
            X1 = lastPos.X,
            Y1 = lastPos.Y,
            X2 = newPos.X,
            Y2 = newPos.Y,
            Stroke = penBrush,
            StrokeThickness = penThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
          };
          line.SetValue(Canvas.ZIndexProperty, 1); //draw over the turtle so that it never hides the shape
          playArea.Children.Add(line);
        }
      }
    }

    #endregion

    #region --- Methods ---

    /// <summary>
    /// Display the current pen color (change the turtle head's fill color if the pen is down).
    /// </summary>
    private void ShowPenColor()
    {
      turtleHead.Fill = (PenIsDown) ? penBrush : (Brush)Resources["KinectPurpleBrush"];
    }

    /// <summary>
    /// Execute a known command by name.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="confidence">The confidence level (has to be above ConfidenceThreshold value)</param>
    public void ExecuteCommand(string command, double confidence)
    {
      ClearCommandHighlights();

      if (command == null) return; //this is needed, else "command.GetKnownColor" will fail if command==null

      if (confidence >= ConfidenceThreshold)
      {
        switch (command)
        {
          case Commands.CLOSE:
            RecognitionHighlight(CLOSE);
            if (!ColorUtils.CloseKnownColorsMessageBox())
              Close(); //if the known colors MessageBox isn't open to close it, close the main app window (exit the app)
            break;

          case Commands.FORWARD:
            RecognitionHighlight(FORWARD);
            TurtlePosition = new Point((playArea.Width + turtleTranslation.X + (displacementAmount * Displacements[CurrentDirection].X)) % playArea.Width,
                                                (playArea.Height + turtleTranslation.Y + (displacementAmount * Displacements[CurrentDirection].Y)) % playArea.Height);
            break;

          case Commands.BACK:
            RecognitionHighlight(BACK);
            TurtlePosition = new Point((playArea.Width + turtleTranslation.X - (displacementAmount * Displacements[CurrentDirection].X)) % playArea.Width,
                                                (playArea.Height + turtleTranslation.Y - (displacementAmount * Displacements[CurrentDirection].Y)) % playArea.Height);
            break;

          case Commands.LEFT:
            RecognitionHighlight(LEFT);
            CurrentDirection = (Direction)(((int)CurrentDirection + 270) % 360); //do not use - 90, do not want to end up with negative numbers (plus can't use Math.Abs on the result of the modulo operation, will end up with wrong number)
            break;

          case Commands.RIGHT:
            RecognitionHighlight(RIGHT);
            CurrentDirection = (Direction)Math.Abs(((int)CurrentDirection + 90) % 360);
            break;

          case Commands.PENDOWN:
            RecognitionHighlight(PENDOWN);
            PenIsDown = true;
            break;

          case Commands.PENUP:
            RecognitionHighlight(PENUP);
            PenIsDown = false;
            break;

          case Commands.BIGGER:
            RecognitionHighlight(BIGGER);
            turtleScale.ScaleX *= ScaleFactor;
            turtleScale.ScaleY *= ScaleFactor;
            displacementAmount *= ScaleFactor; //Bigger turtles move in bigger steps
            penThickness *= ScaleFactor; //Bigger turtles leave thicker trails
            break;

          case Commands.SMALLER:
            RecognitionHighlight(SMALLER);
            turtleScale.ScaleX /= ScaleFactor;
            turtleScale.ScaleY /= ScaleFactor;
            displacementAmount /= ScaleFactor; //Smaller turtles move in smaller steps
            penThickness /= ScaleFactor; //Smaller turtles leave thiner trails
            break;

          case Commands.COLORS:
            RecognitionHighlight(COLORS);
            Dispatcher.BeginInvoke(new Action(() => { ColorUtils.ShowKnownColors(); })); //Do not call colorsHyperlink.DoClick() or ColorUtils.ShowKnownColors() directly since the latter uses MessageBox.Show which would block the speech recognition event thread, so we wouldn't be able to then speak the CLOSE command
            break;

          default:
            RecognitionHighlight(COLORS);
            PenColor = command.GetKnownColor();
            break;
        }
      }
    }

    #region Command highlighting

    /// <summary>
    /// Highlight/Unhighlight command at command instructions.
    /// </summary>
    /// <param name="hyperlink"></param>
    private void RecognitionHighlight(Hyperlink hyperlink, bool highlight = true)
    {
      if (highlight)
      {
        hyperlink.Foreground = Brushes.DeepSkyBlue;
        hyperlink.FontWeight = FontWeights.Bold;
      }
      else
      {
        hyperlink.Foreground = (Brush)Resources["MediumGreyBrush"];
        hyperlink.FontWeight = FontWeights.Normal;
      }
    }

    /// <summary>
    /// Remove any highlighting from command instructions.
    /// </summary>
    private void ClearCommandHighlights()
    {
      foreach (Inline inline in txtCommands.Inlines)
      {
        Hyperlink hyperlink = inline as Hyperlink;
        if (hyperlink != null)
          RecognitionHighlight(hyperlink, false);
      }
    }

    #endregion

    #endregion

    #region --- Events ---

    #region Speech events

    /// <summary>
    /// Handler for recognized speech events.
    /// </summary>
    /// <param name="sender">object sending the event.</param>
    /// <param name="e">event arguments.</param>
    private void SpeechRecognition_Recognized(object sender, SpeechRecognitionEventArgs e)
    {
      ExecuteCommand(e.command, e.confidence);
    }

    /// <summary>
    /// Handler for not recognized speech events.
    /// </summary>
    /// <param name="sender">object sending the event.</param>
    /// <param name="e">event arguments.</param>
    private void SpeechRecognition_NotRecognized(object sender, EventArgs e)
    {
      ClearCommandHighlights();
    }

    #endregion

    /// <summary>
    /// Window loaded event handler.
    /// </summary>
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      Init();
    }

    /// <summary>
    /// Window closing event handler.
    /// </summary>
    /// <param name="sender">object sending the event.</param>
    /// <param name="e">event arguments.</param>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
      Cleanup();
    }

    #region Commands

    public void SpeakCommand(string command)
    {
      //speechRecognition.Pause(); //TODO: not working correctly when async speech recognition method is used
      speechSynthesis.Speak(command);
      //speechRecognition.Resume();
    }

    /// <summary>
    /// Handles the Click event of the commands' Hyperlink controls.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void Command_Click(object sender, RoutedEventArgs e)
    {
      string command = ((Hyperlink)sender).Name;
      SpeakCommand(command);
      ExecuteCommand(command, confidence: 1);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
      string command = null;
      if (Commands.CommandShortcuts.TryGetValue(e.Key, out command))
        ExecuteCommand(command, confidence: 1);
    }

    #endregion

    #endregion

  }

}
