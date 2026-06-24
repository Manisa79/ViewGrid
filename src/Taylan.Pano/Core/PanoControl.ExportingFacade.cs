using System.ComponentModel;
using Taylan.Pano.Exporting;

namespace Taylan.Pano.Core;

public sealed class PanoControlExportingFacade
{
    private readonly PanoControl _owner;

    internal PanoControlExportingFacade(PanoControl owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public string ExportVisibleCsv(string path, char separator = ';') => _owner.ExportVisibleCsv(path, separator);
    public string ExportVisibleExcel(string path, string worksheetName = "PanoControl") => _owner.ExportVisibleExcel(path, worksheetName);
    public string ExportVisiblePdf(string path, string title = "PanoControl") => _owner.ExportVisiblePdf(path, title);
    public string ExportVisiblePdf(string path, PanoPdfExportOptions options) => _owner.ExportVisiblePdf(path, options);
    public string ExportVisibleHtml(string path, string title = "PanoControl") => _owner.ExportVisibleHtml(path, title);

    public string ExportSelectedCsvWithDialog() => _owner.ExportSelectedCsvWithDialog();
    public string ExportVisibleCsvWithDialog() => _owner.ExportVisibleCsvWithDialog();
    public string ExportVisibleExcelWithDialog() => _owner.ExportVisibleExcelWithDialog();
}

public partial class PanoControl
{
    private PanoControlExportingFacade? _exportingFacade;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PanoControlExportingFacade Exporting => _exportingFacade ??= new PanoControlExportingFacade(this);
}
