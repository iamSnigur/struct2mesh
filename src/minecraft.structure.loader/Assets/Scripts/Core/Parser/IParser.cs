using Core.Parser.Model;
using Cysharp.Threading.Tasks;

namespace Core.Parser
{
    public interface IParser
    {
        UniTask<ParsedStructure> Parse(byte[] fileContent);
    }
}