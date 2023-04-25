using System;
using System.IO;
using UnityEngine;

public class WavUtility
{
    public static AudioClip ToAudioClip(byte[] data) {
        // ヘッダー解析
        int channels = data[22];
        int frequency = BitConverter.ToInt32(data, 24);
        int length = data.Length - 44;
        float[] samples = new float[length / 2];

        // 波形データ解析
        for (int i = 0; i < length / 2; i++) {
            short value = BitConverter.ToInt16(data, i * 2 + 44);
            samples[i] = value / 32768f;
        }

        // AudioClipを作成
        AudioClip audioClip = AudioClip.Create("AudioClip", samples.Length, channels, frequency, false);
        audioClip.SetData(samples, 0);

        return audioClip;
    }

    public static void WriteHeader(FileStream fileStream, AudioClip audioClip) {
        var hz = audioClip.frequency();
        var channels = audioClip.channels;
        var samples = audioClip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        byte[] chunkSize = System.BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        byte[] subChunk1 = System.BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        ushort one = 1;
        byte[] audioFormat = System.BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        byte[] numChannels = System.BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        byte[] sampleRate = System.BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        byte[] byteRate = System.BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels
        fileStream.Write(byteRate, 0, 4);

        ushort blockAlign = (ushort)(channels * 2);
        fileStream.Write(System.BitConverter.GetBytes(blockAlign), 0, 2);

        ushort bps = 16;
        byte[] bitsPerSample = System.BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        byte[] subChunk2 = System.BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}
