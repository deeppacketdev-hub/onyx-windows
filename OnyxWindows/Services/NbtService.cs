using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace OnyxWindows.Services;

public enum NbtType : byte
{
    End = 0,
    Byte = 1,
    Short = 2,
    Int = 3,
    Long = 4,
    Float = 5,
    Double = 6,
    ByteArray = 7,
    String = 8,
    List = 9,
    Compound = 10,
    IntArray = 11,
    LongArray = 12
}

public class NbtTag
{
    public NbtType Type { get; set; }
    public object? Value { get; set; }

    public NbtTag(NbtType type, object? value)
    {
        Type = type;
        Value = value;
    }

    public static NbtTag End => new(NbtType.End, null);
    public static NbtTag CreateByte(byte val) => new(NbtType.Byte, val);
    public static NbtTag CreateShort(short val) => new(NbtType.Short, val);
    public static NbtTag CreateInt(int val) => new(NbtType.Int, val);
    public static NbtTag CreateLong(long val) => new(NbtType.Long, val);
    public static NbtTag CreateFloat(float val) => new(NbtType.Float, val);
    public static NbtTag CreateDouble(double val) => new(NbtType.Double, val);
    public static NbtTag CreateString(string val) => new(NbtType.String, val);
    public static NbtTag CreateCompound(Dictionary<string, NbtTag> val) => new(NbtType.Compound, val);
}

public class NbtParser
{
    private readonly byte[] _data;
    private int _cursor = 0;

    public NbtParser(byte[] data)
    {
        _data = data;
    }

    public (string Name, NbtTag Tag)? Parse()
    {
        if (_cursor >= _data.Length) return null;

        var typeId = _data[_cursor++];
        if (typeId == 0) return ("", NbtTag.End);

        var name = ReadString();
        var tag = ReadTagPayload((NbtType)typeId);

        return (name, tag);
    }

    private byte ReadByte() => _data[_cursor++];

    private short ReadShort()
    {
        var val = BitConverter.ToInt16(_data, _cursor);
        if (BitConverter.IsLittleEndian)
        {
            val = System.Net.IPAddress.NetworkToHostOrder(val);
        }
        _cursor += 2;
        return val;
    }

    private int ReadInt()
    {
        var val = BitConverter.ToInt32(_data, _cursor);
        if (BitConverter.IsLittleEndian)
        {
            val = System.Net.IPAddress.NetworkToHostOrder(val);
        }
        _cursor += 4;
        return val;
    }

    private long ReadLong()
    {
        var val = BitConverter.ToInt64(_data, _cursor);
        if (BitConverter.IsLittleEndian)
        {
            val = System.Net.IPAddress.NetworkToHostOrder(val);
        }
        _cursor += 8;
        return val;
    }

    private float ReadFloat()
    {
        var bytes = new byte[4];
        Array.Copy(_data, _cursor, bytes, 0, 4);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _cursor += 4;
        return BitConverter.ToSingle(bytes, 0);
    }

    private double ReadDouble()
    {
        var bytes = new byte[8];
        Array.Copy(_data, _cursor, bytes, 0, 8);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _cursor += 8;
        return BitConverter.ToDouble(bytes, 0);
    }

    private string ReadString()
    {
        var length = (ushort)ReadShort();
        if (length == 0) return "";
        var val = Encoding.UTF8.GetString(_data, _cursor, length);
        _cursor += length;
        return val;
    }

    private NbtTag ReadTagPayload(NbtType type)
    {
        switch (type)
        {
            case NbtType.End:
                return NbtTag.End;
            case NbtType.Byte:
                return NbtTag.CreateByte(ReadByte());
            case NbtType.Short:
                return NbtTag.CreateShort(ReadShort());
            case NbtType.Int:
                return NbtTag.CreateInt(ReadInt());
            case NbtType.Long:
                return NbtTag.CreateLong(ReadLong());
            case NbtType.Float:
                return NbtTag.CreateFloat(ReadFloat());
            case NbtType.Double:
                return NbtTag.CreateDouble(ReadDouble());
            case NbtType.ByteArray:
                var len = ReadInt();
                var bytes = new byte[len];
                Array.Copy(_data, _cursor, bytes, 0, len);
                _cursor += len;
                return new NbtTag(NbtType.ByteArray, bytes);
            case NbtType.String:
                return NbtTag.CreateString(ReadString());
            case NbtType.List:
                var elemTypeId = ReadByte();
                var count = ReadInt();
                var elements = new List<NbtTag>();
                for (int i = 0; i < count; i++)
                {
                    elements.Add(ReadTagPayload((NbtType)elemTypeId));
                }
                return new NbtTag(NbtType.List, (elemTypeId, elements));
            case NbtType.Compound:
                var dict = new Dictionary<string, NbtTag>();
                while (true)
                {
                    var nextType = ReadByte();
                    if (nextType == 0) break;
                    var name = ReadString();
                    var payload = ReadTagPayload((NbtType)nextType);
                    dict[name] = payload;
                }
                return NbtTag.CreateCompound(dict);
            case NbtType.IntArray:
                var ilen = ReadInt();
                var ints = new int[ilen];
                for (int i = 0; i < ilen; i++) ints[i] = ReadInt();
                return new NbtTag(NbtType.IntArray, ints);
            case NbtType.LongArray:
                var llen = ReadInt();
                var longs = new long[llen];
                for (int i = 0; i < llen; i++) longs[i] = ReadLong();
                return new NbtTag(NbtType.LongArray, longs);
            default:
                throw new Exception($"Invalid NBT tag type: {type}");
        }
    }
}

public class NbtWriter
{
    private readonly MemoryStream _stream = new();

    public byte[] Write(string rootName, NbtTag rootTag)
    {
        _stream.SetLength(0);
        _stream.Position = 0;

        _stream.WriteByte((byte)rootTag.Type);
        WriteString(rootName);
        WriteTagPayload(rootTag);

        return _stream.ToArray();
    }

    private void WriteByte(byte val) => _stream.WriteByte(val);

    private void WriteShort(short val)
    {
        if (BitConverter.IsLittleEndian)
        {
            val = System.Net.IPAddress.HostToNetworkOrder(val);
        }
        var bytes = BitConverter.GetBytes(val);
        _stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteInt(int val)
    {
        if (BitConverter.IsLittleEndian)
        {
            val = System.Net.IPAddress.HostToNetworkOrder(val);
        }
        var bytes = BitConverter.GetBytes(val);
        _stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteLong(long val)
    {
        if (BitConverter.IsLittleEndian)
        {
            val = System.Net.IPAddress.HostToNetworkOrder(val);
        }
        var bytes = BitConverter.GetBytes(val);
        _stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteFloat(float val)
    {
        var bytes = BitConverter.GetBytes(val);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _stream.Write(bytes, 0, 4);
    }

    private void WriteDouble(double val)
    {
        var bytes = BitConverter.GetBytes(val);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _stream.Write(bytes, 0, 8);
    }

    private void WriteString(string val)
    {
        var bytes = Encoding.UTF8.GetBytes(val);
        WriteShort((short)bytes.Length);
        _stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteTagPayload(NbtTag tag)
    {
        switch (tag.Type)
        {
            case NbtType.End:
                break;
            case NbtType.Byte:
                WriteByte((byte)tag.Value!);
                break;
            case NbtType.Short:
                WriteShort((short)tag.Value!);
                break;
            case NbtType.Int:
                WriteInt((int)tag.Value!);
                break;
            case NbtType.Long:
                WriteLong((long)tag.Value!);
                break;
            case NbtType.Float:
                WriteFloat((float)tag.Value!);
                break;
            case NbtType.Double:
                WriteDouble((double)tag.Value!);
                break;
            case NbtType.ByteArray:
                var bytes = (byte[])tag.Value!;
                WriteInt(bytes.Length);
                _stream.Write(bytes, 0, bytes.Length);
                break;
            case NbtType.String:
                WriteString((string)tag.Value!);
                break;
            case NbtType.List:
                var (elemTypeId, list) = ((byte, List<NbtTag>))tag.Value!;
                WriteByte(elemTypeId);
                WriteInt(list.Count);
                foreach (var item in list) WriteTagPayload(item);
                break;
            case NbtType.Compound:
                var dict = (Dictionary<string, NbtTag>)tag.Value!;
                foreach (var kp in dict)
                {
                    WriteByte((byte)kp.Value.Type);
                    WriteString(kp.Key);
                    WriteTagPayload(kp.Value);
                }
                WriteByte(0); // End tag
                break;
            case NbtType.IntArray:
                var ints = (int[])tag.Value!;
                WriteInt(ints.Length);
                foreach (var i in ints) WriteInt(i);
                break;
            case NbtType.LongArray:
                var longs = (long[])tag.Value!;
                WriteInt(longs.Length);
                foreach (var l in longs) WriteLong(l);
                break;
        }
    }
}

public static class NbtService
{
    public static (string Name, NbtTag Tag)? ReadLevelDat(string filePath)
    {
        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var memStream = new MemoryStream();
            gzipStream.CopyTo(memStream);

            var parser = new NbtParser(memStream.ToArray());
            return parser.Parse();
        }
        catch
        {
            return null;
        }
    }

    public static void WriteLevelDat(string rootName, NbtTag tag, string filePath)
    {
        var backupPath = filePath + "_old";
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(backupPath);
                File.Copy(filePath, backupPath);
            }
            catch { }
        }

        var writer = new NbtWriter();
        var uncompressedBytes = writer.Write(rootName, tag);

        using var fileStream = File.Create(filePath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
        gzipStream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
    }
}
