using System.ComponentModel;
using ViewGrid.Exporting;

namespace ViewGrid.Core;

public sealed class ViewGridControlExportingFacade
{
    private readonly ViewGridControl _owner;

    internal ViewGridControlExportingFacade(ViewGridControl owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public string ExportVisibleCsv(string path, char separator = ';') => _owner.ExportVisibleCsv(path, separator);
    public string ExportVisibleExcel(string path, string worksheetName = "ViewGridControl") => _owner.ExportVisibleExcel(path, worksheetName);
    public string ExportVisiblePdf(string path, string title = "ViewGridControl") => _owner.ExportVisiblePdf(path, title);
    public string ExportVisiblePdf(string path, ViewGridPdfExportOptions options) => _owner.ExportVisiblePdf(path, options);
    public string ExportVisibleHtml(string path, string title = "ViewGridControl") => _owner.ExportVisibleHtml(path, title);

    public string ExportSelectedCsvWithDialog() => _owner.ExportSelectedCsvWithDialog();
    public string ExportVisibleCsvWithDialog() => _owner.ExportVisibleCsvWithDialog();
    public string ExportVisibleExcelWithDialog() => _owner.ExportVisibleExcelWithDialog();
}

public partial class ViewGridControl
{
    private ViewGridControlExportingFacade? _exportingFacade;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ViewGridControlExportingFacade Exporting => _exportingFacade ??= new ViewGridControlExportingFacade(this);
}
