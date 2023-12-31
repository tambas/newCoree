﻿using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Giny.Protocol
{
    public static class ProtocolTypeManager
    {
        private static readonly Dictionary<ushort, Type> m_types = new Dictionary<ushort, Type>(200);

        private static readonly Dictionary<ushort, Func<object>> m_typesConstructors = new Dictionary<ushort, Func<object>>(200);

        public static void Initialize()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(BufferInformation));
            foreach (var type in assembly.GetTypes().Where(x => x.Namespace == "Giny.Protocol.Types"))
            {
                FieldInfo field = type.GetField("Id");
                if (field != null)
                {
                    ushort key = (ushort)field.GetValue(type);
                    m_types.Add(key, type);
                    ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor == null)
                    {
                        throw new Exception(string.Format("'{0}' doesn't implemented a parameterless constructor", type));
                    }
                    m_typesConstructors.Add(key, constructor.CreateDelegate<Func<object>>());
                }
            }
        }
        public static T GetInstance<T>(short id) where T : class
        {
            if (!m_types.ContainsKey((ushort)id))
            {
                throw new ProtocolTypeNotFoundException(string.Format("Type <id:{0}> doesn't exist", id));
            }
            return ProtocolTypeManager.m_typesConstructors[(ushort)id]() as T;
        }
    }
    [Serializable]
    public class ProtocolTypeNotFoundException : Exception
    {
        public ProtocolTypeNotFoundException()
        {
        }
        public ProtocolTypeNotFoundException(string message)
            : base(message)
        {
        }
        public ProtocolTypeNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
        protected ProtocolTypeNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    public static class ReflectionHelper
    {
        public static T CreateDelegate<T>(this ConstructorInfo ctor)
        {
            List<ParameterExpression> list = Enumerable.ToList<ParameterExpression>(Enumerable.Select<ParameterInfo, ParameterExpression>((IEnumerable<ParameterInfo>)ctor.GetParameters(), (Func<ParameterInfo, ParameterExpression>)(param => Expression.Parameter(param.ParameterType))));
            return Expression.Lambda<T>((Expression)Expression.New(ctor, (IEnumerable<Expression>)list), (IEnumerable<ParameterExpression>)list).Compile();
        }
    }
}
