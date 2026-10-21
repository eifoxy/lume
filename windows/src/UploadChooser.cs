using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace Lume;

public sealed class UploadChooser : UserControl
{
 public UploadPlan? Plan {get;private set;}
 public event EventHandler? Confirmed;
 public event EventHandler? Cancelled;
 readonly IReadOnlyList<Profile> profiles;readonly int selected;
 readonly RadioButton single=new(){Content=I18n.T("Somente o perfil selecionado"),IsChecked=true,GroupName="uploadScope"};
 readonly RadioButton all=new(){Content=I18n.T("Todos os perfis \u00B7 substituir a mem\u00F3ria"),GroupName="uploadScope"};
 readonly ComboBox slot=new(){ItemsSource=Enumerable.Range(1,16).Select(i=>I18n.T("Perfil ")+i).ToList(),SelectedIndex=0};
 readonly TextBlock list=new(){TextWrapping=TextWrapping.Wrap,FontSize=12,LineHeight=21};
 readonly TextBlock warning=new(){TextWrapping=TextWrapping.Wrap,Foreground=new SolidColorBrush(Color.FromRgb(238,191,143)),FontSize=12,Margin=new Thickness(0,12,0,0)};
 readonly TextBlock summary=new(){TextWrapping=TextWrapping.Wrap,FontSize=12,Margin=new Thickness(0,12,0,8)};
 readonly CheckBox consent=new(){Content=I18n.T("Entendi: apagar os perfis salvos na controladora"),Margin=new Thickness(0,16,0,0),Foreground=Brushes.White};
 readonly Button send=new(){Content=I18n.T("Gravar perfil"),Background=new SolidColorBrush(Color.FromRgb(119,223,191)),Foreground=new SolidColorBrush(Color.FromRgb(16,40,32)),Margin=new Thickness(0,18,0,0)};
 public UploadChooser(IReadOnlyList<Profile> profiles,int selected,string device)
 {
  this.profiles=profiles;this.selected=selected;
  var s=new StackPanel{Margin=new Thickness(24)};
  s.Children.Add(new TextBlock{Text=I18n.T("O que voc\u00EA quer enviar?"),FontSize=22,FontWeight=FontWeights.SemiBold});
  s.Children.Add(new TextBlock{Text=device,FontSize=12,Margin=new Thickness(0,7,0,18),Foreground=new SolidColorBrush(Color.FromRgb(163,185,177))});
  foreach(var r in new[]{single,all}){r.Margin=new Thickness(0,0,0,12);r.Foreground=Brushes.White;r.VerticalContentAlignment=VerticalAlignment.Center;s.Children.Add(r);}
  s.Children.Add(slot);s.Children.Add(summary);
  s.Children.Add(new Border{Background=new SolidColorBrush(Color.FromRgb(16,29,33)),CornerRadius=new CornerRadius(8),Padding=new Thickness(12),Child=new ScrollViewer{Content=list,MaxHeight=145,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});
  s.Children.Add(warning);
  s.Children.Add(new TextBlock{Text=I18n.T("Um backup da biblioteca local ser\u00E1 criado antes do primeiro comando. Ele n\u00E3o recupera perfis que s\u00F3 estejam no aparelho. Feche o Pled antes de enviar. Integra\u00E7\u00E3o ainda em valida\u00E7\u00E3o f\u00EDsica."),TextWrapping=TextWrapping.Wrap,Foreground=new SolidColorBrush(Color.FromRgb(163,185,177)),FontSize=11,Margin=new Thickness(0,12,0,0)});
  s.Children.Add(consent);s.Children.Add(send);var cancel=new Button{Content=I18n.T("Cancelar"),Margin=new Thickness(0,8,0,0),IsCancel=true};cancel.Click+=(_,_)=>Cancelled?.Invoke(this,EventArgs.Empty);s.Children.Add(cancel);
  Content=new ScrollViewer{Content=s,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
  single.Checked+=(_,_)=>Refresh();all.Checked+=(_,_)=>{consent.IsChecked=false;Refresh();};slot.SelectionChanged+=(_,_)=>Refresh();consent.Checked+=(_,_)=>Refresh();consent.Unchecked+=(_,_)=>Refresh();
  send.Click+=(_,_)=>{Refresh();if(Plan is not null&&send.IsEnabled)Confirmed?.Invoke(this,EventArgs.Empty);};Refresh();
 }
 void Refresh()
 {
  bool replace=all.IsChecked==true;slot.Visibility=replace?Visibility.Collapsed:Visibility.Visible;consent.Visibility=replace?Visibility.Visible:Visibility.Collapsed;
  try{
   Plan=UploadPlan.Create(replace?profiles:new[]{profiles[selected]},replace,replace?0:slot.SelectedIndex);
   summary.Text=replace?$"{Plan.Items.Count}{I18n.T(" perfis \u00B7 ")}{Plan.Items.Count*48}{I18n.T(" hor\u00E1rios \u00B7 ordem da lista lateral")}":I18n.T("O perfil de destino ser\u00E1 substitu\u00EDdo; os outros perfis ser\u00E3o mantidos.");
   list.Text=string.Join("\n",Plan.Items.Select(i=>$"{i.Slot+1:00}  ·  {i.LocalName}  →  {i.DeviceName}"));
   warning.Text=replace?I18n.T("Todos os perfis existentes na controladora ser\u00E3o apagados antes do envio. Se o cabo desconectar ou houver erro, a mem\u00F3ria pode ficar vazia ou incompleta. Seus arquivos locais ser\u00E3o preservados."):I18n.T("O conte\u00FAdo do n\u00FAmero escolhido ser\u00E1 substitu\u00EDdo. As cinco sa\u00EDdas f\u00EDsicas CH1\u2013CH5 continuam as mesmas.");
   send.Content=replace?$"{I18n.T("Apagar antigos e enviar ")}{Plan.Items.Count}{I18n.T(" perfis")}":I18n.T("Gravar neste perfil");send.IsEnabled=!replace||consent.IsChecked==true;
  }catch(Exception ex)when(ex is InvalidDataException||ex is ArgumentOutOfRangeException){Plan=null;list.Text=I18n.T("Nada ser\u00E1 enviado.");summary.Text=I18n.T("Revise a biblioteca antes de continuar.");warning.Text=ex.Message;send.IsEnabled=false;}
 }
 internal void SelectAllForTest(bool acknowledged){all.IsChecked=true;consent.IsChecked=acknowledged;Refresh();}
 internal bool CanConfirm=>send.IsEnabled;
}
