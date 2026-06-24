using Taylan.Pano.Columns;

namespace Taylan.Pano.Editing;

public sealed class CheckBoxCellEditor : ICellEditor
{
    public Control CreateEditor(Control owner, Rectangle bounds, PanoColumn column, object row, object? value)
    {
        var check = new CheckBox
        {
            ThreeState = false,
            Checked = value != null && value != DBNull.Value && Convert.ToBoolean(value),
            Text = string.Empty,
            TextAlign = ContentAlignment.MiddleCenter,
            CheckAlign = ContentAlignment.MiddleCenter
        };
        check.Bounds = bounds;
        owner.Controls.Add(check);
        check.Focus();
        return check;
    }

    public object? GetEditedValue(Control editor) => ((CheckBox)editor).Checked;
}

public sealed class NumericCellEditor : ICellEditor
{
    public Control CreateEditor(Control owner, Rectangle bounds, PanoColumn column, object row, object? value)
    {
        var numeric = new NumericUpDown
        {
            BorderStyle = BorderStyle.FixedSingle,
            DecimalPlaces = column.NumericDecimalPlaces,
            Minimum = column.NumericMinimum,
            Maximum = column.NumericMaximum,
            ThousandsSeparator = true
        };
        try
        {
            if (value != null && value != DBNull.Value)
            {
                var d = Convert.ToDecimal(value);
                if (d < numeric.Minimum) d = numeric.Minimum;
                if (d > numeric.Maximum) d = numeric.Maximum;
                numeric.Value = d;
            }
        }
        catch { }
        numeric.Bounds = bounds;
        owner.Controls.Add(numeric);
        numeric.Focus();
        numeric.Select(0, numeric.Text.Length);
        return numeric;
    }

    public object? GetEditedValue(Control editor) => ((NumericUpDown)editor).Value;
}

public sealed class DateTimeCellEditor : ICellEditor
{
    public Control CreateEditor(Control owner, Rectangle bounds, PanoColumn column, object row, object? value)
    {
        var picker = new DateTimePicker
        {
            Format = column.DateTimeFormat == PanoDateTimeEditorFormat.Custom ? DateTimePickerFormat.Custom : DateTimePickerFormat.Short,
            CustomFormat = column.DateTimeCustomFormat,
            ShowCheckBox = column.DateTimeNullable
        };
        if (value == null || value == DBNull.Value)
        {
            picker.Checked = false;
        }
        else
        {
            try { picker.Value = Convert.ToDateTime(value); picker.Checked = true; } catch { picker.Checked = false; }
        }
        picker.Bounds = bounds;
        owner.Controls.Add(picker);
        picker.Focus();
        return picker;
    }

    public object? GetEditedValue(Control editor)
    {
        var picker = (DateTimePicker)editor;
        return picker.ShowCheckBox && !picker.Checked ? null : picker.Value;
    }
}

public enum PanoDateTimeEditorFormat
{
    Short,
    Custom
}
