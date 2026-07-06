using System;

namespace Armoury.UI
{
    public readonly struct InputValue
    {
        private readonly InputValueKind _kind;

        private readonly bool _bool;
        private readonly int _int;
        private readonly float _float;
        private readonly double _double;
        private readonly object _object;

        public InputValueKind Kind => _kind;

        private InputValue(
            InputValueKind kind, bool boolValue, int intValue, float floatValue, double doubleValue, object objectValue
        )
        {
            _kind = kind;
            _bool = boolValue;
            _int = intValue;
            _float = floatValue;
            _double = doubleValue;
            _object = objectValue;
        }

        public static InputValue None() 
            => new(InputValueKind.None, false, 0, 0f, 0d, null);
        
        public static InputValue FromBool(bool value)
            => new(InputValueKind.Bool, value, 0, 0f, 0d, null);

        public static InputValue FromInt(int value)
            => new(InputValueKind.Int, false, value, 0f, 0d, null);

        public static InputValue FromFloat(float value)
            => new(InputValueKind.Float, false, 0, value, 0d, null);

        public static InputValue FromDouble(double value)
            => new(InputValueKind.Double, false, 0, 0f, value, null);

        public static InputValue FromString(string value)
            => new(InputValueKind.String, false, 0, 0f, 0d, value);

        public static InputValue FromObject<T>(T value) where T : class
            => new(InputValueKind.Object, false, 0, 0f, 0d, value);

        public bool AsBool()
        {
            if (_kind != InputValueKind.Bool)
                ThrowInvalidCast(InputValueKind.Bool);

            return _bool;
        }
        
        public int AsInt()
        {
            if (_kind != InputValueKind.Int)
                ThrowInvalidCast(InputValueKind.Int);

            return _int;
        }
        
        public float AsFloat()
        {
            if (_kind != InputValueKind.Float)
                ThrowInvalidCast(InputValueKind.Float);

            return _float;
        }
        
        public double AsDouble()
        {
            if (_kind != InputValueKind.Double)
                ThrowInvalidCast(InputValueKind.Double);

            return _double;
        }
        
        public string AsString()
        {
            if (_kind != InputValueKind.String)
                ThrowInvalidCast(InputValueKind.String);

            return (string) _object;
        }

        public T AsObject<T>() where T : class
        {
            if (_kind != InputValueKind.Object)
                ThrowInvalidCast(InputValueKind.Object);

            var value = _object as T;

            if (value == null && _object != null)
            {
                throw new InvalidOperationException(
                    $"Input value is of type '{_object.GetType().FullName}', not '{typeof(T).FullName}'.");
            }

            return value;
        }

        private void ThrowInvalidCast(InputValueKind expected)
        {
            throw new InvalidOperationException(
                $"Input value is '{_kind}', but '{expected}' was requested.");
        }
    }
}