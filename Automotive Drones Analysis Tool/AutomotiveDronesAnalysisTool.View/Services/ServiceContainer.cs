using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    /// <summary>
    /// Container class that stores all services.
    /// </summary>
    public class ServiceContainer
    {

        private static Dictionary<string, ServiceBase> _serviceContainer;

        /// <summary>
        /// Creates a new empty container.
        /// </summary>
        public static void CreateContainer() => _serviceContainer = new Dictionary<string, ServiceBase>();

        /// <summary>
        /// Registers the given service to the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        public static void RegisterService<T>(ServiceBase service) => _serviceContainer.Add(typeof(T).Name, service);

        /// <summary>
        /// Gets a desired service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetService<T>() where T : ServiceBase
        {
            if (_serviceContainer.TryGetValue(typeof(T).Name, out var service))
                return (T)service;
            else
                throw new KeyNotFoundException("Given service has not been registered.");
        }

    }
}
