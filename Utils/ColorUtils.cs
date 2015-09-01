//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: ColorUtils.cs
//Version: 20150901

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace SpeechTurtle.Utils
{
  /// <summary>
  /// Color-related utility methods
  /// </summary>
  public static class ColorUtils //based on http://stackoverflow.com/questions/4475391/wpf-silverlight-find-the-name-of-a-color
  {
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

    #endregion
  }
}
