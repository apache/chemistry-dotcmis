/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotCMIS.Client.Impl.Cache
{
    /// <summary>
    /// Client cache interface.
    /// </summary>
    public interface ICache
    {
        void Initialize(ISession session, IDictionary<string, string> parameters);
        bool ContainsId(string objectId, string cacheKey);
        bool ContainsPath(string path, string cacheKey);
        void Put(ICmisObject cmisObject, string cacheKey);
        void PutPath(string path, ICmisObject cmisObject, string cacheKey);
        ICmisObject GetById(string objectId, string cacheKey);
        ICmisObject GetByPath(string path, string cacheKey);
        void Clear();
        int CacheSize { get; }
    }

    /// <summary>
    /// Cache implementation that doesn't cache.
    /// </summary>
    public class NoCache : ICache
    {
        public void Initialize(ISession session, IDictionary<string, string> parameters) { }
        public bool ContainsId(string objectId, string cacheKey) { return false; }
        public bool ContainsPath(string path, string cacheKey) { return false; }
        public void Put(ICmisObject cmisObject, string cacheKey) { }
        public void PutPath(string path, ICmisObject cmisObject, string cacheKey) { }
        public ICmisObject GetById(string objectId, string cacheKey) { return null; }
        public ICmisObject GetByPath(string path, string cacheKey) { return null; }
        public void Clear() { }
        public int CacheSize { get { return 0; } }
    }
}
