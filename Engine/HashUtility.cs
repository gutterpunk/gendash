namespace GenDash.Engine
{
    public static class HashUtility
    {
        private const ulong FnvOffset = 14695981039346656037;
        private const ulong FnvPrime = 1099511628211;

        public static ulong ComputeFNV1aHash(byte width, byte height, byte startX, byte startY, byte exitX, byte exitY, byte idle, string data)
        {
            ulong hash = FnvOffset;
            
            hash ^= width;
            hash *= FnvPrime;
            hash ^= height;
            hash *= FnvPrime;
            
            hash ^= startX;
            hash *= FnvPrime;
            hash ^= startY;
            hash *= FnvPrime;
            
            hash ^= exitX;
            hash *= FnvPrime;
            hash ^= exitY;
            hash *= FnvPrime;
            
            hash ^= idle;
            hash *= FnvPrime;
            
            foreach (char c in data)
            {
                hash ^= (byte)c;
                hash *= FnvPrime;
            }
            
            return hash;
        }

        public static ulong ComputeFNV1aHash(byte width, byte height, string data)
        {
            ulong hash = FnvOffset;
            
            hash ^= width;
            hash *= FnvPrime;
            hash ^= height;
            hash *= FnvPrime;
            
            foreach (char c in data)
            {
                hash ^= (byte)c;
                hash *= FnvPrime;
            }
            
            return hash;
        }
    }
}
