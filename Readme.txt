SpeechTurtle - http://SpeechTurtle.codeplex.com

Project Description
--------------------
Control a Turtle character on a 2D canvas using speech, either using the default audio input source on Windows, or using Microsoft Kinect
Supports Windows speech recognition via the system's default audio input source 
Supports Kinect-based speech recognition (currently via Kinect for Xbox 360 or Kinect for Windows v1, via Kinect for Windows 1.8 SDK)

Requirements
-------------
To Build/Run the source code in recent Visual Studio you need Kinect for Windows SDK 1.8 installed 
To Run with Kinect for Windows v1 sensor you just need Kinect for Windows Runtime 1.8 (unless you have Kinect for Xbox 360 sensor, in which case you need the Kinect for Windows SDK 1.8 instead - it does contains the Runtime too) 
Microsoft Windows Speech API (SAPI), included in Windows Vista and after

UI
---
- MainWindow
- Known Colors dialog

Credits
--------
Based on the sample "SpeechBasics-WPF" for C# (https://msdn.microsoft.com/en-us/library/hh855387.aspx from Microsoft Kinect SDK 1.8 (http://www.microsoft.com/en-us/download/details.aspx?id=40278)
