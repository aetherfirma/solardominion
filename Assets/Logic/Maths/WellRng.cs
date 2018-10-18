using System;
using System.Security.Cryptography;
using System.Text;

namespace Logic.Maths
{
    [Serializable]
    public class WellRng
    {
        public ulong[] State;
        public int Index;

        private void SetState(ulong[] state, int index)
        {
            if (state.Length != 16) throw new ArgumentOutOfRangeException();
            State = (ulong[]) state.Clone();
            Index = index;
        }

        public WellRng(ulong[] state, int index)
        {
            SetState(state, index);
        }

        public WellRng(string seed)
        {
            var state = new ulong[16];
            using (var sha512 = SHA512.Create())
            {
                var halfState = sha512.ComputeHash(Encoding.UTF8.GetBytes(seed));
                for (var n = 0; n < 8; n++)
                {
                    state[n] = state[n + 8] = BitConverter.ToUInt64(halfState, n * 8);
                }
            }
            SetState(state, 0);
        }

        public uint NextUint()
        {
            // WELL512, stolen from Chris Lomont
            // http://lomont.org/Math/Papers/2008/Lomont_PRNG_2008.pdf
            ulong a, b, c, d;
            a = State[Index];
            c = State[(Index + 13) & 15];
            b = a ^ c ^ (a << 16) ^ (c << 15);
            c = State[(Index + 9) & 15];
            c ^= c >> 11;
            a = State[Index] = b ^ c;
            d = a ^ ((a << 5) & 0xDA442D24UL);
            Index = (Index + 15) & 15;
            a = State[Index];
            State[Index] = a ^ b ^ d ^ (a << 2) ^ (b << 18) ^ (c << 28);
            return (uint) State[Index];
        }

        public double NextUnitDouble()
        {
            var r = NextUint();
            return (double) r / uint.MaxValue;
        }

        public int NextInt(int min = int.MinValue, int max = int.MaxValue)
        {
            var delta = (long) max + 1 - min;
            var spread = (int) (NextUint() % delta);
            return spread + min;
        }

        public float NextFloat(float min = float.MinValue, float max = float.MaxValue)
        {
            var delta = max + 1 - min;
            return (float) (NextUnitDouble() * delta + min);
        }

        public bool NextBool()
        {
            return NextInt(0, 1) == 1;
        }
    }
}