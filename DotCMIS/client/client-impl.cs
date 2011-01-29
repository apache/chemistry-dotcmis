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
using DotCMIS.Binding;
using DotCMIS.Data;
using DotCMIS.Exceptions;
using System.Threading;
using DotCMIS.Enums;
using DotCMIS.Data.Extensions;
using DotCMIS.Binding.Services;

namespace DotCMIS.Client
{
    /// <summary>
    /// Session factory implementation.
    /// </summary>
    public class SessionFactory : ISessionFactory
    {
        private SessionFactory()
        {
        }

        public static SessionFactory NewInstance()
        {
            return new SessionFactory();
        }

        public ISession CreateSession(IDictionary<string, string> parameters)
        {
            Session session = new Session(parameters);
            session.Connect();

            return session;
        }

        public IList<IRepository> GetRepositories(IDictionary<string, string> parameters)
        {
            ICmisBinding binding = CmisBindingHelper.CreateProvider(parameters);

            IList<IRepositoryInfo> repositoryInfos = binding.GetRepositoryService().GetRepositoryInfos(null);

            IList<IRepository> result = new List<IRepository>();
            foreach (IRepositoryInfo data in repositoryInfos)
            {
                result.Add(new Repository(data, parameters, this));
            }

            return result;
        }
    }

    /// <summary>
    /// Binding helper class.
    /// </summary>
    internal class CmisBindingHelper
    {
        public static ICmisBinding CreateProvider(IDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            if (!parameters.ContainsKey(SessionParameter.BindingType))
            {
                parameters[SessionParameter.BindingType] = BindingType.Custom;
            }

            string bt = parameters[SessionParameter.BindingType];
            switch (bt)
            {
                case BindingType.AtomPub:
                    return CreateAtomPubBinding(parameters);
                case BindingType.WebServices:
                    return CreateWebServiceBinding(parameters);
                case BindingType.Custom:
                    return CreateCustomBinding(parameters);
                default:
                    throw new CmisRuntimeException("Ambiguous session parameter: " + parameters);
            }
        }

        private static ICmisBinding CreateCustomBinding(IDictionary<string, string> parameters)
        {
            CmisBindingFactory factory = CmisBindingFactory.NewInstance();
            ICmisBinding binding = factory.CreateCmisBinding(parameters);

            return binding;
        }

        private static ICmisBinding CreateWebServiceBinding(IDictionary<string, string> parameters)
        {
            CmisBindingFactory factory = CmisBindingFactory.NewInstance();
            ICmisBinding binding = factory.CreateCmisWebServicesBinding(parameters);

            return binding;
        }

        private static ICmisBinding CreateAtomPubBinding(IDictionary<string, string> parameters)
        {
            CmisBindingFactory factory = CmisBindingFactory.NewInstance();
            ICmisBinding binding = factory.CreateCmisAtomPubBinding(parameters);

            return binding;
        }
    }

    /// <summary>
    /// Repository implementation.
    /// </summary>
    public class Repository : RepositoryInfo, IRepository
    {
        private IDictionary<string, string> parameters;
        private ISessionFactory sessionFactory;

        public Repository(IRepositoryInfo info, IDictionary<string, string> parameters, ISessionFactory sessionFactory)
            : base(info)
        {
            this.parameters = new Dictionary<string, string>(parameters);
            this.parameters[SessionParameter.RepositoryId] = Id;

            this.sessionFactory = sessionFactory;
        }

        public ISession CreateSession()
        {
            return sessionFactory.CreateSession(parameters);
        }
    }

    /// <summary>
    /// Session implementation.
    /// </summary>
    public class Session : ISession
    {
        protected static IOperationContext FallbackContext = new OperationContext(null, false, true, false, IncludeRelationshipsFlag.None, null, true, null, true, 100);

        protected IDictionary<string, string> parameters;
        private object sessionLock = new object();

        public ICmisBinding Binding { get; protected set; }
        public IRepositoryInfo RepositoryInfo { get; protected set; }
        public string RepositoryId { get { return RepositoryInfo.Id; } }

        public IObjectFactory ObjectFactory { get; protected set; }
        protected ICache Cache { get; set; }

        private IOperationContext context = FallbackContext;
        public IOperationContext DefaultContext
        {
            get
            {
                Lock();
                try
                {
                    return context;
                }
                finally
                {
                    Unlock();
                }
            }
            set
            {
                Lock();
                try
                {
                    context = (value == null ? FallbackContext : value);
                }
                finally
                {
                    Unlock();
                }
            }
        }

        public Session(IDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            this.parameters = parameters;

            ObjectFactory = CreateObjectFactory();
            Cache = CreateCache();
        }

        public void Connect()
        {
            Lock();
            try
            {
                Binding = CmisBindingHelper.CreateProvider(parameters);

                string repositoryId;
                if (!parameters.TryGetValue(SessionParameter.RepositoryId, out repositoryId))
                {
                    throw new ArgumentException("Repository Id is not set!");
                }

                RepositoryInfo = Binding.GetRepositoryService().GetRepositoryInfo(repositoryId, null);
            }
            finally
            {
                Unlock();
            }
        }

        protected ICache CreateCache()
        {
            try
            {
                string typeName;
                Type cacheType;

                if (parameters.TryGetValue(SessionParameter.CacheClass, out typeName))
                {
                    cacheType = Type.GetType(typeName);
                }
                else
                {
                    cacheType = typeof(NoCache);
                }

                object cacheObject = Activator.CreateInstance(cacheType);
                if (!(cacheObject is ICache))
                {
                    throw new Exception("Class does not implement ICache!");
                }

                ((ICache)cacheObject).Initialize(this, parameters);

                return (ICache)cacheObject;
            }
            catch (Exception e)
            {
                throw new ArgumentException("Unable to create cache: " + e, e);
            }
        }

        protected IObjectFactory CreateObjectFactory()
        {
            try
            {
                string ofName;
                Type ofType;

                if (parameters.TryGetValue(SessionParameter.ObjectFactoryClass, out ofName))
                {
                    ofType = Type.GetType(ofName);
                }
                else
                {
                    ofType = typeof(ObjectFactory);
                }

                object ofObject = Activator.CreateInstance(ofType);
                if (!(ofObject is IObjectFactory))
                {
                    throw new Exception("Class does not implement IObjectFactory!");
                }

                ((IObjectFactory)ofObject).Initialize(this, parameters);

                return (IObjectFactory)ofObject;
            }
            catch (Exception e)
            {
                throw new ArgumentException("Unable to create object factory: " + e, e);
            }
        }

        public void Clear()
        {
            Lock();
            try
            {
                Cache = CreateCache();
                Binding.ClearAllCaches();
            }
            finally
            {
                Unlock();
            }
        }

        // session context

        public IOperationContext CreateOperationContext()
        {
            return new OperationContext();
        }

        public IOperationContext CreateOperationContext(HashSet<string> filter, bool includeAcls, bool includeAllowableActions, bool includePolicies,
            IncludeRelationshipsFlag includeRelationships, HashSet<string> renditionFilter, bool includePathSegments, string orderBy,
            bool cacheEnabled, int maxItemsPerPage)
        {
            return new OperationContext(filter, includeAcls, includeAllowableActions, includePolicies, includeRelationships, renditionFilter,
                includePathSegments, orderBy, cacheEnabled, maxItemsPerPage);
        }

        public IObjectId CreateObjectId(string id)
        {
            return new ObjectId(id);
        }

        // types

        public IObjectType GetTypeDefinition(string typeId)
        {
            ITypeDefinition typeDefinition = Binding.GetRepositoryService().GetTypeDefinition(RepositoryId, typeId, null);
            return ObjectFactory.ConvertTypeDefinition(typeDefinition);
        }

        public IItemEnumerable<IObjectType> GetTypeChildren(string typeId, bool includePropertyDefinitions)
        {
            IRepositoryService repositoryService = Binding.GetRepositoryService();

            PageFetcher<IObjectType>.FetchPage fetchPageDelegate = delegate(long maxNumItems, long skipCount)
            {
                // fetch the data
                ITypeDefinitionList tdl = repositoryService.GetTypeChildren(RepositoryId, typeId, includePropertyDefinitions, maxNumItems, skipCount, null);

                // convert type definitions
                IList<IObjectType> page = new List<IObjectType>(tdl.List.Count);
                foreach (ITypeDefinition typeDefinition in tdl.List)
                {
                    page.Add(ObjectFactory.ConvertTypeDefinition(typeDefinition));
                }

                return new PageFetcher<IObjectType>.Page<IObjectType>(page, tdl.NumItems, tdl.HasMoreItems);
            };

            return new CollectionEnumerable<IObjectType>(new PageFetcher<IObjectType>(DefaultContext.MaxItemsPerPage, fetchPageDelegate));
        }

        public IList<ITree<IObjectType>> GetTypeDescendants(string typeId, int depth, bool includePropertyDefinitions)
        {
            IList<ITypeDefinitionContainer> descendants = Binding.GetRepositoryService().GetTypeDescendants(
            RepositoryId, typeId, depth, includePropertyDefinitions, null);

            return ConvertTypeDescendants(descendants);
        }

        private IList<ITree<IObjectType>> ConvertTypeDescendants(IList<ITypeDefinitionContainer> descendantsList)
        {
            IList<ITree<IObjectType>> result = new List<ITree<IObjectType>>();

            foreach (ITypeDefinitionContainer container in descendantsList)
            {
                Tree<IObjectType> tree = new Tree<IObjectType>();
                tree.Item = ObjectFactory.ConvertTypeDefinition(container.TypeDefinition);
                tree.Children = ConvertTypeDescendants(container.Children);

                result.Add(tree);
            }

            return result;
        }

        // navigation

        public IFolder GetRootFolder()
        {
            return GetRootFolder(DefaultContext);
        }

        public IFolder GetRootFolder(IOperationContext context)
        {
            ICmisObject rootFolder = GetObject(CreateObjectId(RepositoryInfo.RootFolderId), context);
            if (!(rootFolder is IFolder))
            {
                throw new CmisRuntimeException("Root folder object is not a folder!");
            }

            return (IFolder)rootFolder;
        }

        public IItemEnumerable<IDocument> GetCheckedOutDocs()
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IItemEnumerable<IDocument> GetCheckedOutDocs(IOperationContext context)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public ICmisObject GetObject(IObjectId objectId)
        {
            return GetObject(objectId, DefaultContext);
        }

        public ICmisObject GetObject(IObjectId objectId, IOperationContext context)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public ICmisObject GetObjectByPath(string path)
        {
            return GetObjectByPath(path, DefaultContext);
        }

        public ICmisObject GetObjectByPath(string path, IOperationContext context)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        // discovery

        public IItemEnumerable<IQueryResult> Query(string statement, bool searchAllVersions)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IItemEnumerable<IQueryResult> Query(string statement, bool searchAllVersions, IOperationContext context)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IChangeEvents GetContentChanges(string changeLogToken, bool includeProperties, long maxNumItems)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IChangeEvents GetContentChanges(string changeLogToken, bool includeProperties, long maxNumItems,
                IOperationContext context)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        // create

        public IObjectId CreateDocument(IDictionary<string, object> properties, IObjectId folderId, IContentStream contentStream,
                VersioningState? versioningState, IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreateDocument(IDictionary<string, object> properties, IObjectId folderId, IContentStream contentStream,
                VersioningState? versioningState)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreateDocumentFromSource(IObjectId source, IDictionary<string, object> properties, IObjectId folderId,
                VersioningState? versioningState, IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreateDocumentFromSource(IObjectId source, IDictionary<string, object> properties, IObjectId folderId,
                VersioningState? versioningState)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreateFolder(IDictionary<string, object> properties, IObjectId folderId, IList<IPolicy> policies, IList<IAce> addAces,
                IList<IAce> removeAces)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreateFolder(IDictionary<string, object> properties, IObjectId folderId)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreatePolicy(IDictionary<string, object> properties, IObjectId folderId, IList<IPolicy> policies, IList<IAce> addAces,
                IList<IAce> removeAces)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreatePolicy(IDictionary<string, object> properties, IObjectId folderId)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreateRelationship(IDictionary<string, object> properties, IList<IPolicy> policies, IList<IAce> addAces,
                IList<IAce> removeAces)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IObjectId CreateRelationship(IDictionary<string, object> properties)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IItemEnumerable<IRelationship> GetRelationships(IObjectId objectId, bool includeSubRelationshipTypes,
                RelationshipDirection? relationshipDirection, IObjectType type, IOperationContext context)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        // permissions

        public IAcl GetAcl(IObjectId objectId, bool onlyBasicPermissions)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public IAcl ApplyAcl(IObjectId objectId, IList<IAce> addAces, IList<IAce> removeAces, AclPropagation? aclPropagation)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public void ApplyPolicy(IObjectId objectId, IObjectId policyIds)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        public void RemovePolicy(IObjectId objectId, IObjectId policyIds)
        { throw new CmisNotSupportedException("Client not implemented!"); }

        protected void Lock()
        {
            Monitor.Enter(sessionLock);
        }

        protected void Unlock()
        {
            Monitor.Exit(sessionLock);
        }
    }
}
