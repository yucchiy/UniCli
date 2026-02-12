using System.Text;

namespace UniCli.Server.Editor
{
    public interface IFormatWriter
    {
        void WriteLine(string line);
    }

    internal sealed class StringFormatWriter : IFormatWriter
    {
        private readonly StringBuilder _sb;
        private bool _hasContent;

        public StringFormatWriter(StringBuilder sb)
        {
            _sb = sb;
        }

        public void WriteLine(string line)
        {
            if (_hasContent)
                _sb.AppendLine();
            _sb.Append(line);
            _hasContent = true;
        }

        public override string ToString() => _sb.ToString();
    }
}
