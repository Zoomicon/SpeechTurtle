//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: KinectUtils.cs
//Version: 20150828

using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Globalization;
using System.IO;

namespace SpeechTurtle
{
  public static class KinectUtils
  {

    public static KinectSensor StartKinectSensor()
    {
      // Look through all sensors and return the first connected one that can be started succesfully, else return null.
      foreach (KinectSensor sensor in KinectSensor.KinectSensors)
        if (sensor.Status == KinectStatus.Connected)
          try
          {
            sensor.Start(); // Start the sensor!
            return sensor; // return if started successfully
          }
          catch (IOException) // Some other application is streaming from the same Kinect sensor
          {
            //NOP
          }

      return null;
    }

    /// <summary>
    /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
    /// process audio from Kinect device.
    /// </summary>
    /// <returns>
    /// RecognizerInfo if found, <code>null</code> otherwise.
    /// </returns>
    public static RecognizerInfo GetKinectRecognizer(CultureInfo culture)
    {
      foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
      {
        string value;
        recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
        if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) &&
             culture.Name.Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
          return recognizer;
      }
      return null;
    }

    public static void SetInputToKinectSensor(this SpeechRecognitionEngine speechEngine, KinectSensor sensor, SpeechAudioFormatInfo speechAudioFormat = null)
    {
      if (speechAudioFormat == null)
        speechAudioFormat = new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null); //default input audio format (taken from SpeechBasics-WPF C# sample of Kinect SDK 1.8)

      speechEngine.SetInputToAudioStream(sensor.AudioSource.Start(), speechAudioFormat);
    }

  }
}
