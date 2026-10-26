import XCTest
@testable import LumeCore

final class TransferTests: XCTestCase {
 func profile() -> LightingProfile { LightingProfile(Name:"Reef",Channels:(1...5).map{"CH\($0)"},Values:Array(repeating:[0,30,50,80,100],count:48)) }
 func testWholeBatchAndDelayedHeaderAck() throws {
  var codes = [UInt8](), headers = [[UInt8]](), points = 0, pauses = 0, maximum = 0
  try ProfileTransfer.run(profiles:[profile(),profile()],replace:true,slot:0,smart:true,exchange:{ payload,ack in
   codes.append(ack)
   if ack == 0x80 { XCTAssertGreaterThanOrEqual(pauses,14); headers.append(payload); if headers.count == 1 { throw AckTimeout(validReply:false,count:0) } }
   if ack == 0x81 { XCTAssertEqual(Int(payload[4]),points%48); points += 1 }
   if ack == 0x91 { XCTAssertEqual(points,96) }
  },check:{},pause:{ pauses += 1 },progress:{_,percent in maximum = max(maximum,percent) })
  XCTAssertEqual(codes.filter{$0 == 0x82}.count,1); XCTAssertEqual(headers[0],headers[1]); XCTAssertEqual(headers[2][4],1); XCTAssertEqual(points,96); XCTAssertEqual(codes.filter{$0 == 0x83}.count,2); XCTAssertEqual(codes.last,0x91); XCTAssertEqual(maximum,100)
 }
 func testSixthPointRetriesIdenticalPacketWithoutRestart() throws {
  var seen = [[UInt8]](), six = [[UInt8]]()
  try ProfileTransfer.run(profiles:[profile()],replace:false,slot:3,smart:true,exchange:{ p,ack in seen.append(p); if ack == 0x81 && p[4] == 5 { six.append(p); if six.count < 3 { throw AckTimeout(validReply:false,count:0) } } },check:{},pause:{},progress:{_,_ in})
  XCTAssertEqual(six.count,3); XCTAssertTrue(six.allSatisfy{$0 == six[0]}); XCTAssertEqual(seen.filter{$0[0] == 5}.count,0); XCTAssertEqual(seen.filter{$0[0] == 1 && $0[1] == 1}.count,1)
 }
 func testEraseAndFinalizersAreNeverRetried() {
  for failing: UInt8 in [0x82,0x83,0x91] { var attempts = 0, maxProgress = 0
   XCTAssertThrowsError(try ProfileTransfer.run(profiles:[profile()],replace:true,slot:0,smart:true,exchange:{_,ack in if ack == failing { attempts += 1; throw AckTimeout(validReply:false,count:0) } },check:{},pause:{},progress:{_,percent in maxProgress = percent })) { error in if failing == 0x82 { XCTAssertTrue(error.localizedDescription.contains("apagada")) } }
   XCTAssertEqual(attempts,1); XCTAssertLessThan(maxProgress,100)
  }
 }
 func testUnexpectedStatusAndCancellationDoNotRestart() {
  var headers = 0, clear = 0, pauses = 0
  XCTAssertThrowsError(try ProfileTransfer.run(profiles:[profile()],replace:false,slot:0,smart:true,exchange:{_,ack in if ack == 0x80 { headers += 1; throw AckTimeout(validReply:true,count:1) } },check:{},pause:{},progress:{_,_ in})); XCTAssertEqual(headers,1)
  headers = 0
  XCTAssertThrowsError(try ProfileTransfer.run(profiles:[profile()],replace:true,slot:0,smart:true,exchange:{_,ack in if ack == 0x82 { clear += 1 }; if ack == 0x80 { headers += 1 } },check:{ if pauses == 3 { throw LumeFailure("cancelled") } },pause:{ pauses += 1 },progress:{_,_ in})); XCTAssertEqual(clear,1); XCTAssertEqual(headers,0)
 }
 func testValidationBeforeMutation() {
  var count = 0; var bad = profile(); bad.Values[0][0] = -1
  XCTAssertThrowsError(try ProfileTransfer.run(profiles:[bad],replace:true,slot:0,smart:true,exchange:{_,_ in count += 1},check:{},pause:{},progress:{_,_ in})); XCTAssertEqual(count,0)
 }
 func testCompleteRealUDPTransaction() throws {
  let ip = IPv4Route.parse("127.0.0.1")!, server = try UDPSocket(address:ip,port:0), client = try UDPSocket(address:ip,port:0), id = "AABBCCDDEEFF"; defer { server.close(); client.close() }
  let completed = expectation(description:"full controller transfer")
  DispatchQueue.global().async { defer { completed.fulfill() }; do { var step = 0; let deadline = Date().addingTimeInterval(10)
   while step < 51 && Date() < deadline { guard let p = try server.receive() else { continue }; let b = p.bytes; XCTAssertEqual(b.count,64)
    let code: UInt8
    if step == 0 { XCTAssertEqual(Array(b[2..<7]),[1,1,0,4,2]); code = 0x80 }
    else if step <= 48 { XCTAssertEqual(b[6],UInt8(step-1)); code = 0x81 }
    else { XCTAssertEqual(b[2],step == 49 ? 3 : 4); code = step == 49 ? 0x83 : 0x91 }
    var ack = [UInt8](repeating:0,count:64); ack[0] = 0x88; ack[4] = code; ack.replaceSubrange(49..<61,with:try ControllerProtocol.identity(id)); try server.send(ack,to:ip,port:p.port); step += 1
   }; XCTAssertEqual(step,51)
  } catch { XCTFail(error.localizedDescription) } }
  try ProfileTransfer.run(profiles:[profile()],replace:false,slot:2,smart:true,exchange:{ payload,code in try Exchange.run(socket:client,target:ip,port:server.localPort,payload:payload,id:id,accepted:[code],smartOnly:true,timeout:1) },check:{},pause:{},progress:{_,_ in})
  wait(for:[completed],timeout:12)
 }
}
