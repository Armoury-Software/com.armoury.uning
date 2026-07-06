using System;

namespace Armoury.UI
{
    public struct InputChangeMask
    {
        private ulong _bits;

        public bool IsEmpty => _bits == 0UL;

        public void Mark(int inputIndex)
        {
            if ((uint) inputIndex >= 64u)
                throw new ArgumentOutOfRangeException(nameof(inputIndex));

            _bits |= 1UL << inputIndex;
        }

        public bool Has(int inputIndex)
        {
            if ((uint)inputIndex >= 64u)
                throw new ArgumentOutOfRangeException(nameof(inputIndex));

            return (_bits & (1UL << inputIndex)) != 0UL;
        }

        public void Clear()
        {
            _bits = 0UL;
        }
    }
}