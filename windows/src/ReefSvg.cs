using System.Globalization;
using System.Text;
namespace Lume;

// Standalone SVG demonstration. Fish outlines are shared with the native renderer.
static class ReefSvg
{
 static string N(double number)=>number.ToString("0.###",CultureInfo.InvariantCulture);
 public static string Export()
 {
  var s=new StringBuilder("""
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 320" role="img" aria-labelledby="title desc">
<title id="title">Recife animado do Lume</title><desc id="desc">Demonstração vetorial: corais abrem durante a fase clara de um ciclo de 36 segundos e peixes-palhaço nadam pelo aquário. No aplicativo, a abertura acompanha os valores de luz da programação.</desc>
<defs>
 <linearGradient id="water" x2="0" y2="1"><stop stop-color="#203849"/><stop offset="1" stop-color="#0a151f"/></linearGradient>
 <linearGradient id="body" x2="0" y2="1"><stop stop-color="#ffb451"/><stop offset="1" stop-color="#f07538"/></linearGradient>
 <linearGradient id="beam" x2="0" y2="1"><stop stop-color="#a9cdf5" stop-opacity=".3"/><stop offset="1" stop-color="#a9cdf5" stop-opacity="0"/></linearGradient>
 <clipPath id="tank"><rect x="7" y="48" width="986" height="260" rx="12"/></clipPath>
</defs>
<rect x="7" y="48" width="986" height="260" rx="12" fill="url(#water)" stroke="#34515b"/>
<g clip-path="url(#tank)">
 <g opacity=".7"><animate attributeName="opacity" values=".03;.03;.85;.85;.03" keyTimes="0;.2;.4;.7;1" dur="36s" repeatCount="indefinite"/>
  <path d="M91 35H109L245 302H-45Z M291 35H309L445 302H155Z M491 35H509L645 302H355Z M691 35H709L845 302H555Z M891 35H909L1045 302H755Z" fill="url(#beam)"/>
 </g>
 <path d="M0 286Q90 241 170 263Q240 224 310 266Q390 236 460 266Q550 220 625 259Q720 218 800 266Q885 240 1000 279V320H0Z" fill="#19353f"/>
 <path d="M0 294Q170 284 340 293Q550 284 740 292Q900 284 1000 293V320H0Z" fill="#304149"/>
"""
  );
  foreach(var (x,color,tip,scale) in new[]{(150,"#ef83ac","#f4bfcd",1.05),(377,"#ab91e7","#e3cdff",.73),(683,"#78d6bb","#c3f8df",.83),(865,"#c68adb","#f1beed",.99)}){
   s.Append($"<ellipse cx='{x}' cy='290' rx='54' ry='14' fill='#48535d'/><g transform='translate({x} 283) scale({N(scale)})'>");
   for(int k=0;k<7;k++){
    int tx=(k-3)*19,length=40+k*11%22;
    s.Append($"<g><animateTransform attributeName='transform' type='scale' values='.23 .26;.23 .26;1 1;1 1;.23 .26' keyTimes='0;.2;.4;.7;1' dur='36s' repeatCount='indefinite'/><path d='M{(k-3)*5} 4Q{N(tx*.4)} -12 {tx} {-length}' fill='none' stroke='{color}' stroke-width='4.8' stroke-linecap='round'/><g transform='translate({tx} {-length})'><g><animateTransform attributeName='transform' type='rotate' values='-4;4;-4' dur='{N(3+k*.2)}s' repeatCount='indefinite'/>");
    for(int petal=0;petal<8;petal++){double a=petal*Math.PI/4,ex=Math.Cos(a)*12,ey=Math.Sin(a)*9;s.Append($"<path d='M0 0L{N(ex)} {N(ey)}' stroke='{color}' stroke-width='2.6' stroke-linecap='round'/><circle cx='{N(ex)}' cy='{N(ey)}' r='1.8' fill='{tip}'/>");}
    s.Append($"<circle r='2.5' fill='{tip}'/></g></g></g>");
   }s.Append("</g>");
  }
  for(int i=0;i<3;i++){
   int y=130+i*28;string dur=N(30+i*7),scale=N(1-i*.22);
   s.Append($"<g><animateTransform attributeName='transform' type='translate' values='160 {y};820 {y+6};160 {y}' dur='{dur}s' begin='-{i*8}s' repeatCount='indefinite'/><g transform='scale({scale})'><g><animateTransform attributeName='transform' type='scale' values='1 1;-1 1;1 1' keyTimes='0;.5;1' calcMode='discrete' dur='{dur}s' begin='-{i*8}s' repeatCount='indefinite'/><g stroke='#2b2831' stroke-width='2.5' stroke-linejoin='round'>");
   s.Append($"<path d='{LedSimulation.FishTail}' fill='#f09948'><animateTransform attributeName='transform' type='scale' values='.85 1;1 1;.85 1' dur='.5s' repeatCount='indefinite'/></path><path d='M-14 -19Q0 -35 20 -16Z M-10 17Q0 31 13 18' fill='#e99a43'/><path d='{LedSimulation.FishBody}' fill='url(#body)'/><path d='{LedSimulation.FishBands}' fill='#fff4d9'/><path d='M6 3Q-3 17 -10 10Q-5 3 6 3' fill='#edab59' stroke='#865131' stroke-width='1'/><circle cx='27' cy='-4' r='4.4' fill='#fff3d5' stroke-width='1'/><ellipse cx='28' cy='-4' rx='2.6' ry='2.9' fill='#202831' stroke='none'/><circle cx='28.8' cy='-5.2' r='.9' fill='white' stroke='none'/></g></g></g></g>");
  }
  s.Append("""
</g><rect x="15" y="17" width="970" height="18" rx="6" fill="#1c2d34" stroke="#3b545b"/>
<g fill="#a8d8fa"><animate attributeName="opacity" values=".15;.15;1;1;.15" keyTimes="0;.2;.4;.7;1" dur="36s" repeatCount="indefinite"/>
<rect x="82" y="23" width="36" height="6" rx="3"/><rect x="282" y="23" width="36" height="6" rx="3"/><rect x="482" y="23" width="36" height="6" rx="3"/><rect x="682" y="23" width="36" height="6" rx="3"/><rect x="882" y="23" width="36" height="6" rx="3"/></g></svg>
"""
  );return s.ToString();
 }
}
