using System.Windows;
using System.Windows.Controls;
namespace Lume;
public partial class MainWindow
{
 bool languageReady;
 void InitializeLanguage(){languageReady=false;LanguageMenu.SelectedIndex=I18n.Current.Preference switch{"pt-BR"=>1,"en"=>2,"es"=>3,_=>0};languageReady=true;}
 void LanguageChanged(object sender,SelectionChangedEventArgs e){if(!languageReady)return;if(busy||!CommitTable()){InitializeLanguage();return;}try{var choice=(string)((ComboBoxItem)LanguageMenu.SelectedItem).Tag;var previous=I18n.Current.Language;var connection=Connection.Text;var status=Status.Text;I18n.Current.Set(choice);Render();Connection.Text=I18n.Current.Relocalize(connection,previous);Note(I18n.Current.Relocalize(status,previous));EnableUsb();}catch(Exception ex){App.Log(ex);InitializeLanguage();MessageBox.Show(this,I18n.T("Falha ao salvar: ")+ex.Message,"Lume");}}
}
