using System;
using UnityEngine;
#if WINDOWS_UWP
using System.IO;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

#endif

public class SpatialVoicePlayer : MonoBehaviour
{
    private AudioSource _source;

    [Tooltip("The string to say")]
    public  string TextToSay = "Hello HoloLens";

    private WaveFileParser _wav;

#if WINDOWS_UWP
    public void PlayVoice()
    {
        _source = GetComponent<AudioSource>();
        if (_source == null)
            return;

        var myTask = ReadSpeechStream();
        myTask.Wait();

        var clip = AudioClip.Create(
            "voiceClip",
            _wav.SampleCount,
            _wav.ChannelCount,
            _wav.Frequency,
            false);

        clip.SetData(_wav.LeftChannel, 0);

        _source.clip = clip;
        _source.Play();
    }

    private async Task ReadSpeechStream()
    {
        using (var speechSynt = new SpeechSynthesizer())
        {
            var speechStream =
                await speechSynt.SynthesizeTextToStreamAsync(TextToSay);
            var stream = speechStream.AsStreamForRead();
            _wav = new WaveFileParser(stream);
        }
    }
#endif
}