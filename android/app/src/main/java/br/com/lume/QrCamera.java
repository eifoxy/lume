package br.com.lume;

import android.app.Activity;
import android.graphics.Color;
import android.hardware.Camera;
import android.os.SystemClock;
import android.view.*;
import android.widget.*;
import com.google.zxing.*;
import com.google.zxing.common.HybridBinarizer;
import java.util.*;
import java.util.concurrent.*;
import java.util.function.Consumer;

/** Camera frames stay on the device. Only decoded Lume QR text reaches the editor. */
@SuppressWarnings("deprecation")
final class QrCamera implements SurfaceHolder.Callback {
 private final Activity activity;private final FrameLayout root,overlay,viewport;private final SurfaceView view;private final TextView progress;
 private final Consumer<String> read,error;private final Runnable cancel;private final ExecutorService decoder=Executors.newSingleThreadExecutor();
 private volatile Camera camera;private volatile boolean closed;private int width,height;private long last;private String previous="";
 QrCamera(Activity activity,FrameLayout root,String title,String instruction,String cancelText,Consumer<String> read,Consumer<String> error,Runnable cancel){
  this.activity=activity;this.root=root;this.read=read;this.error=error;this.cancel=cancel;
  overlay=new FrameLayout(activity);overlay.setBackgroundColor(Color.BLACK);LinearLayout column=new LinearLayout(activity);column.setOrientation(LinearLayout.VERTICAL);overlay.addView(column,new FrameLayout.LayoutParams(-1,-1));
  TextView heading=new TextView(activity);heading.setText(title);heading.setTextSize(22);heading.setTextColor(Color.WHITE);heading.setPadding(24,24,24,12);column.addView(heading);
  progress=new TextView(activity);progress.setText(instruction);progress.setTextSize(16);progress.setTextColor(Color.WHITE);progress.setPadding(24,8,24,16);column.addView(progress);
  viewport=new FrameLayout(activity);viewport.setClipChildren(true);column.addView(viewport,new LinearLayout.LayoutParams(-1,0,1));view=new SurfaceView(activity);viewport.addView(view,new FrameLayout.LayoutParams(-1,-1));view.getHolder().addCallback(this);
  Button button=new Button(activity);button.setText(cancelText);button.setOnClickListener(v->cancel.run());column.addView(button,new LinearLayout.LayoutParams(-1,-2));root.addView(overlay,new FrameLayout.LayoutParams(-1,-1));
 }
 void progress(String text){if(!closed)progress.setText(text);}
 @Override public void surfaceCreated(SurfaceHolder holder){
  if(closed)return;
  try{
   int id=-1;Camera.CameraInfo info=new Camera.CameraInfo();for(int i=0;i<Camera.getNumberOfCameras();i++){Camera.getCameraInfo(i,info);if(info.facing==Camera.CameraInfo.CAMERA_FACING_BACK){id=i;break;}}
   if(id<0)throw new Exception("Câmera traseira indisponível.");Camera.getCameraInfo(id,info);Camera opened=Camera.open(id);camera=opened;
   Camera.Parameters parameters=opened.getParameters();Camera.Size size=Collections.min(parameters.getSupportedPreviewSizes(),Comparator.comparingLong(s->Math.abs((long)s.width*s.height-640*480)));width=size.width;height=size.height;parameters.setPreviewSize(width,height);parameters.setPreviewFormat(android.graphics.ImageFormat.NV21);
   List<String> modes=parameters.getSupportedFocusModes();if(modes!=null&&modes.contains(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE))parameters.setFocusMode(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE);opened.setParameters(parameters);
   int rotation=activity.getWindowManager().getDefaultDisplay().getRotation()*90,angle=(info.orientation-rotation+360)%360;opened.setDisplayOrientation(angle);opened.setPreviewDisplay(holder);
   final int w=angle%180==0?width:height,h=angle%180==0?height:width;viewport.post(()->{if(closed)return;float scale=Math.max(viewport.getWidth()/(float)w,viewport.getHeight()/(float)h);view.setLayoutParams(new FrameLayout.LayoutParams(Math.round(w*scale),Math.round(h*scale),Gravity.CENTER));});
   opened.setPreviewCallbackWithBuffer((bytes,c)->{
    if(closed)return;if(SystemClock.elapsedRealtime()-last<150){c.addCallbackBuffer(bytes);return;}last=SystemClock.elapsedRealtime();
    try{decoder.execute(()->{try{
     Map<DecodeHintType,Object> hints=new EnumMap<>(DecodeHintType.class);hints.put(DecodeHintType.POSSIBLE_FORMATS,Collections.singletonList(BarcodeFormat.QR_CODE));hints.put(DecodeHintType.TRY_HARDER,Boolean.TRUE);
     MultiFormatReader reader=new MultiFormatReader();reader.setHints(hints);Result result=reader.decodeWithState(new BinaryBitmap(new HybridBinarizer(new PlanarYUVLuminanceSource(bytes,width,height,0,0,width,height,false))));String value=result.getText();
     if(value.startsWith("LUMEQR1:")&&!value.equals(previous)){previous=value;activity.runOnUiThread(()->{if(!closed)read.accept(value);});}
    }catch(ReaderException ignored){}catch(Exception e){activity.runOnUiThread(()->{if(!closed)error.accept("Não foi possível ler o QR. Tente novamente.");});}finally{if(!closed&&camera==c)try{c.addCallbackBuffer(bytes);}catch(RuntimeException ignored){}}});}catch(RejectedExecutionException ignored){}
   });opened.addCallbackBuffer(new byte[width*height*3/2]);opened.startPreview();
  }catch(Exception e){error.accept("Não foi possível abrir a câmera. Confira a permissão e tente novamente.");cancel.run();}
 }
 @Override public void surfaceChanged(SurfaceHolder holder,int format,int w,int h){}
 @Override public void surfaceDestroyed(SurfaceHolder holder){release();}
 private void release(){Camera c=camera;camera=null;if(c!=null){try{c.setPreviewCallbackWithBuffer(null);c.stopPreview();}catch(RuntimeException ignored){}c.release();}}
 void close(){if(closed)return;closed=true;release();decoder.shutdownNow();root.removeView(overlay);}
}
