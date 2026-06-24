using Taylan.Pano.Localization;
using System.Text.Json;

namespace Taylan.Pano.TestApp;

public sealed partial class StartupLanguageForm : Form
{
    public PanoLanguage SelectedLanguage { get; private set; } = PanoLanguage.Auto;
    public bool RememberSelection => chkRemember.Checked;

    public StartupLanguageForm(PanoLanguage initialLanguage)
    {
        InitializeComponent();

        foreach (var lang in PanoLocalization.SupportedLanguages)
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
        var lang = cmbLanguage.SelectedItem is LanguageChoice choice ? choice.Language : PanoLanguage.Auto;
        PanoLocalization.Use(lang);

        Text = lang switch
        {
            PanoLanguage.Turkish => "Pano TestApp - Dil Seçimi",
            PanoLanguage.English => "Pano TestApp - Language",
            _ => "Pano TestApp - Language"
        };

        lblTitle.Text = lang == PanoLanguage.Turkish ? "Pano TestApp Dil Seçimi" : "Pano TestApp Language";
        lblInfo.Text = lang == PanoLanguage.Turkish
            ? "Pano dahili menüleri, pencereleri ve örnek ekranları için kullanılacak dili seçin."
            : "Select the language used by Pano built-in menus, dialogs and sample screens.";
        lblLanguage.Text = lang == PanoLanguage.Turkish ? "Dil" : "Language";
        chkRemember.Text = lang == PanoLanguage.Turkish ? "Seçimimi hatırla" : "Remember my selection";
        btnContinue.Text = lang == PanoLanguage.Turkish ? "Devam" : "Continue";
        btnCancel.Text = lang == PanoLanguage.Turkish ? "İptal" : "Cancel";
    }

    private sealed class LanguageChoice
    {
        public LanguageChoice(PanoLanguage language) => Language = language;
        public PanoLanguage Language { get; }
        public override string ToString() => PanoLocalization.DisplayName(Language);
    }

    public static PanoLanguage LoadSavedLanguage(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return PanoLanguage.Auto;
            var data = JsonSerializer.Deserialize<LanguageSettings>(File.ReadAllText(filePath));
            return PanoLocalization.FromName(data?.Language, PanoLanguage.Auto);
        }
        catch
        {
            return PanoLanguage.Auto;
        }
    }

    public static void SaveLanguage(string filePath, PanoLanguage language)
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
