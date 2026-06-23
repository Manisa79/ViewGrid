using ViewGrid.Localization;
using System.Text.Json;

namespace ViewGrid.TestApp;

public sealed partial class StartupLanguageForm : Form
{
    public ViewGridLanguage SelectedLanguage { get; private set; } = ViewGridLanguage.Auto;
    public bool RememberSelection => chkRemember.Checked;

    public StartupLanguageForm(ViewGridLanguage initialLanguage)
    {
        InitializeComponent();

        foreach (var lang in ViewGridLocalization.SupportedLanguages)
            cmbLanguage.Items.Add(new LanguageChoice(lang));

        var selected = cmbLanguage.Items.Cast<LanguageChoice>().FirstOrDefault(x => x.Language == initialLanguage)
            ?? cmbLanguage.Items.Cast<LanguageChoice>().First();
        cmbLanguage.SelectedItem = selected;

        cmbLanguage.SelectedIndexChanged += (_, __) => RefreshTexts();
        btnContinue.Click += (_, __) => SelectedLanguage = ((LanguageChoice)cmbLanguage.SelectedItem!).Language;
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        var lang = cmbLanguage.SelectedItem is LanguageChoice choice ? choice.Language : ViewGridLanguage.Auto;
        ViewGridLocalization.Use(lang);

        Text = lang switch
        {
            ViewGridLanguage.Turkish => "ViewGrid TestApp - Dil Seçimi",
            ViewGridLanguage.English => "ViewGrid TestApp - Language",
            _ => "ViewGrid TestApp - Language"
        };

        lblTitle.Text = lang == ViewGridLanguage.Turkish ? "ViewGrid TestApp Dil Seçimi" : "ViewGrid TestApp Language";
        lblInfo.Text = lang == ViewGridLanguage.Turkish
            ? "ViewGrid dahili menüleri, pencereleri ve örnek ekranları için kullanılacak dili seçin."
            : "Select the language used by ViewGrid built-in menus, dialogs and sample screens.";
        lblLanguage.Text = lang == ViewGridLanguage.Turkish ? "Dil" : "Language";
        chkRemember.Text = lang == ViewGridLanguage.Turkish ? "Seçimimi hatırla" : "Remember my selection";
        btnContinue.Text = lang == ViewGridLanguage.Turkish ? "Devam" : "Continue";
        btnCancel.Text = lang == ViewGridLanguage.Turkish ? "İptal" : "Cancel";
    }

    private sealed class LanguageChoice
    {
        public LanguageChoice(ViewGridLanguage language) => Language = language;
        public ViewGridLanguage Language { get; }
        public override string ToString() => ViewGridLocalization.DisplayName(Language);
    }

    public static ViewGridLanguage LoadSavedLanguage(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return ViewGridLanguage.Auto;
            var data = JsonSerializer.Deserialize<LanguageSettings>(File.ReadAllText(filePath));
            return ViewGridLocalization.FromName(data?.Language, ViewGridLanguage.Auto);
        }
        catch
        {
            return ViewGridLanguage.Auto;
        }
    }

    public static void SaveLanguage(string filePath, ViewGridLanguage language)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var json = JsonSerializer.Serialize(new LanguageSettings { Language = language.ToString() }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // TestApp should not fail just because settings cannot be written.
        }
    }

    private sealed class LanguageSettings
    {
        public string? Language { get; set; }
    }
}
