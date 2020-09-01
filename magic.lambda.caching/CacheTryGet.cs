﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.lambda.caching
{
    /// <summary>
    /// [cache.try-get] slot saving its first child node's value to the memory cache.
    /// </summary>
    [Slot(Name = "cache.try-get")]
    [Slot(Name = "wait.cache.try-get")]
    public class CacheTryGet : ISlotAsync, ISlot
    {
        readonly IMemoryCache _cache;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="cache">Actual implementation.</param>
        public CacheTryGet(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            var key = input.GetEx<string>() ?? 
                throw new ArgumentNullException("[cache.try-get] must be given a key");

            var lambda = input.Children.FirstOrDefault(x => x.Name == ".lambda")?.Clone();
            if (lambda == null)
                throw new ArgumentNullException("[cache.try-get] must have a [.lambda] argument");

            input.Value = _cache.GetOrCreate(key, entry =>
            {
                var result = new Node();
                signaler.Scope("slots.result", result, () =>
                {
                    signaler.Signal("eval", lambda);
                });
                return result.Value ?? result;
            });
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            var key = input.GetEx<string>() ?? 
                throw new ArgumentNullException("[cache.try-get] must be given a key");

            var lambda = input.Children.FirstOrDefault(x => x.Name == ".lambda")?.Clone();
            if (lambda == null)
                throw new ArgumentNullException("[cache.try-get] must have a [.lambda] argument");

            input.Value = await _cache.GetOrCreate(key, async entry =>
            {
                var result = new Node();
                await signaler.ScopeAsync("slots.result", result, async () =>
                {
                    await signaler.SignalAsync("wait.eval", lambda);
                });
                return result.Value ?? result;
            });
        }
    }
}