using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ConnectServer
{
    public static class Logs
    {
        public static void WriteLog(string color, string text, params object[] arg0)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                MainWindow window = (MainWindow)Application.Current.MainWindow;
                RtbAppendText(window.csLogsBox, string.Format(text, arg0) + Environment.NewLine, color);
            });
        }

        private static void RtbAppendText(RichTextBox box, string text, string color)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                    bc.ConvertFromString(color));
            }
            catch (FormatException) { }

            box.ScrollToEnd();
        }
    }
}
