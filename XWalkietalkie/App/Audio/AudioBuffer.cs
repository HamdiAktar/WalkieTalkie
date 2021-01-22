
namespace XWalkietalkie
{
    abstract class AudioBuffer
    {
        private static int[] PossibleSampleRates = new int[] { 8000, 11025, 16000, 22050, 44100, 48000 };

        public int Size;
        public int SampleRate;
        public byte[] Data;

        protected AudioBuffer()
        {
            int size = -1;
            int samplerate = -1;

            // Iterate over all possible sample rates, and try to find the shortest one. 
            // The shorter it is, the faster it'll stream.
            foreach (int rate in PossibleSampleRates)
            {
                samplerate = rate;
                size = GetMinBufferSize(samplerate);
                if (ValidSize(size))
                {
                    break;
                }
            }

            // If none of them good, then, pick 1kb
            if (!ValidSize(size))
            {
                size = 1024;
            }

            Size = size;
            SampleRate = samplerate;
            Data = new byte[size];
        }

        protected abstract bool ValidSize(int size);

        protected abstract int GetMinBufferSize(int SampleRate);
    }
}