//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: MainWindows.xaml.cs
//Version: 20150902

//Credits:
// based on sample "SpeechBasics-WPF" for C# (https://msdn.microsoft.com/en-us/library/hh855387.aspx)
// from Microsoft Kinect SDK 1.8 (http://www.microsoft.com/en-us/download/details.aspx?id=40278)

//TODO: Add functionality to Record and name sequences of commands in order to repeat later using their name (find some way to allow recursion down to some max level though)
//TODO: Add history and UNDO/REDO commands (and possibly also display the history like a log with the current position and back being black and the undone stuff that can be redone being grey)
//TODO: Apart from saying a color to change pen color, add extra more verbose command to change pen/foreground and background colors (as phrases). See https://msdn.microsoft.com/en-us/library/system.speech.recognition.choices(v=vs.110).aspx on how to do it

using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using SpeechTurtle.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SpeechTurtle
{

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
      Justification = "In a full-fledged application, the SpeechRecognitionEngine object should be properly disposed. Will implement in the future")]
  public partial class MainWindow : Window
  {

    #region --- Constants ---

    /// <summary>
    /// Speech utterance confidence below which we treat speech as if it hadn't been heard.
    /// </summary>
    const double ConfidenceThreshold = 0.3; //use higher values to require more accurate recognition

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
    /// Active Kinect sensor.
    /// </summary>
    private KinectSensor sensor;

    /// <summary>
    /// Speech recognition engine using audio data from Kinect.
    /// </summary>
    private SpeechRecognitionEngine speechEngine;

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
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
      sensor = KinectUtils.StartKinectSensor(); // This requires that a Kinect is connected at the time of app startup.
      // To make the app robust against plug/unplug,
      // Microsoft recommends using KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).

      RecognizerInfo ri = null;
      if (sensor == null)
      {
        statusBarText.Text = Properties.Resources.NoKinectReady;
        imgKinect.Visibility = Visibility.Hidden;
      }
      else
      {
        statusBarText.Text = Properties.Resources.KinectReady;
        imgKinect.Visibility = Visibility.Visible;
        ri = KinectUtils.GetKinectRecognizer(CultureInfo.GetCultureInfoByIetfLanguageTag("en-US"));
      }
      speechEngine = (ri != null) ? new SpeechRecognitionEngine(ri.Id) : new SpeechRecognitionEngine();

      speechEngine.LoadGrammar(SpeechUtils.CreateGrammarFromXML(Properties.Resources.SpeechGrammar_en, "Main")); //could use SpeechGrammar_en.Create() to generate the grammar programmatically instead of loading it from an XML (resource) file
      speechEngine.LoadGrammar(SpeechUtils.CreateGrammarFromNames(ColorUtils.GetKnownColorNames(), "en", "Colors"));

      //setup recognition event handlers
      speechEngine.SpeechRecognized += SpeechRecognized;
      speechEngine.SpeechRecognitionRejected += SpeechRejected;

      // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model.
      // This will prevent recognition accuracy from degrading over time.
      ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

      if (sensor != null)
        speechEngine.SetInputToKinectSensor(sensor);
      else
        speechEngine.SetInputToDefaultAudioDevice();

      speechEngine.RecognizeAsync(RecognizeMode.Multiple); //start speech recognition (set to keep on firing speech recognition events, not just once)
    }

    #endregion

    #region --- Cleanup ---

    /// <summary>
    /// Execute uninitialization tasks.
    /// </summary>
    /// <param name="sender">object sending the event.</param>
    /// <param name="e">event arguments.</param>
    private void WindowClosing(object sender, CancelEventArgs e)
    {
      if (null != sensor)
      {
        sensor.AudioSource.Stop();
        sensor.Stop();
        sensor = null;
      }

      if (null != speechEngine)
      {
        speechEngine.SpeechRecognized -= SpeechRecognized;
        speechEngine.SpeechRecognitionRejected -= SpeechRejected;
        speechEngine.RecognizeAsyncStop();
      }
    }

    #endregion

    #region --- Properties ---

    public bool PenIsDown {
      get { return penIsDown; }
      set
      {
        penIsDown = value;
        ShowPenColor();
      }
    }

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

    public Direction CurrentDirection
    {
      get { return curDirection; }
      set
      {
        curDirection = value;
        turtleRotation.Angle = (int)curDirection;
      }
    }

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
            X1 = lastPos.X, Y1 = lastPos.Y,
            X2 = newPos.X, Y2 = newPos.Y,
            Stroke = penBrush,
            StrokeThickness = penThickness,
            StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
          };
          line.SetValue(Canvas.ZIndexProperty, 1); //draw over the turtle so that it never hides the shape
          playArea.Children.Add(line);
        }
      }
    }

    #endregion

    #region --- Methods ---

    private void ShowPenColor()
    {
      turtleHead.Fill = (PenIsDown) ? penBrush : (Brush)Resources["KinectPurpleBrush"];
    }

    #region Recognition highlighting

    /// <summary>
    /// Highlight/Unhighlight command at recognition instructions.
    /// </summary>
    /// <param name="span"></param>
    private void RecognitionHighlight(Span span, bool highlight = true)
    {
      if (highlight)
      {
        span.Foreground = Brushes.DeepSkyBlue;
        span.FontWeight = FontWeights.Bold;
      }
      else
      {
        span.Foreground = (Brush)Resources["MediumGreyBrush"];
        span.FontWeight = FontWeights.Normal;
      }
    }

    /// <summary>
    /// Remove any highlighting from recognition instructions.
    /// </summary>
    private void ClearRecognitionHighlights()
    {
      foreach (Inline inline in txtSpeechCommands.Inlines)
      {
        Span span = inline as Span;
        if (span != null)
          RecognitionHighlight(span, false);
      }
    }

    #endregion

    #endregion

    #region --- Events ---

    /// <summary>
    /// Handler for recognized speech events.
    /// </summary>
    /// <param name="sender">object sending the event.</param>
    /// <param name="e">event arguments.</param>
    private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
      ClearRecognitionHighlights();

      if (e.Result.Confidence >= ConfidenceThreshold)
      {
        string command = e.Result.Semantics.Value.ToString();
        switch (command)
        {
          case SpeechCommands.FORWARD:
            RecognitionHighlight(forwardSpan);
            TurtlePosition = new Point((playArea.Width + turtleTranslation.X + (displacementAmount * Displacements[CurrentDirection].X)) % playArea.Width,
                                        (playArea.Height + turtleTranslation.Y + (displacementAmount * Displacements[CurrentDirection].Y)) % playArea.Height);
            break;

          case SpeechCommands.BACKWARD:
            RecognitionHighlight(backSpan);
            TurtlePosition = new Point((playArea.Width + turtleTranslation.X - (displacementAmount * Displacements[CurrentDirection].X)) % playArea.Width,
                                        (playArea.Height + turtleTranslation.Y - (displacementAmount * Displacements[CurrentDirection].Y)) % playArea.Height);
            break;

          case SpeechCommands.LEFT:
            RecognitionHighlight(leftSpan);
            CurrentDirection = (Direction)(((int)CurrentDirection + 270) % 360); //do not use - 90, do not want to end up with negative numbers (plus can't use Math.Abs on the result of the modulo operation, will end up with wrong number)
            break;

          case SpeechCommands.RIGHT:
            RecognitionHighlight(rightSpan);
            CurrentDirection = (Direction)Math.Abs(((int)CurrentDirection + 90) % 360);
            break;

          case SpeechCommands.PENDOWN:
            RecognitionHighlight(pendownSpan);
            PenIsDown = true;
            break;

          case SpeechCommands.PENUP:
            RecognitionHighlight(penupSpan);
            PenIsDown = false;
            break;

          case SpeechCommands.BIGGER:
            RecognitionHighlight(biggerSpan);
            turtleScale.ScaleX *= ScaleFactor;
            turtleScale.ScaleY *= ScaleFactor;
            displacementAmount *= ScaleFactor; //Bigger turtles move in bigger steps
            penThickness *= ScaleFactor; //Bigger turtles leave thicker trails
            break;

          case SpeechCommands.SMALLER:
            RecognitionHighlight(smallerSpan);
            turtleScale.ScaleX /= ScaleFactor;
            turtleScale.ScaleY /= ScaleFactor;
            displacementAmount /= ScaleFactor; //Smaller turtles move in smaller steps
            penThickness /= ScaleFactor; //Smaller turtles leave thiner trails
            break;

          default:
            PenColor = command.GetKnownColor();
            break;
        }

      }
    }

    /// <summary>
    /// Handler for rejected speech events.
    /// </summary>
    /// <param name="sender">object sending the event.</param>
    /// <param name="e">event arguments.</param>
    private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
    {
      ClearRecognitionHighlights();
    }

    private void colorsHyperlink_Click(object sender, RoutedEventArgs e)
    {
      ColorUtils.ShowKnownColors();
    }

    #endregion

  }

}
