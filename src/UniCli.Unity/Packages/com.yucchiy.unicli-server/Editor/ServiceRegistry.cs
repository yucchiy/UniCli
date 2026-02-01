using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UniCli.Server.Editor
{
    public sealed class ServiceRegistry
    {
        private readonly Dictionary<Type, object> _instances = new();
        private readonly Dictionary<Type, Type> _typeMap = new();

        public ServiceRegistry AddSingleton<TService>(TService instance) where TService : class
        {
            _instances[typeof(TService)] = instance ?? throw new ArgumentNullException(nameof(instance));
            return this;
        }

        public ServiceRegistry AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _typeMap[typeof(TService)] = typeof(TImplementation);
            return this;
        }

        public ServiceRegistry AddSingleton<TService>() where TService : class
        {
            _typeMap[typeof(TService)] = typeof(TService);
            return this;
        }

        private object GetService(Type serviceType)
        {
            if (_instances.TryGetValue(serviceType, out var instance))
            {
                return instance;
            }

            if (!_typeMap.TryGetValue(serviceType, out var implType))
            {
                return null;
            }

            instance = CreateInstance(implType);
            if (instance != null)
            {
                _instances[serviceType] = instance;
            }

            return instance;
        }

        public object CreateInstance(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(c => c.GetParameters().Length)
                .ToArray();

            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                var args = new object[parameters.Length];
                var canResolve = true;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var service = GetService(parameters[i].ParameterType);
                    if (service == null)
                    {
                        canResolve = false;
                        break;
                    }
                    args[i] = service;
                }

                if (canResolve)
                {
                    return Activator.CreateInstance(type, args);
                }
            }

            return null;
        }
    }
}
