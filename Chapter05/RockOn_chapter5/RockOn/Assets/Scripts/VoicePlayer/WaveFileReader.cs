using System.IO;

public class WaveFileParser
{
    public WaveFileParser(Stream stream)
    {
        var size = stream.Length;
        var buffer = new byte[size];
        var data = stream.ReadByte();
        long counter = 0;
        while (data != -1)
        {
            buffer[counter++] = (byte) data;
            data = stream.ReadByte();
        }
        ConvertByteArray(buffer);
    }

    // properties
    public float[] LeftChannel { get; internal set; }
    public float[] RightChannel { get; internal set; }
    public int ChannelCount { get; internal set; }
    public int SampleCount { get; internal set; }
    public int Frequency { get; internal set; }


    private void ConvertByteArray(byte[] wav)
    {
        ChannelCount = wav[22];
        Frequency = BytesToInt(wav, 24);
        var pos = 12;
        while (!((wav[pos] == 100) && (wav[pos + 1] == 97) && (wav[pos + 2] == 116) && (wav[pos + 3] == 97)))
        {
            pos += 4;
            var chunkSize = wav[pos] + wav[pos + 1]*256 + wav[pos + 2]*65536 + wav[pos + 3]*16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;
        SampleCount = (wav.Length - pos)/2;
        if (ChannelCount == 2) SampleCount /= 2;


        LeftChannel = new float[SampleCount];
        if (ChannelCount == 2) RightChannel = new float[SampleCount];
        else RightChannel = null;

        var i = 0;
        while (pos < wav.Length)
        {
            LeftChannel[i] = BytesToFloat(wav[pos], wav[pos + 1]);
            pos += 2;
            if ((ChannelCount == 2) && (RightChannel != null))
            {
                RightChannel[i] = BytesToFloat(wav[pos], wav[pos + 1]);
                pos += 2;
            }
            i++;
        }
    }

  
    private static float BytesToFloat(byte firstByte, byte secondByte)
    {
        var s = (short) ((secondByte << 8) | firstByte);
        return s/32768.0F;
    }

    private static int BytesToInt(byte[] bytes, int offset = 0)
    {
        var value = 0;
        for (var i = 0; i < 4; i++)
            value |= bytes[offset + i] << (i*8);
        return value;
    }
}