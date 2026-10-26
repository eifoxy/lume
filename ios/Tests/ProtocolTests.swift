import XCTest
@testable import LumeCore

final class ProtocolTests: XCTestCase {
 let id = "AABBCCDDEEFF"
 func reply(_ status: UInt8, id: String = "AABBCCDDEEFF") throws -> [UInt8] { var b = [UInt8](repeating:0,count:64); b[0] = 0x88; b[4] = status; b.replaceSubrange(49..<61,with:try ControllerProtocol.identity(id)); return b }
 func testDiscoveryAndIdentity() throws {
  let query = ControllerProtocol.discovery(); XCTAssertEqual(Array(query.prefix(9)),[0x55,0xaa,0x17,0,0,3,0xdd,0xee,0xff]); XCTAssertEqual(query[61],0xe3); XCTAssertNil(ControllerProtocol.discovered(query))
  var found = try reply(0); found[2] = 0x17; found.replaceSubrange(4..<16,with:Array(id.utf8)); XCTAssertEqual(ControllerProtocol.discovered(found)?.0,id); XCTAssertEqual(ControllerProtocol.discovered(found)?.1,"smart")
  found[1] = 1; XCTAssertNil(ControllerProtocol.discovered(found)); XCTAssertNil(ControllerProtocol.discovered([0x88]))
  XCTAssertThrowsError(try ControllerProtocol.identity("000000000000")); XCTAssertThrowsError(try ControllerProtocol.identity("raw:"+String(repeating:"FF",count:12))); XCTAssertThrowsError(try ControllerProtocol.identity("a command"))
  let raw = "raw:0102030405060708090A0B0C"; XCTAssertEqual(try ControllerProtocol.identity(raw),Array(1...12).map(UInt8.init))
 }
 func testProfileWireLayout() throws {
  let header = try ControllerProtocol.frame(ControllerProtocol.header(slot:2,alias:"Recife",smart:true),id)
  XCTAssertEqual(Array(header[2..<13]),[1,1,0,6,2]+Array("Recife".utf8)); XCTAssertEqual(Array(header[49..<61]),Array(id.utf8)); XCTAssertEqual(Int(header[61]),header[0..<49].reduce(0){$0+Int($1)} & 255); XCTAssertEqual(Array(header[62...63]),[0,0])
  let point = try ControllerProtocol.frame(ControllerProtocol.point(47,[0,1,50,99,100]),id); XCTAssertEqual(Array(point[2..<14]),[1,2,0,8,47,23,30,0,1,50,99,100])
  let legacy = try ControllerProtocol.header(slot:0,alias:"Day",smart:false); XCTAssertEqual(legacy.count,14); XCTAssertEqual(Array(legacy.suffix(2)),[0,48]); XCTAssertEqual(legacy[3],10)
  XCTAssertThrowsError(try ControllerProtocol.point(48,[0,0,0,0,0])); XCTAssertThrowsError(try ControllerProtocol.header(slot:16,alias:"Day",smart:true)); XCTAssertThrowsError(try ControllerProtocol.point(0,[0,0,0,0,101])); XCTAssertThrowsError(try ControllerProtocol.frame([],id))
  XCTAssertEqual(ControllerProtocol.aliases(["Sol","Sol","SOL","Água azul"]),["Sol","Sol2","SOL3","Aguaazu"])
 }
 func testAckIdentityAndStatus() throws {
  XCTAssertTrue(ControllerProtocol.accepts(try reply(0x81),command:1,status:0x81,id:id)); XCTAssertFalse(ControllerProtocol.accepts(try reply(0x80),command:1,status:0x81,id:id))
  XCTAssertFalse(ControllerProtocol.accepts(try reply(0x81,id:"112233445566"),command:1,status:0x81,id:id)); XCTAssertTrue(ControllerProtocol.accepts([0x55,0xaa,3,0,0x83],command:3,status:0x83,id:id)); XCTAssertFalse(ControllerProtocol.accepts([0x55,0xaa,3,0,0x83],command:3,status:0x83,id:id,smartOnly:true))
 }
 func testPreviewAndProvision() throws {
  XCTAssertEqual(try ControllerProtocol.previewStart("Reef"),[6,1,0,4]+Array("Reef".utf8)); XCTAssertEqual(try ControllerProtocol.preview([100,90,80,70,0]),[6,2,0,5,100,90,80,70,0]); XCTAssertThrowsError(try ControllerProtocol.preview([101,0,0,0,0]))
  let frames = try ControllerProtocol.provision(id:id,ssid:"Home",password:"password",wpa2:true,aes:true); XCTAssertEqual(Array(frames[0][5..<11]),[4,1]+Array("Home".utf8)); XCTAssertEqual(frames[0][39],2); XCTAssertEqual(frames[0][40],1); XCTAssertEqual(frames[1][6],2)
  for frame in frames { XCTAssertEqual(Int(frame[61]),frame[0..<49].reduce(0){$0+Int($1)} & 255) }
  XCTAssertThrowsError(try ControllerProtocol.provision(id:id,ssid:"Réseau",password:"password",wpa2:true,aes:true)); XCTAssertThrowsError(try ControllerProtocol.provision(id:id,ssid:"Home",password:"short",wpa2:true,aes:true))
  XCTAssertEqual(Array(ControllerProtocol.clock().prefix(4)),[2,0,0,7])
 }
 func testLibraryAndLMFValidation() throws {
  let profile = LightingProfile(Name:"Reef",Channels:(1...5).map{"CH\($0)"},Values:Array(repeating:[0,20,40,80,100],count:48)); let data = try ProfileLibrary(Profiles:[profile],Selected:99).encoded(); let decoded = try ProfileLibrary.decode(data); XCTAssertEqual(decoded.Selected,0); XCTAssertEqual(decoded.Profiles[0].Values[47],[0,20,40,80,100])
  var bad = profile; bad.Values[2][4] = 101; XCTAssertThrowsError(try ProfileLibrary.decode(ProfileLibrary(Profiles:[bad]).encoded())); XCTAssertThrowsError(try ProfileLibrary.decode(Data(repeating:0,count:2_000_001)))
  let lmf = [[UInt8]](repeating:[0x46,0,0,0],count:240).flatMap{$0}; XCTAssertEqual(try ProfileLibrary.importFile(Data(lmf)).Profiles[0].Values[47],[100,100,100,100,100]); var invalid = lmf; invalid[1] = 1; XCTAssertThrowsError(try ProfileLibrary.importFile(Data(invalid)))
  let withoutDefaults = try JSONSerialization.data(withJSONObject:["Profiles":[["Name":"Reef","Channels":profile.Channels,"Values":profile.Values]]]); XCTAssertEqual(try ProfileLibrary.decode(withoutDefaults).Selected,0)
 }
 func testBoundedWiFiUnicastTargets() throws {
  let phone = IPv4Route(address:IPv4Route.parse("192.168.4.2")!,prefix:24), targets = try phone.targets(); XCTAssertEqual(IPv4Route.text(targets[0]),"192.168.4.1"); XCTAssertEqual(targets.count,253); XCTAssertEqual(Set(targets).count,targets.count)
  for ip in ["192.168.4.0","192.168.4.255","255.255.255.255","192.168.4.2","10.0.0.1","127.0.0.1"] { XCTAssertFalse(phone.peer(IPv4Route.parse(ip)!)); XCTAssertThrowsError(try phone.targets(direct:ip)) }
  let wide = IPv4Route(address:IPv4Route.parse("10.2.3.4")!,prefix:16); XCTAssertLessThanOrEqual(try wide.targets().count,256); XCTAssertEqual(try wide.targets(direct:"10.2.9.8").count,1)
  XCTAssertNil(IPv4Route.parse("1.2.3.256")); XCTAssertNil(IPv4Route.parse("1.2.3")); XCTAssertEqual(try phone.targets(cached:"192.168.4.19").first,IPv4Route.parse("192.168.4.19"))
 }
 func testRealUDPAckFilteringAndTimeout() throws {
  let ip = IPv4Route.parse("127.0.0.1")!, server = try UDPSocket(address:ip,port:0), client = try UDPSocket(address:ip,port:0); defer { server.close(); client.close() }
  let done = expectation(description:"controller replied"); let ack = try reply(0x85), wrong = try reply(0x85,id:"112233445566")
  DispatchQueue.global().async { defer { done.fulfill() }; do { let until = Date().addingTimeInterval(2); while Date()<until { if let packet = try server.receive() { try server.send(wrong,to:ip,port:packet.port); try server.send(ack,to:ip,port:packet.port); return } } } catch { XCTFail(error.localizedDescription) } }
  try Exchange.run(socket:client,target:ip,port:server.localPort,payload:ControllerProtocol.clock(),id:id,accepted:[0x85],timeout:1); wait(for:[done],timeout:3)
  XCTAssertThrowsError(try Exchange.run(socket:client,target:ip,port:server.localPort,payload:[3,8],id:id,accepted:[0x83],timeout:0.08)) { error in XCTAssertFalse((error as? AckTimeout)?.validReply ?? true) }
 }
}
