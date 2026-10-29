import UIKit
import AVFoundation

final class QrScanner: UIViewController, AVCaptureMetadataOutputObjectsDelegate {
 private let session = AVCaptureSession()
 private let worker = DispatchQueue(label:"Lume.camera")
 private let lock = NSLock()
 private var stopped = false, last = ""
 private let progress = UILabel()
 private var preview: AVCaptureVideoPreviewLayer!
 var onRead: (String)->Void = {_ in}
 var onCancel: ()->Void = {}
 var onError: (String)->Void = {_ in}
 var labels = ["","",""]
 private func isStopped() -> Bool { lock.lock(); defer { lock.unlock() }; return stopped }
 override func viewDidLoad() {
  super.viewDidLoad(); view.backgroundColor = .black
  DispatchQueue.main.asyncAfter(deadline:.now()+600) { [weak self] in guard let self = self, !self.isStopped() else { return }; self.finish(notify:true) }
  let title = UILabel(); title.text = labels[0]; title.textColor = .white; title.font = .boldSystemFont(ofSize:22)
  progress.text = labels[1]; progress.textColor = .white; progress.numberOfLines = 0
  let button = UIButton(type:.system); button.setTitle(labels[2],for:.normal); button.addTarget(self,action:#selector(cancel),for:.touchUpInside)
  let panel = UIStackView(arrangedSubviews:[title,progress]); panel.axis = .vertical; panel.spacing = 12; panel.translatesAutoresizingMaskIntoConstraints = false; view.addSubview(panel); button.translatesAutoresizingMaskIntoConstraints = false; view.addSubview(button)
  preview = AVCaptureVideoPreviewLayer(session:session); preview.videoGravity = .resizeAspectFill; view.layer.insertSublayer(preview,at:0)
  NSLayoutConstraint.activate([panel.topAnchor.constraint(equalTo:view.safeAreaLayoutGuide.topAnchor,constant:20),panel.leadingAnchor.constraint(equalTo:view.leadingAnchor,constant:20),panel.trailingAnchor.constraint(equalTo:view.trailingAnchor,constant:-20),button.bottomAnchor.constraint(equalTo:view.safeAreaLayoutGuide.bottomAnchor,constant:-12),button.centerXAnchor.constraint(equalTo:view.centerXAnchor),button.heightAnchor.constraint(equalToConstant:48)])
  panel.backgroundColor = UIColor.black.withAlphaComponent(0.7); button.backgroundColor = .black
  worker.async { [weak self] in guard let self = self, !self.isStopped() else { return }; do {
   guard let device = AVCaptureDevice.default(.builtInWideAngleCamera,for:.video,position:.back) else { throw LumeFailure("Câmera traseira indisponível.") }
   let input = try AVCaptureDeviceInput(device:device); let output = AVCaptureMetadataOutput(); self.session.beginConfiguration()
   guard self.session.canAddInput(input), self.session.canAddOutput(output) else { self.session.commitConfiguration(); throw LumeFailure("Não foi possível abrir a câmera. Confira a permissão e tente novamente.") }
   self.session.addInput(input); self.session.addOutput(output); output.setMetadataObjectsDelegate(self,queue:.main)
   guard output.availableMetadataObjectTypes.contains(.qr) else { self.session.commitConfiguration(); throw LumeFailure("Não foi possível ler o QR. Tente novamente.") }
   output.metadataObjectTypes = [.qr]; self.session.commitConfiguration(); if !self.isStopped() { self.session.startRunning() }
  } catch { DispatchQueue.main.async { self.onError(error.localizedDescription); self.finish(notify:true) } } }
 }
 override func viewDidLayoutSubviews() { super.viewDidLayoutSubviews(); preview.frame = view.bounds; if let connection = preview.connection, connection.isVideoOrientationSupported, let orientation = view.window?.windowScene?.interfaceOrientation { switch orientation { case .landscapeLeft: connection.videoOrientation = .landscapeLeft; case .landscapeRight: connection.videoOrientation = .landscapeRight; case .portraitUpsideDown: connection.videoOrientation = .portraitUpsideDown; default: connection.videoOrientation = .portrait } } }
 func update(_ text: String) { progress.text = text }
 @objc private func cancel() { finish(notify:true) }
 func finish(notify: Bool) { lock.lock(); if stopped { lock.unlock(); return }; stopped = true; lock.unlock(); worker.async { self.session.stopRunning() }; dismiss(animated:true) { if notify { self.onCancel() } } }
 override func viewDidDisappear(_ animated: Bool) { super.viewDidDisappear(animated); if !isStopped() { finish(notify:true) } }
 func metadataOutput(_ output: AVCaptureMetadataOutput,didOutput objects: [AVMetadataObject],from connection: AVCaptureConnection) {
  guard !isStopped() else { return }; for object in objects { if let code = object as? AVMetadataMachineReadableCodeObject, let text = code.stringValue, text.hasPrefix("LUMEQR1:"), text.utf8.count <= 600, text != last { last = text; onRead(text); break } }
 }
}
