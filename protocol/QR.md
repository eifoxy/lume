# Offline profile transfer, wire format v1

The transfer carries a snapshot of the local Lume library. It does not carry network credentials, controller identities, hardware instructions, URLs or executable content. Import requires explicit confirmation and uses the existing library validator and append flow. CRC-32 detects accidental corruption; it does not authenticate the sender or encrypt the data.

Each QR contains ASCII text:

```text
LUMEQR1:<transfer-id>:<part>:<count>:<crc32>:<base64url>
```

- Transfer ID: 16 hexadecimal digits, randomly generated per export selection.
- Part/count: decimal, 1-based, at most 384 parts.
- CRC: eight hexadecimal digits, IEEE CRC-32 of the complete binary library. Initial state `FFFFFFFF`, reflected polynomial `EDB88320`, final XOR `FFFFFFFF`. Test vector `123456789` produces `cbf43926`.
- Payload: unpadded base64url of at most 360 bytes; every non-final chunk is exactly 360 bytes. Maximum frame text length: 600 characters.
- Receiver: accepts reorder and identical repeats, rejects conflicting duplicates or a different ID/count/checksum, caps the assembled library at 131072 bytes, expires a scan session after fifteen minutes, verifies CRC before decoding.

Binary integers are little endian. Library layout:

| Field | Encoding |
|---|---|
| Magic/version | `4C 55 4D 45 01` |
| Profile count | uint16, 1..100 |
| Selected profile | uint16, zero based, less than count |
| Each profile name | uint16 UTF-8 byte length, followed by strict UTF-8, at most 80 UTF-16 code units |
| Each of five channel names | same encoding, at most 40 UTF-16 code units |
| Each of 48 schedule points | five uint8 values, each 0..100 |

Names must be nonempty after trimming. Reject invalid UTF-8, truncated fields, trailing data, invalid counts, and out-of-range intensities. A string's maximum UTF-8 byte count is three times its UTF-16 limit. Preserve names and channel values exactly; language preferences must not translate user data.

The sender's default is one selected profile with selected index zero. All-profile export retains the PC's selected index in the payload, while mobile append currently selects the first newly imported profile, matching JSON import. No receiver reads or rewrites controller memory. Existing phone profiles remain untouched until confirmation; the combined library must remain within its 100-profile limit.

`qr-fixture.json` contains synthetic Windows-generated frames and expected library data. `scripts/test-qr.cjs` checks mobile reassembly against this fixture and against fresh output of Windows tests. `QrImageTest.java` decodes an actual PNG from QRCoder using the same ZXing core used by the Android camera scanner. These tests do not validate a physical camera, its focus, or monitor glare.
