using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Markup;
namespace Lume;

public sealed class I18n : INotifyPropertyChanged
{
 public static I18n Current {get;}=new();
 readonly Dictionary<string,Dictionary<string,string>> catalog=new(StringComparer.Ordinal);
 readonly Regex pattern;
 public string Preference {get;private set;}="auto";
 public string Language {get;private set;}="pt-BR";
 public event PropertyChangedEventHandler? PropertyChanged;
 static string SettingsFile=>Path.Combine(Store.Root,"language.json");
 I18n(){using var stream=typeof(I18n).Assembly.GetManifestResourceStream("Lume.Strings.json")!;using var doc=JsonDocument.Parse(stream);foreach(var row in doc.RootElement.EnumerateArray())catalog.Add(row.GetProperty("pt").GetString()!,new(){["en"]=row.GetProperty("en").GetString()!,["es"]=row.GetProperty("es").GetString()!});pattern=new Regex("(?<![\\p{L}\\p{N}_])(?:"+string.Join("|",catalog.Keys.OrderByDescending(k=>k.Length).Select(Regex.Escape))+")(?![\\p{L}\\p{N}_])",RegexOptions.CultureInvariant);try{if(File.Exists(SettingsFile))Preference=JsonSerializer.Deserialize<string>(File.ReadAllText(SettingsFile))??"auto";}catch{Preference="auto";}if(Preference is not ("auto" or "pt-BR" or "en" or "es"))Preference="auto";Language=Resolve(Preference,CultureInfo.CurrentUICulture.Name);}
 public static string Resolve(string choice,string system){var value=choice=="auto"?system:choice;value=value.ToLowerInvariant();return value.StartsWith("pt")?"pt-BR":value.StartsWith("es")?"es":"en";}
 public static string T(string text)=>Current.Translate(text);
 public string Translate(string text)=>Language=="pt-BR"?text:pattern.Replace(text,m=>catalog[m.Value][Language]);
 public string Relocalize(string text,string previous){if(previous=="pt-BR")return Translate(text);var reverse=catalog.GroupBy(r=>r.Value[previous],StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.First().Key,StringComparer.Ordinal);var expression="(?<![\\p{L}\\p{N}_])(?:"+string.Join("|",reverse.Keys.OrderByDescending(k=>k.Length).Select(Regex.Escape))+")(?![\\p{L}\\p{N}_])";return Translate(Regex.Replace(text,expression,m=>reverse[m.Value],RegexOptions.CultureInvariant));}
 public void Set(string choice,bool persist=true){if(choice is not ("auto" or "pt-BR" or "en" or "es"))throw new ArgumentException("Invalid language");if(persist){Directory.CreateDirectory(Store.Root);var temp=SettingsFile+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(choice));File.Move(temp,SettingsFile,true);}Preference=choice;Language=Resolve(choice,CultureInfo.CurrentUICulture.Name);PropertyChanged?.Invoke(this,new(nameof(Language)));}
}
public sealed class Tr : MarkupExtension
{
 public string Key {get;set;}="";
 public override object ProvideValue(IServiceProvider provider)=>new Binding(nameof(I18n.Language)){Source=I18n.Current,Converter=new Translator(),ConverterParameter=Key}.ProvideValue(provider);
 sealed class Translator : IValueConverter {public object Convert(object value,Type type,object parameter,CultureInfo culture)=>I18n.T((string)parameter);public object ConvertBack(object value,Type type,object parameter,CultureInfo culture)=>Binding.DoNothing;}
}
