﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Norm.BSON;

namespace Norm.Collections
{
    /// <summary>
    /// Class that generates a new identity value using the HILO algorithm.
    /// Only one instance of this class should be used in your project
    /// </summary>
    public class HiLoIdGenerator
    {
        private readonly long _capacity;
        private readonly object generatorLock = new object();
        private long _currentHi;
        private long _currentLo;
        private MongoDatabase _db;

        public HiLoIdGenerator(MongoDatabase db, long capacity)
        {
            _db = db;
            _currentHi = 0;
            _capacity = capacity;
            _currentLo = capacity + 1;
        }

        /// <summary>
        /// Generates a new identity value
        /// </summary>
        /// <param name="collectionName">Collection Name</param>
        /// <returns></returns>
        public long GenerateId(string collectionName)
        {
            long incrementedCurrentLow = Interlocked.Increment(ref _currentLo);
            if (incrementedCurrentLow > _capacity)
            {
                lock (generatorLock)
                {
                    if (Thread.VolatileRead(ref _currentLo) > _capacity)
                    {
                        _currentHi = GetNextHi(collectionName);
                        _currentLo = 1;
                        incrementedCurrentLow = 1;
                    }
                }
            }
            return (_currentHi - 1) * _capacity + (incrementedCurrentLow);
        }

        private long GetNextHi(string collectionName)
        {
            while (true)
            {
                try
                {
                    var update = new Expando();
                    update["$inc"] = new { ServerHi = 1 };

                    var hiLoKey = _db.GetCollection<NormHiLoKey>().FindAndModify(new { _id = collectionName }, update);
                    if (hiLoKey == null)
                    {
                        _db.GetCollection<NormHiLoKey>().Insert(new NormHiLoKey { CollectionName = collectionName, ServerHi = 2 });
                        return 1;
                    }

                    var newHi = hiLoKey.ServerHi;
                    return newHi;
                }
                catch (MongoException ex)
                {
                    if (!ex.Message.Contains("duplicate key"))
                        throw;
                }
            }
        }

        #region Nested type: HiLoKey

        private class NormHiLoKey
        {
            [MongoIdentifier]
            public string CollectionName { get; set; }
            public long ServerHi { get; set; }
        }

        #endregion
    }
}
