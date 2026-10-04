using Logic.Parser.Model;

namespace Logic.Parser
{
    public record ParserResult
    {
        public ParsedStructure Structure;
        public bool IsSuccess;
        public string Error;

        public static ParserResult Succeeded(ParsedStructure structure) =>
            new() { Structure = structure, IsSuccess = true };

        public static ParserResult Failed(string error) =>
            new() { Error = error, IsSuccess = false };
    }
}