//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: ColorUtils.cs
//Version: 20150903

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace SpeechTurtle.Utils
{
  /// <summary>
  /// Color-related utility methods
  /// </summary>
  public static class ColorUtils //based on http://stackoverflow.com/questions/4475391/wpf-silverlight-find-the-name-of-a-color
  {

    #region --- Constants ---

    private const string KNOWN_COLORS_MESSAGEBOX_CAPTION = "Known colors";

    #endregion

    #region --- Win32 Interop ---

    private const int WM_CLOSE = 0x0010;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

    #endregion

    #region --- Fields ---

    private static Dictionary<string, Color> knownColors; //=null

    #endregion

    #region --- Methods ---

    #region Extension methods

    public static string GetKnownColorName(this Color color)
    {
      return GetKnownColors()
          .Where(kvp => kvp.Value.Equals(color))
          .Select(kvp => kvp.Key)
          .FirstOrDefault();
    }

    public static Color GetKnownColor(this string name)
    {
      Color color;
      return GetKnownColors().TryGetValue(name, out color) ? color : Colors.Black; //if color for name is not found, return black
    }

    #endregion

    public static Dictionary<string, Color> GetKnownColors()
    {
      if (knownColors == null)
      {
        var colorProperties = typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.Public);
        knownColors = colorProperties.ToDictionary(
          p => p.Name,
          p => (Color)p.GetValue(null, null));
      }
      return knownColors;
    }

    public static string[] GetKnownColorNames()
    {
      return GetKnownColors().Keys.ToArray();
    }

    public static void ShowKnownColors()
    {
      MessageBox.Show(string.Join(", ", GetKnownColorNames()), KNOWN_COLORS_MESSAGEBOX_CAPTION);
    }

    public static bool CloseKnownColorsMessageBox() //based on DmitryG's answer at http://stackoverflow.com/questions/14522540/close-a-messagebox-after-several-seconds
    {
      IntPtr mbWnd = FindWindow("#32770", KNOWN_COLORS_MESSAGEBOX_CAPTION); // lpClassName is #32770 for MessageBox
      if (mbWnd == IntPtr.Zero)
        return false; //Known colors MessageBox not found

      SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); //close that MessageBox
      return true;
    }

    #endregion
  }
}
