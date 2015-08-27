//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: MainWindows.xaml.cs
//Version: 20150828

//Credits:
// based on sample "SpeechBasics-WPF" for C# (https://msdn.microsoft.com/en-us/library/hh855387.aspx)
// from Microsoft Kinect SDK 1.8 (http://www.microsoft.com/en-us/download/details.aspx?id=40278)

namespace SpeechTurtle
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.IO;
  using System.Text;
  using System.Windows;
  using System.Windows.Documents;
  using System.Windows.Media;
  using Microsoft.Kinect;
  using Microsoft.Speech.Recognition;
  using Microsoft.Speech.AudioFormat;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
      Justification = "In a full-fledged application, the SpeechRecognitionEngine object should be properly disposed. Will implement in the future")]
  public partial class MainWindow : Window
  {

    /// <summary>
    /// Enumeration of directions in which turtle may be facing (using clock-wise degrees for values)
    /// </summary>
    private enum Direction
    {
      Up = 0,
      Right = 90,
      Down = 180,
      Left = 270
    }

    /// <summary>
    /// Resource key for medium-gray-colored brush.
    /// </summary>
    private const string UnrecognizedSpanForegroundKey = "MediumGreyBrush";

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
    /// List of all UI span elements used to select recognized text.
    /// </summary>
    private List<Span> recognitionSpans;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
      recognitionSpans = new List<Span> { forwardSpan, backSpan, rightSpan, leftSpan };
    }

    /// <summary>
    /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
    /// process audio from Kinect device.
    /// </summary>
    /// <returns>
    /// RecognizerInfo if found, <code>null</code> otherwise.
    /// </returns>
    private static RecognizerInfo GetKinectRecognizer()
    {
      foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
      {
        string value;
        recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
        if ( "True".Equals(value, StringComparison.OrdinalIgnoreCase) &&
             "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase) )
          return recognizer;
      }
      return null;
    }

    /// <summary>
    /// Execute initialization tasks.
    /// </summary>
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
      // Look through all sensors and start the first connected one.
      // This requires that a Kinect is connected at the time of app startup.
      // To make the app robust against plug/unplug,
      // Microsoft recommends using KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
      foreach (var potentialSensor in KinectSensor.KinectSensors)
        if (potentialSensor.Status == KinectStatus.Connected)
        {
          sensor = potentialSensor;
          break;
        }

      if (sensor != null)
      {
        try
        {
          sensor.Start(); // Start the sensor!
        }
        catch (IOException) // Some other application is streaming from the same Kinect sensor
        {
          sensor = null;
        }
      }

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
        ri = GetKinectRecognizer();
      }
      speechEngine = (ri != null) ? new SpeechRecognitionEngine(ri.Id) : new SpeechRecognitionEngine();

      Grammar g = CreateGrammarFromXML(); //could use SpeechGrammar_en.Create() to generate the grammar programmatically instead of loading it from an XML (resource) file
      speechEngine.LoadGrammar(g);

      //setup recognition event handlers
      speechEngine.SpeechRecognized += SpeechRecognized;
      speechEngine.SpeechRecognitionRejected += SpeechRejected;

      // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model.
      // This will prevent recognition accuracy from degrading over time.
      ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

      if (sensor != null)
        speechEngine.SetInputToAudioStream(
         sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
      else
        speechEngine.SetInputToDefaultAudioDevice();
      speechEngine.RecognizeAsync(RecognizeMode.Multiple);
    }

    /// <summary>
    /// Create a grammar from grammar definition XML (resource) file.
    /// </summary>
    /// <returns>Grammar</returns>
    private Grammar CreateGrammarFromXML()
    {
      using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.SpeechGrammar_en)))
        return new Grammar(memoryStream);
    }

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

    /// <summary>
    /// Remove any highlighting from recognition instructions.
    /// </summary>
    private void ClearRecognitionHighlights()
    {
      foreach (Span span in recognitionSpans)
      {
        span.Foreground = (Brush)Resources[UnrecognizedSpanForegroundKey];
        span.FontWeight = FontWeights.Normal;
      }
    }

    /// <summary>
    /// Handler for recognized speech events.
    /// </summary>
    /// <param name="sender">object sending the event.</param>
    /// <param name="e">event arguments.</param>
    private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
      // Speech utterance confidence below which we treat speech as if it hadn't been heard
      const double ConfidenceThreshold = 0.3;

      // Number of pixels turtle should move forwards or backwards each time.
      const int DisplacementAmount = 60;

      ClearRecognitionHighlights();

      if (e.Result.Confidence >= ConfidenceThreshold)
      {
        switch (e.Result.Semantics.Value.ToString())
        {
          case "FORWARD":
            forwardSpan.Foreground = Brushes.DeepSkyBlue;
            forwardSpan.FontWeight = FontWeights.Bold;
            turtleTranslation.X = (playArea.Width + turtleTranslation.X + (DisplacementAmount * Displacements[curDirection].X)) % playArea.Width;
            turtleTranslation.Y = (playArea.Height + turtleTranslation.Y + (DisplacementAmount * Displacements[curDirection].Y)) % playArea.Height;
            break;

          case "BACKWARD":
            backSpan.Foreground = Brushes.DeepSkyBlue;
            backSpan.FontWeight = FontWeights.Bold;
            turtleTranslation.X = (playArea.Width + turtleTranslation.X - (DisplacementAmount * Displacements[curDirection].X)) % playArea.Width;
            turtleTranslation.Y = (playArea.Height + turtleTranslation.Y - (DisplacementAmount * Displacements[curDirection].Y)) % playArea.Height;
            break;

          case "LEFT":
            leftSpan.Foreground = Brushes.DeepSkyBlue;
            leftSpan.FontWeight = FontWeights.Bold;
            curDirection = (Direction)(((int)curDirection + 270) % 360); //do not use - 90, do not want to end up with negative numbers (plus can't use Math.Abs on the result of the modulo operation, will end up with wrong number)
            turtleRotation.Angle = (int)curDirection;
            break;

          case "RIGHT":
            rightSpan.Foreground = Brushes.DeepSkyBlue;
            rightSpan.FontWeight = FontWeights.Bold;
            curDirection = (Direction)Math.Abs(((int)curDirection + 90) % 360);
            turtleRotation.Angle = (int)curDirection;
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

  }
}