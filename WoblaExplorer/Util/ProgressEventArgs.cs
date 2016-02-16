using System;

namespace WoblaExplorer.Util
{
    public class ProgressEventArgs : EventArgs
    {
        public string Message { get; set; }
        public double Percent { get; set; }

        public ProgressEventArgs(double percent)
        {
            Percent = percent;
        }

        public ProgressEventArgs(string message, double percent)
        {
            Message = message;
            Percent = percent;
        }
    }
}
