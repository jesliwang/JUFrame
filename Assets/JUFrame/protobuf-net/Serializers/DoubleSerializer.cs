﻿#if !NO_RUNTIME
using System;
using MuffinProtoBuf.Meta;

#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;
#endif

namespace MuffinProtoBuf.Serializers
{
    sealed class DoubleSerializer : IProtoSerializer
    {
#if FEAT_IKVM
        readonly Type expectedType;
#else
        static readonly Type expectedType = typeof(double);
#endif
        public DoubleSerializer(MuffinProtoBuf.Meta.TypeModel model)
        {
#if FEAT_IKVM
            expectedType = model.MapType(typeof(double));
#endif
        }

        public Type ExpectedType { get { return expectedType; } }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
#if !FEAT_IKVM
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadDouble();
        }
        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteDouble((double)value, dest);
        }
#endif
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteDouble", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadDouble", ExpectedType);
        }
#endif
    }
}
#endif