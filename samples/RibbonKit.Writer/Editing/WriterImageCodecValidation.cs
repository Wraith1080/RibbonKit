using System.Buffers.Binary;

namespace RibbonKit.Writer.Editing;

internal static class WriterImageCodecValidation
{
    internal const int MaximumHeaderBytes = 1 * 1024 * 1024;

    internal static bool IsAllowedSignature(ReadOnlySpan<byte> bytes, string? extension = null)
    {
        if (extension is not null)
            return extension switch
            {
                "png" => HasPngSignature(bytes),
                "jpg" or "jpeg" => HasJpegSignature(bytes),
                "gif" => HasGifSignature(bytes),
                "bmp" => HasBmpSignature(bytes),
                _ => false
            };

        return HasPngSignature(bytes) || HasJpegSignature(bytes)
            || HasGifSignature(bytes) || HasBmpSignature(bytes);
    }

    internal static bool TryReadDimensions(ReadOnlySpan<byte> bytes,
        out int width, out int height)
    {
        width = 0;
        height = 0;
        if (HasPngSignature(bytes))
            return TryReadPngDimensions(bytes, out width, out height);
        if (HasJpegSignature(bytes))
            return TryReadJpegDimensions(bytes, out width, out height);
        if (HasGifSignature(bytes))
            return TryReadGifDimensions(bytes, out width, out height);
        if (HasBmpSignature(bytes))
            return TryReadBmpDimensions(bytes, out width, out height);
        return false;
    }

    internal static bool IsWithinLimits(int width, int height, long maximumPixels,
        int maximumDimension) => width > 0 && height > 0
            && width <= maximumDimension && height <= maximumDimension
            && (long)width * height <= maximumPixels;

    private static bool HasPngSignature(ReadOnlySpan<byte> bytes) => bytes.Length >= 8
        && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

    private static bool HasJpegSignature(ReadOnlySpan<byte> bytes) => bytes.Length >= 3
        && bytes[..3].SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF });

    private static bool HasGifSignature(ReadOnlySpan<byte> bytes) => bytes.Length >= 6
        && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8));

    private static bool HasBmpSignature(ReadOnlySpan<byte> bytes) => bytes.Length >= 2
        && bytes[..2].SequenceEqual("BM"u8);

    private static bool TryReadPngDimensions(ReadOnlySpan<byte> bytes,
        out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bytes.Length < 24 || !bytes[12..16].SequenceEqual("IHDR"u8))
            return false;
        var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(bytes[8..12]);
        if (chunkLength < 13 || chunkLength > bytes.Length - 16)
            return false;
        width = ReadPositiveInt32(BinaryPrimitives.ReadUInt32BigEndian(bytes[16..20]));
        height = ReadPositiveInt32(BinaryPrimitives.ReadUInt32BigEndian(bytes[20..24]));
        return width > 0 && height > 0;
    }

    private static bool TryReadGifDimensions(ReadOnlySpan<byte> bytes,
        out int width, out int height)
    {
        width = bytes.Length >= 10 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..8]) : 0;
        height = bytes.Length >= 10 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[8..10]) : 0;
        return width > 0 && height > 0;
    }

    private static bool TryReadBmpDimensions(ReadOnlySpan<byte> bytes,
        out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bytes.Length < 26)
            return false;
        var dibSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[14..18]);
        if (dibSize == 12)
        {
            width = BinaryPrimitives.ReadUInt16LittleEndian(bytes[18..20]);
            height = BinaryPrimitives.ReadUInt16LittleEndian(bytes[20..22]);
            return width > 0 && height > 0;
        }
        if (dibSize < 40 || bytes.Length < 26)
            return false;
        var rawWidth = BinaryPrimitives.ReadInt32LittleEndian(bytes[18..22]);
        var rawHeight = BinaryPrimitives.ReadInt32LittleEndian(bytes[22..26]);
        if (rawWidth <= 0 || rawHeight == int.MinValue)
            return false;
        width = rawWidth;
        height = Math.Abs(rawHeight);
        return height > 0;
    }

    private static bool TryReadJpegDimensions(ReadOnlySpan<byte> bytes,
        out int width, out int height)
    {
        width = 0;
        height = 0;
        var index = 2;
        var limit = Math.Min(bytes.Length, MaximumHeaderBytes);
        while (index + 3 < limit)
        {
            if (bytes[index++] != 0xFF)
                continue;
            while (index < limit && bytes[index] == 0xFF)
                index++;
            if (index >= limit)
                return false;
            var marker = bytes[index++];
            if (marker is 0xD8 or 0xD9 or 0xDA)
                return false;
            if (marker is 0x01 or >= 0xD0 and <= 0xD7)
                continue;
            if (index + 2 > limit)
                return false;
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[index..]);
            if (segmentLength < 2 || index + segmentLength > limit)
                return false;
            if (IsJpegStartOfFrame(marker))
            {
                if (segmentLength < 7)
                    return false;
                height = BinaryPrimitives.ReadUInt16BigEndian(bytes[(index + 3)..]);
                width = BinaryPrimitives.ReadUInt16BigEndian(bytes[(index + 5)..]);
                return width > 0 && height > 0;
            }
            index += segmentLength;
        }
        return false;
    }

    private static bool IsJpegStartOfFrame(byte marker) => marker is
        0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7
        or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;

    private static int ReadPositiveInt32(uint value) => value is > int.MaxValue ? 0 : (int)value;
}
