using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace Lume;

// Vector illustration: no USB, browser, network, or raster assets.
public sealed class LedSimulation : FrameworkElement
{
 readonly DispatcherTimer animation=new(){Interval=TimeSpan.FromMilliseconds(33)};
 readonly Stopwatch clock=new();double previousSeconds,phase,opening;bool motionEnabled=true;
 double[] levels=new double[5];
 public double[] Levels {get=>levels.ToArray();set {if(value.Length!=5||value.Any(v=>!double.IsFinite(v)||v<0||v>100))throw new ArgumentException(I18n.T("A pr\u00E9via precisa de cinco intensidades de 0 a 100."));levels=value.ToArray();if(!IsLoaded||!motionEnabled)opening=TargetOpening;InvalidateVisual();}}
 public bool MotionEnabled {get=>motionEnabled;set {motionEnabled=value;if(!value){animation.Stop();opening=TargetOpening;}else if(IsLoaded&&IsVisible){clock.Restart();previousSeconds=0;animation.Start();}InvalidateVisual();}}
 public double CoralOpening=>opening;
 public static double OpeningFor(double brightness){double t=Math.Clamp((brightness-.04)/.86,0,1);return t*t*(3-2*t);}
 double TargetOpening=>OpeningFor(levels.Max()/100);
 static readonly Color[] Palette=[C("#66B8FF"),C("#E0E9DB"),C("#B29AFF"),C("#E8BF73"),C("#74D7B7")];
 public LedSimulation()
 {
  Loaded+=(_,_)=>{opening=TargetOpening;clock.Restart();previousSeconds=0;if(motionEnabled&&IsVisible)animation.Start();};
  Unloaded+=(_,_)=>{animation.Stop();clock.Stop();};
  IsVisibleChanged+=(_,_)=>{if(IsVisible&&IsLoaded&&motionEnabled){clock.Restart();previousSeconds=0;animation.Start();}else animation.Stop();};
  animation.Tick+=(_,_)=>{double now=clock.Elapsed.TotalSeconds,delta=Math.Clamp(now-previousSeconds,0,.12);previousSeconds=now;if(!InViewport())return;phase+=delta;opening+=(TargetOpening-opening)*(1-Math.Exp(-delta*4));InvalidateVisual();};
 }
 bool InViewport()
 {
  for(DependencyObject? p=VisualTreeHelper.GetParent(this);p is not null;p=VisualTreeHelper.GetParent(p))if(p is ScrollViewer view){try{return TransformToAncestor(view).TransformBounds(new Rect(RenderSize)).IntersectsWith(new Rect(view.RenderSize));}catch(InvalidOperationException){return false;}}
  return true;
 }
 internal void SeekForPreview(double time){phase=time;opening=TargetOpening;InvalidateVisual();}
 static Color C(string hex)=>(Color)ColorConverter.ConvertFromString(hex);
 static SolidColorBrush B(string hex)=>new(C(hex));
 static SolidColorBrush Solid(Color color)=>new(color);
 static Pen Pen(string hex,double width)=>new(B(hex),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};
 static StreamGeometry Shape(Action<StreamGeometryContext> action){var g=new StreamGeometry();using(var c=g.Open())action(c);g.Freeze();return g;}
 static void Path(DrawingContext d,string path,Brush? fill,Pen? stroke=null)=>d.DrawGeometry(fill,stroke,Geometry.Parse(path));
 protected override void OnRender(DrawingContext d)
 {
  base.OnRender(d);double w=ActualWidth,h=ActualHeight;if(w<20||h<20)return;
  d.PushTransform(new ScaleTransform(w/1000,h/320));
  double light=levels.Max()/100;
  var glass=new Rect(7,48,986,260);d.PushClip(new RectangleGeometry(glass,12,12));
  d.DrawRectangle(new LinearGradientBrush(C("#142A37"),C("#0A151F"),90),null,glass);
  var mixed=Color.FromRgb((byte)(levels.Select((v,i)=>Palette[i].R*v).Sum()/500),(byte)(levels.Select((v,i)=>Palette[i].G*v).Sum()/500),(byte)(levels.Select((v,i)=>Palette[i].B*v).Sum()/500));
  d.DrawRectangle(new LinearGradientBrush(Color.FromArgb(105,mixed.R,mixed.G,mixed.B),Color.FromArgb(0,mixed.R,mixed.G,mixed.B),90),null,glass);
  // Diffuse shafts, with very slow movement at the water surface.
  for(int c=0;c<5;c++){
   double x=100+c*200,intensity=levels[c]/100,sway=Math.Sin(phase*.35+c)*9;
   var beam=Shape(g=>{g.BeginFigure(new Point(x-9,35),true,true);g.LineTo(new Point(x+9,35),true,false);g.LineTo(new Point(x+145+sway,302),true,false);g.LineTo(new Point(x-145+sway,302),true,false);});
   d.DrawGeometry(new LinearGradientBrush(Color.FromArgb((byte)(90*intensity),Palette[c].R,Palette[c].G,Palette[c].B),Colors.Transparent,90),null,beam);
  }
  // Distant reef adds depth without competing with the animated foreground.
  d.PushOpacity(.35+light*.35);
  Path(d,"M0,286 Q90,241 170,263 Q240,224 310,266 Q390,236 460,266 Q550,220 625,259 Q720,218 800,266 Q885,240 1000,279 L1000,320 L0,320 Z",B("#17333C"));
  Branch(d,70,285,.6,"#28505C");Branch(d,800,284,.85,"#244F5B");Branch(d,925,290,.48,"#35606A");
  d.Pop();
  // Substrate and rocks anchor the polyps.
  Path(d,"M0,294 Q170,284 340,293 Q550,284 740,292 Q900,284 1000,293 L1000,320 L0,320 Z",new LinearGradientBrush(C("#3C4848"),C("#18282D"),90));
  Rock(d,143,288,110,29);Rock(d,390,293,88,23);Rock(d,650,291,92,24);Rock(d,865,291,110,28);
  d.PushOpacity(.27+.73*light);
  // Large soft coral colonies: the span and extension of every tentacle follow the light.
  Colony(d,150,283,1.05,"#EF83AC","#F4BFCD",0);
  Colony(d,377,287,.73,"#AB91E7","#E3CDFF",2);
  Branch(d,524,290,.77,"#D98977");
  Colony(d,683,287,.83,"#78D6BB","#C3F8DF",4);
  Colony(d,865,284,.99,"#C68ADB","#F1BEED",7);
  // Three clownfish, all drawn from the same SVG-compatible paths.
  Fish(d,phase,0,475,290,132,.93);
  Fish(d,phase,2.2,520,245,184,.66);
  Fish(d,phase,4.4,565,225,106,.48);
  d.Pop();
  // Small bubbles drift upward; the cycles are deterministic and never allocate a particle list.
  for(int i=0;i<13;i++){
   double y=300-((phase*(10+i%3*3)+i*23)%230),x=34+i*77+Math.Sin(phase*.7+i)*4;
   d.DrawEllipse(null,new Pen(new SolidColorBrush(Color.FromArgb((byte)(17+light*33),173,224,232)),.8),new Point(x,y),1.1+i%3*.6,1.1+i%3*.6);
  }
  d.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb((byte)(30+light*45),157,211,224)),1),new Point(17,56),new Point(983,56));
  d.Pop(); // tank clip
  d.DrawRoundedRectangle(null,Pen("#34515B",1),glass,12,12);
  d.DrawRoundedRectangle(B("#1C2D34"),Pen("#3B545B",1),new Rect(15,17,970,18),6,6);
  for(int c=0;c<5;c++){
   double x=100+c*200,intensity=levels[c]/100;var color=Palette[c];
   if(intensity>0)d.DrawEllipse(new RadialGradientBrush(Color.FromArgb((byte)(180*intensity),color.R,color.G,color.B),Colors.Transparent),null,new Point(x,26),37,22);
   var led=Color.FromRgb((byte)(26+(color.R-26)*intensity),(byte)(35+(color.G-35)*intensity),(byte)(40+(color.B-40)*intensity));
   d.DrawRoundedRectangle(Solid(led),Pen("#46616A",.6),new Rect(x-18,23,36,6),3,3);
  }
  d.Pop(); // scale
 }
 void Colony(DrawingContext d,double x,double y,double scale,string color,string tip,int seed)
 {
  d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new ScaleTransform(scale,scale));
  for(int k=0;k<7;k++){
   double spread=(k-3)*(9+opening*10),length=14+opening*(26+(k*11+seed*7)%22),sway=Math.Sin(phase*.9+k*.8+seed)*(1+opening*3);
   var stem=Shape(g=>{g.BeginFigure(new Point((k-3)*5,4),false,false);g.BezierTo(new Point(spread*.4+sway,-length*.25),new Point(spread+sway,-length*.7),new Point(spread+sway,-length),true,false);});
   d.DrawGeometry(null,new Pen(B(color),4.8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},stem);
   double tx=spread+sway,ty=-length;double radius=2+opening*10;
   for(int petal=0;petal<8;petal++){
    double a=petal*Math.PI/4+Math.Sin(phase*.7+k)*.07,ex=tx+Math.Cos(a)*radius,ey=ty+Math.Sin(a)*radius*.75;
    d.DrawLine(new Pen(B(color),2.6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},new Point(tx,ty),new Point(ex,ey));
    d.DrawEllipse(B(tip),null,new Point(ex,ey),1.3+opening*.45,1.3+opening*.45);
   }
   d.DrawEllipse(B(tip),null,new Point(tx,ty),2.5,2.1);
   d.DrawEllipse(B(color),null,new Point(tx,ty),1.1,1.0);
  }
  d.Pop();d.Pop();
 }
 static void Branch(DrawingContext d,double x,double y,double scale,string color)
 {
  d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new ScaleTransform(scale,scale));
  Path(d,"M0,2 C-5,-23 2,-46 -2,-76 M-1,-30 C-23,-32 -27,-44 -26,-58 M-25,-47 L-40,-56 M-2,-49 C15,-50 23,-62 20,-77 M19,-62 L34,-74 M-2,-65 L-14,-85 M-26,-54 L-19,-68",null,new Pen(B(color),5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round});
  d.Pop();d.Pop();
 }
 static void Rock(DrawingContext d,double x,double y,double width,double height)
 {
  d.DrawEllipse(new LinearGradientBrush(C("#4B515B"),C("#27363E"),90),null,new Point(x,y),width/2,height/2);
  d.DrawEllipse(B("#52606A"),null,new Point(x-width*.15,y-height*.2),width*.17,height*.18);
 }
 internal static readonly string FishBody="M-29,-2 C-29,-20 -8,-25 13,-20 C31,-16 39,-5 39,0 C36,10 26,19 9,20 C-10,24 -28,16 -29,3 Z";
 internal static readonly string FishTail="M-27,-5 C-38,-9 -46,-20 -49,-16 L-47,16 C-41,20 -34,8 -27,6 Z";
 internal static readonly string FishBands="M-20,-17 C-25,-6 -24,5 -16,18 L-8,21 C-17,5 -15,-8 -10,-22 Z M4,-22 C-2,-7 1,8 11,20 L20,16 C11,3 10,-9 14,-20 Z M29,-12 C23,-3 25,8 29,11 L33,7 C30,1 31,-3 34,-6 Z";
 void Fish(DrawingContext d,double time,double offset,double center,double range,double height,double scale)
 {
  double t=time*(.18+offset*.006)+offset,x=center+Math.Sin(t)*range,y=height+Math.Sin(time*.68+offset)*12;bool right=Math.Cos(t)>=0;
  d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new ScaleTransform(right?scale:-scale,scale));d.PushTransform(new RotateTransform(Math.Sin(time*1.6+offset)*3));
  double flicker=.82+Math.Sin(time*8+offset)*.15;
  d.PushTransform(new ScaleTransform(flicker,1,new Point(-28,0).X,0));Path(d,FishTail,new LinearGradientBrush(C("#F0AC56"),C("#E66A36"),90),Pen("#2B2831",2.5));d.Pop();
  Path(d,"M-14,-19 C-9,-33 11,-29 20,-16 L8,-14 Z",B("#E99A43"),Pen("#2B2831",2));
  Path(d,"M-10,17 Q0,31 13,18",B("#D97A3C"),Pen("#2B2831",2));
  Path(d,FishBody,new LinearGradientBrush(C("#FFB451"),C("#F07538"),90),Pen("#2B2831",2.7));
  d.PushClip(Geometry.Parse(FishBody));Path(d,FishBands,B("#FFF4D9"),Pen("#342D34",2.5));d.Pop();
  var fin=Shape(g=>{g.BeginFigure(new Point(6,3),true,true);g.QuadraticBezierTo(new Point(-3+Math.Sin(time*9)*2,17),new Point(-10,10),true,false);g.QuadraticBezierTo(new Point(-5,3),new Point(6,3),true,false);});d.DrawGeometry(B("#EDAB59"),Pen("#865131",1),fin);
  d.DrawEllipse(B("#FFF3D5"),Pen("#302C31",1),new Point(27,-4),4.4,4.4);d.DrawEllipse(B("#202831"),null,new Point(28,-4),2.6,2.9);d.DrawEllipse(Brushes.White,null,new Point(28.8,-5.2),.9,.9);
  Path(d,"M35,3 Q38,5 39,2",null,Pen("#6B3B33",1.2));d.Pop();d.Pop();d.Pop();
 }
}
