using Android.Media;
using Android.Util;
using Java.IO;
using Java.Lang;
using Thread = System.Threading.Thread;

namespace XWalkietalkie
{
    public class AudioPlayer
    {
        private string Tag = "WalkiTalki";

        private System.IO.Stream InputStream;

        //If true, the background thread will continue to loop and play audio. Once false, the thread
        //will shut down.
        private volatile bool Alive;

        // The background thread recording audio for us.
        private Thread MyThread;

        /// <summary>
        /// A simple audio player.
        /// </summary>
        /// <param name="inputStream">The input stream of the recording.</param>
        public AudioPlayer(System.IO.Stream inputStream)
        {
            InputStream = inputStream;
        }

        // return True if currently playing.
        public bool IsPlaying()
        {
            return Alive;
        }
        public void Start()
        {
            Alive = true;
            MyThread = new Thread(delegate ()
            {
                Buffer buffer = new Buffer();
                AudioTrack audioTrack = new AudioTrack(
                    Stream.Music,
                    buffer.SampleRate,
                    ChannelOut.Mono,
                    Android.Media.Encoding.Pcm16bit,
                    buffer.Size,
                    AudioTrackMode.Stream);

                audioTrack.Play();

                int len;
                try
                {
                    while (IsPlaying() && (len = InputStream.Read(buffer.Data)) > 0)
                    {
                        audioTrack.Write(buffer.Data, 0, len);
                    }
                }
                catch (IOException e)
                {
                    Log.Error(Tag, "Exception with playing stream", e);
                }
                finally
                {
                    StopInternal();
                    audioTrack.Release();
                }
            });

            MyThread.Start();
        }
        private void StopInternal()
        {
            Alive = false;
            try
            {
                InputStream.Close();
            }
            catch (IOException e)
            {
                Log.Error(Tag, "Failed to close input stream", e);
            }
        }
        //Stops playing the stream.
        public void Stop()
        {
            StopInternal();
            try
            {
                MyThread.Join();
            }
            catch (InterruptedException e)
            {
                Log.Error(Tag, "Interrupted while joining AudioRecorder thread", e);
                Thread.CurrentThread.Interrupt();
            }
        }
        class Buffer : AudioBuffer
        {
            protected override bool ValidSize(int size)
            {
                return size != -1 && size != -2;
            }

            protected override int GetMinBufferSize(int sampleRate)
            {
                return AudioTrack.GetMinBufferSize(
                    sampleRate, ChannelOut.Mono, Android.Media.Encoding.Pcm16bit);
            }
        }
    }
}