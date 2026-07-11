using Cysharp.Threading.Tasks;
using Logic.Parser.Model;

namespace Logic.Parser
{
    public interface IParser
    {
        UniTask<ParsedStructure> Parse(byte[] fileContent);
    }
}