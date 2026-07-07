using Core.Common;

namespace Core.Parser
{
    public class ParserRegistry : AbstractRegistry<string, IParser>
    {
        public ParserRegistry()
        {
            Register("nbt", new NbtParser());
        }
    }
}