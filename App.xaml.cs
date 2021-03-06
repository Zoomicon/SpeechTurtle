﻿//Project: SpeechTurtle (http://SpeechTurtle.codeplex.com)
//Filename: App.xaml.cs
//Version: 20151208

namespace SpeechTurtle
{
  using System;
  using System.Windows;

  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {

    #region --- Events ---

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      Exception outer = e.Exception;
      Exception inner = outer.InnerException;
      MessageBox.Show((inner ?? outer).Message);

      e.Handled = true; //handle the exception
      Shutdown(); //gracefully shutdown //TODO: could check here if the UI has loaded OK and in that case not shutdown maybe
    }

    #endregion

  }
}
