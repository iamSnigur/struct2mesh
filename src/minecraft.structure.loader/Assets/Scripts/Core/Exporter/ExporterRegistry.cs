using Core.Common;

namespace Core.Exporter
{
    public class ExporterRegistry : AbstractRegistry<string, IExporter>
    {
        public ExporterRegistry()
        {
            Register("fbx", new FbxExporter());
            Register("glb", new GlbExporter());
            Register("obj", new ObjExporter());
            Register("ply", new PlyExporter());
        }
    }
}