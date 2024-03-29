/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using magic.node;
using magic.signals.services;
using magic.signals.contracts;
using magic.node.extensions.hyperlambda;

namespace magic.lambda.caching.tests
{
    public static class Common
    {
        static public Node Evaluate(string hl, bool config = true)
        {
            var signaler = Initialize(config);
            var lambda = new Parser(hl).Lambda();
            signaler.Signal("eval", lambda);
            return lambda;
        }

        static async public Task<Node> EvaluateAsync(string hl)
        {
            var signaler = Initialize();
            var lambda = new Parser(hl).Lambda();
            await signaler.SignalAsync("eval", lambda);
            return lambda;
        }

        public static ISignaler Initialize(bool config = true)
        {
            var services = new ServiceCollection();
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration
                .SetupGet(x => x[It.Is<string>(x => x == "magic:caching:expiration")])
                .Returns(() => config ? "5" : null);
            mockConfiguration
                .SetupGet(x => x[It.Is<string>(x => x == "magic:caching:expiration-type")])
                .Returns(() => config ? "sliding" : null);
            services.AddTransient((svc) => mockConfiguration.Object);
            services.AddTransient<ISignaler, Signaler>();
            services.AddMemoryCache();
            var types = new SignalsProvider(InstantiateAllTypes<ISlot>(services));
            services.AddTransient<ISignalsProvider>((svc) => types);
            var provider = services.BuildServiceProvider();
            return provider.GetService<ISignaler>();
        }

        #region [ -- Private helper methods -- ]

        static IEnumerable<Type> InstantiateAllTypes<T>(ServiceCollection services) where T : class
        {
            var type = typeof(T);
            var result = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic && !x.FullName.StartsWith("Microsoft", StringComparison.InvariantCulture))
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var idx in result)
            {
                services.AddTransient(idx);
            }
            return result;
        }

        #endregion
    }
}
