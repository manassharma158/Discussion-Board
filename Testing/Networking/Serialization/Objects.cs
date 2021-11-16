﻿namespace Testing.Networking.Objects
{
    public class SimpleObject
    {
        // Default Parameterless Constructor
        public SimpleObject()
        {
        }

        public SimpleObject(int arg1, string arg2, double arg3, char arg4, bool arg5)
        {
            prop1 = arg1;
            prop2 = arg2;
            prop3 = arg3;
            prop4 = arg4;
            prop5 = arg5;
        }

        public int prop1 { get; set; }
        public string prop2 { get; set; }
        public double prop3 { get; set; }
        public char prop4 { get; set; }
        public bool prop5 { get; set; }
    }

    public class ComplexObject
    {
        public ComplexObject()
        {
        }

        public ComplexObject(SimpleObject arg1, string arg7)
        {
            prop1 = arg1;
            prop2 = arg7;
        }

        public ComplexObject(int arg1, string arg2, double arg3, char arg4, bool arg5, string arg6)
        {
            prop1 = new SimpleObject(arg1, arg2, arg3, arg4, arg5);
            prop2 = arg6;
        }

        public SimpleObject prop1 { get; set; }
        public string prop2 { get; set; }
    }

    public class NonSerializableObject
    {
        public NonSerializableObject(int arg1)
        {
            prop1 = arg1;
        }

        public int prop1 { get; set; }
    }

    public class NonSerializableAttribute
    {
        public NonSerializableAttribute()
        {
        }

        public NonSerializableAttribute(int arg1, int arg2)
        {
            prop1 = arg1;
            prop2 = new NonSerializableObject(arg1);
        }

        public int prop1 { get; set; }
        public NonSerializableObject prop2 { get; set; }
    }
}