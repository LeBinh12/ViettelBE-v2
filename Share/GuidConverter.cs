using System.Numerics;

namespace Share;

public static class GuidConverter
{
    public static BigInteger GuidToUint256(Guid id)
    {
        var bytes = id.ToByteArray(); // 16 bytes
        // Convert to big-endian (most-significant first)
        var bigEndian = new byte[32]; // pad to 32 bytes
        // Guid's bytes are little endian on some parts; but as long as same rule used for push & read, consistent is enough.
        // We'll put Guid bytes into lower-order bytes (right side)
        Array.Copy(bytes, 0, bigEndian, 32 - 16, 16);
        return new BigInteger(bigEndian, isUnsigned: true, isBigEndian: true);
    }

    public static BigInteger GuidToUint256_ForNethereum(Guid id)
    {
        // Nethereum expects BigInteger; we can reuse above
        return GuidToUint256(id);
    }
    
    public static byte[] HexStringToBytes32(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentException("Hex string is null or empty");

        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];

        var bytes = new byte[32];

        var hexBytes = Enumerable.Range(0, hex.Length / 2)
            .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
            .ToArray();

        if (hexBytes.Length > 32)
            throw new ArgumentException("Hex string is too long for bytes32");

        // Copy vào cuối mảng 32 byte (right-aligned)
        Array.Copy(hexBytes, 0, bytes, 32 - hexBytes.Length, hexBytes.Length);

        return bytes;
    }

}