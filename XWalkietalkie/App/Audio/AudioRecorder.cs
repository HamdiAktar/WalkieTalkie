using Android.Content;
using Android.Media;
using Android.OS;
using Android.Util;
using Java.IO;
using Java.Lang;
using Thread = System.Threading.Thread;

namespace XWalkietalkie
{
    public class AudioRecorder
    {
        private string Tag = "WalkiTalki";

        // The stream to write to.
        private OutputStream OutputStream;

        //If true, the background thread will continue to loop and record audio. Once false, the thread
        //will shut down.
        private volatile bool Alive;

        // The background thread recording audio for us.
        private Thread MyThread;

        /// <summary>
        /// A simple audio recorder.
        /// </summary>
        /// <param name="file">The output stream of the recording.</param>
        /// <param name="context"></param>
        public AudioRecorder(ParcelFileDescriptor file,Context context)
        {
            OutputStream = new ParcelFileDescriptor.AutoCloseOutputStream(file);
        }

        // return True if actively recording. False otherwise.
        public bool IsRecording()
        {
            return Alive;
        }

        // Starts recording audio.
        public void Start()
        {
            if (IsRecording())
            {
                Log.Warn(Tag, "Already running");
                return;
            }

            Alive = true;
            MyThread = new Thread(delegate ()
            {
                Buffer buffer = new Buffer();
                AudioRecord record = new AudioRecord(
                            AudioSource.Default,
                            buffer.SampleRate,
                            ChannelIn.Mono,
                            Android.Media.Encoding.Pcm16bit,
                            buffer.Size);
                
                if (record.State != State.Initialized)
                {
                    Log.Warn(Tag, "Failed to start recording");
                    Alive = false;
                    return;
                }
                record.StartRecording();

                // While we're running, we'll read the bytes from the AudioRecord and write them
                // to our output stream.
                try
                {
                    while (IsRecording())
                    {
                        int len = record.Read(buffer.Data, 0, buffer.Size);
                        if (len >= 0 && len <= buffer.Size)
                        {
                            OutputStream.Write(buffer.Data, 0, len);
                            OutputStream.Flush();
                        }
                        else
                        {
                            Log.Warn(Tag, "Unexpected length returned: " + len);
                        }
                    }
                }
                catch (IOException e)
                {
                    Log.Error(Tag, "Exception with recording stream", e);
                }
                catch (InterruptedException e)
                {
                    Log.Error(Tag, "Interrupted while joining AudioRecorder thread", e);
                    System.Threading.Thread.CurrentThread.Interrupt();
                }
                finally
                {
                    StopInternal();
                    try
                    {
                        record.Stop();
                    }
                    catch (IllegalStateException e)
                    {
                        Log.Error(Tag, "Failed to stop AudioRecord", e);
                    }
                    record.Release();
                }
            });

            MyThread.Start();
        }

        // Stops recording audio.
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
                System.Threading.Thread.CurrentThread.Interrupt();
            }

        }

        private void StopInternal()
        {
            Alive = false;
            try
            {
                OutputStream.Close();
            }
            catch (IOException e)
            {
                Log.Error(Tag, "Failed to close output stream", e);
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
                return AudioRecord.GetMinBufferSize(
                    sampleRate, ChannelIn.Mono, Android.Media.Encoding.Pcm16bit);
            }
        }
    }
}