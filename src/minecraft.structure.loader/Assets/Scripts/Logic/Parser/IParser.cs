using Cysharp.Threading.Tasks;

namespace Logic.Parser
{
    public interface IParser
    {
        UniTask<ParserResult> Parse(byte[] fileContent);
    }
}