﻿//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: MainWindows.xaml.cs
//Version: 20150828

//Credits:
// based on sample "SpeechBasics-WPF" for C# (https://msdn.microsoft.com/en-us/library/hh855387.aspx)
// from Microsoft Kinect SDK 1.8 (http://www.microsoft.com/en-us/download/details.aspx?id=40278)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using System.Globalization;

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
    const double ConfidenceThreshold = 0.3;

    /// <summary>
    /// Number of pixels turtle should move forwards or backwards each time.
    /// </summary>
    const int DisplacementAmount = 10;

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

    #endregion

    #region --- Initialization ---

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
    }

    #endregion

    #region --- Methods ---

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

      Grammar g = SpeechUtils.CreateGrammarFromXML(); //could use SpeechGrammar_en.Create() to generate the grammar programmatically instead of loading it from an XML (resource) file
      speechEngine.LoadGrammar(g);

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
        switch (e.Result.Semantics.Value.ToString())
        {
          case SpeechCommands.FORWARD:
            RecognitionHighlight(forwardSpan);
            turtleTranslation.X = (playArea.Width + turtleTranslation.X + (DisplacementAmount * Displacements[curDirection].X)) % playArea.Width;
            turtleTranslation.Y = (playArea.Height + turtleTranslation.Y + (DisplacementAmount * Displacements[curDirection].Y)) % playArea.Height;
            break;

          case SpeechCommands.BACKWARD:
            RecognitionHighlight(backSpan);
            turtleTranslation.X = (playArea.Width + turtleTranslation.X - (DisplacementAmount * Displacements[curDirection].X)) % playArea.Width;
            turtleTranslation.Y = (playArea.Height + turtleTranslation.Y - (DisplacementAmount * Displacements[curDirection].Y)) % playArea.Height;
            break;

          case SpeechCommands.LEFT:
            RecognitionHighlight(leftSpan);
            curDirection = (Direction)(((int)curDirection + 270) % 360); //do not use - 90, do not want to end up with negative numbers (plus can't use Math.Abs on the result of the modulo operation, will end up with wrong number)
            turtleRotation.Angle = (int)curDirection;
            break;

          case SpeechCommands.RIGHT:
            RecognitionHighlight(rightSpan);
            curDirection = (Direction)Math.Abs(((int)curDirection + 90) % 360);
            turtleRotation.Angle = (int)curDirection;
            break;

          case SpeechCommands.PENUP:
            RecognitionHighlight(penupSpan);
            MessageBox.Show("Command not yet implemented");
            break;

          case SpeechCommands.PENDOWN:
            RecognitionHighlight(pendownSpan);
            MessageBox.Show("Command not yet implemented");
            break;

          case SpeechCommands.BIGGER:
            RecognitionHighlight(biggerSpan);
            turtleScale.ScaleX *= ScaleFactor;
            turtleScale.ScaleY *= ScaleFactor;
            break;

          case SpeechCommands.SMALLER:
            RecognitionHighlight(smallerSpan);
            turtleScale.ScaleX /= ScaleFactor;
            turtleScale.ScaleY /= ScaleFactor;
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

    #endregion

  }

}
