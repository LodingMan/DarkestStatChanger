namespace DarkestStatChanger.Models
{
    public class FileLine
    {
        public string OriginalText { get; set; }
        public bool IsParsed { get; set; }
        public string ParsedType { get; set; }
        public int DataIndex { get; set; } = -1;
    }
}
