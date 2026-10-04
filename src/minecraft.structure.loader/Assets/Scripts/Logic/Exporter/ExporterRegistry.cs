using Core.Common;

namespace Logic.Exporter
{
    public class ExporterRegistry : AbstractRegistry<ExportType, IExporter>
    {
        public ExporterRegistry()
        {
            Register(ExportType.AsciiFbx, new FbxExporter());
            Register(ExportType.Glb, new GlbExporter());
            Register(ExportType.Obj, new ObjExporter());
            Register(ExportType.Ply, new PlyExporter());
        }
    }
}