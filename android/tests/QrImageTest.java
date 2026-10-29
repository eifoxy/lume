package br.com.lume;
import com.google.zxing.*;
import com.google.zxing.common.HybridBinarizer;
import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.File;
/** Decode a real PNG produced by the Windows QR generator. No camera is used. */
public final class QrImageTest {
 public static void main(String[] args)throws Exception{
  BufferedImage image=ImageIO.read(new File(args[0]));int w=image.getWidth(),h=image.getHeight();int[] pixels=image.getRGB(0,0,w,h,null,0,w);
  Result result=new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(new RGBLuminanceSource(w,h,pixels))));
  if(!result.getText().startsWith("LUMEQR1:0123456789abcdef:1:"))throw new AssertionError("Wrong decoded Windows QR payload");
  System.out.println("Actual Windows QR PNG decoded successfully with the Android ZXing engine");
 }
}
