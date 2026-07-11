using Core.Common;

namespace Logic.Parser
{
    public class ParserRegistry : AbstractRegistry<string, IParser>
    {
        public ParserRegistry()
        {
            Register("nbt", new NbtParser());
        }
    }
}