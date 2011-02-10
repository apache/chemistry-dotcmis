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
using DotCMIS.Binding;
using DotCMIS.Data;
using DotCMIS.Data.Extensions;
using DotCMIS.Enums;
using System.IO;

namespace DotCMIS.Client
{
    /// <summary>
    /// Session factory interface.
    /// </summary>
    public interface ISessionFactory
    {
        /// <summary>
        /// Creates a new session with the given parameters and connects to the repository.
        /// </summary>
        /// <param name="parameters">the session parameters</param>
        /// <returns>the newly created session</returns>
        /// <example>
        /// Connect to an AtomPub CMIS endpoint:
        /// <code>
        /// Dictionary&lt;string, string&gt; parameters = new Dictionary&lt;string, string&gt;();
        /// 
        /// parameters[SessionParameter.BindingType] = BindingType.AtomPub;
        /// parameters[SessionParameter.AtomPubUrl] = "http://localhost/cmis/atom";
        /// parameters[SessionParameter.Password] = "admin";
        /// parameters[SessionParameter.User] = "admin";
        /// parameters[SessionParameter.RepositoryId] = "1234-abcd-5678";
        ///
        /// SessionFactory factory = SessionFactory.NewInstance();
        /// ISession session = factory.CreateSession(parameters);
        /// </code>
        /// 
        /// Connect to a Web Services CMIS endpoint:
        /// <code>
        /// Dictionary&lt;string, string&gt; parameters = new Dictionary&lt;string, string&gt;();
        /// 
        /// string baseUrlWS = "https://localhost:443/cmis/ws";
        ///
        /// parameters[SessionParameter.BindingType] = BindingType.WebServices;
        /// parameters[SessionParameter.WebServicesRepositoryService] = baseUrlWS + "/RepositoryService?wsdl";
        /// parameters[SessionParameter.WebServicesAclService] = baseUrlWS + "/AclService?wsdl";
        /// parameters[SessionParameter.WebServicesDiscoveryService] = baseUrlWS + "/DiscoveryService?wsdl";
        /// parameters[SessionParameter.WebServicesMultifilingService] = baseUrlWS + "/MultifilingService?wsdl";
        /// parameters[SessionParameter.WebServicesNavigationService] = baseUrlWS + "/NavigationService?wsdl";
        /// parameters[SessionParameter.WebServicesObjectService] = baseUrlWS + "/ObjectService?wsdl";
        /// parameters[SessionParameter.WebServicesPolicyService] = baseUrlWS + "/PolicyService?wsdl";
        /// parameters[SessionParameter.WebServicesRelationshipService] = baseUrlWS + "/RelationshipService?wsdl";
        /// parameters[SessionParameter.WebServicesVersioningService] = baseUrlWS + "/VersioningService?wsdl";
        /// parameters[SessionParameter.RepositoryId] = "1234-abcd-5678"
        /// parameters[SessionParameter.User] = "admin";
        /// parameters[SessionParameter.Password] = "admin";
        ///
        /// SessionFactory factory = SessionFactory.NewInstance();
        /// ISession session = factory.CreateSession(parameters);
        /// </code>
        /// </example>
        /// <seealso cref="DotCMIS.SessionParameter"/>
        ISession CreateSession(IDictionary<string, string> parameters);

        /// <summary>
        /// Gets all repository available at the specified endpoint.
        /// </summary>
        /// <param name="parameters">the session parameters</param>
        /// <returns>a list of all available repositories</returns>
        /// <seealso cref="DotCMIS.SessionParameter"/>
        IList<IRepository> GetRepositories(IDictionary<string, string> parameters);
    }

    /// <summary>
    /// Repository interface.
    /// </summary>
    public interface IRepository : IRepositoryInfo
    {
        /// <summary>
        /// Creates a session for this repository.
        /// </summary>
        ISession CreateSession();
    }

    /// <summary>
    /// Session interface.
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// Clears all caches.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the CMIS binding object.
        /// </summary>
        ICmisBinding Binding { get; }

        /// <summary>
        /// Gets the default operation context.
        /// </summary>
        IOperationContext DefaultContext { get; set; }

        /// <summary>
        /// Creates a new operation context object.
        /// </summary>
        IOperationContext CreateOperationContext();

        /// <summary>
        /// Creates a new operation context object with the given parameters.
        /// </summary>
        IOperationContext CreateOperationContext(HashSet<string> filter, bool includeAcls, bool includeAllowableActions, bool includePolicies,
            IncludeRelationshipsFlag includeRelationships, HashSet<string> renditionFilter, bool includePathSegments, string orderBy,
            bool cacheEnabled, int maxItemsPerPage);

        /// <summary>
        /// Creates a new <see cref="DotCMIS.Client.IObjectId"/> with the giveb id.
        /// </summary>
        IObjectId CreateObjectId(string id);

        /// <summary>
        /// Gets the CMIS repositoy info.
        /// </summary>
        IRepositoryInfo RepositoryInfo { get; }

        /// <summary>
        /// Gets the internal object factory. 
        /// </summary>
        IObjectFactory ObjectFactory { get; }

        // types

        IObjectType GetTypeDefinition(string typeId);
        IItemEnumerable<IObjectType> GetTypeChildren(string typeId, bool includePropertyDefinitions);
        IList<ITree<IObjectType>> GetTypeDescendants(string typeId, int depth, bool includePropertyDefinitions);

        // navigation

        IFolder GetRootFolder();
        IFolder GetRootFolder(IOperationContext context);
        IItemEnumerable<IDocument> GetCheckedOutDocs();
        IItemEnumerable<IDocument> GetCheckedOutDocs(IOperationContext context);
        ICmisObject GetObject(IObjectId objectId);
        ICmisObject GetObject(IObjectId objectId, IOperationContext context);
        ICmisObject GetObjectByPath(string path);
        ICmisObject GetObjectByPath(string path, IOperationContext context);

        // discovery

        IItemEnumerable<IQueryResult> Query(string statement, bool searchAllVersions);
        IItemEnumerable<IQueryResult> Query(string statement, bool searchAllVersions, IOperationContext context);
        IChangeEvents GetContentChanges(string changeLogToken, bool includeProperties, long maxNumItems);
        IChangeEvents GetContentChanges(string changeLogToken, bool includeProperties, long maxNumItems,
                IOperationContext context);

        // create

        IObjectId CreateDocument(IDictionary<string, object> properties, IObjectId folderId, IContentStream contentStream,
                VersioningState? versioningState, IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces);
        IObjectId CreateDocument(IDictionary<string, object> properties, IObjectId folderId, IContentStream contentStream,
                VersioningState? versioningState);
        IObjectId CreateDocumentFromSource(IObjectId source, IDictionary<string, object> properties, IObjectId folderId,
                VersioningState? versioningState, IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces);
        IObjectId CreateDocumentFromSource(IObjectId source, IDictionary<string, object> properties, IObjectId folderId,
                VersioningState? versioningState);
        IObjectId CreateFolder(IDictionary<string, object> properties, IObjectId folderId, IList<IPolicy> policies, IList<IAce> addAces,
                IList<IAce> removeAces);
        IObjectId CreateFolder(IDictionary<string, object> properties, IObjectId folderId);
        IObjectId CreatePolicy(IDictionary<string, object> properties, IObjectId folderId, IList<IPolicy> policies, IList<IAce> addAces,
                IList<IAce> removeAces);
        IObjectId CreatePolicy(IDictionary<string, object> properties, IObjectId folderId);
        IObjectId CreateRelationship(IDictionary<string, object> properties, IList<IPolicy> policies, IList<IAce> addAces,
                IList<IAce> removeAces);
        IObjectId CreateRelationship(IDictionary<string, object> properties);

        IItemEnumerable<IRelationship> GetRelationships(IObjectId objectId, bool includeSubRelationshipTypes,
                RelationshipDirection? relationshipDirection, IObjectType type, IOperationContext context);

        // permissions

        IAcl GetAcl(IObjectId objectId, bool onlyBasicPermissions);
        IAcl ApplyAcl(IObjectId objectId, IList<IAce> addAces, IList<IAce> removeAces, AclPropagation? aclPropagation);
        void ApplyPolicy(IObjectId objectId, params IObjectId[] policyIds);
        void RemovePolicy(IObjectId objectId, params IObjectId[] policyIds);
    }

    public interface IObjectFactory
    {
        void Initialize(ISession session, IDictionary<string, string> parameters);

        // ACL and ACE
        IAcl ConvertAces(IList<IAce> aces);
        IAcl CreateAcl(IList<IAce> aces);
        IAce CreateAce(string principal, IList<string> permissions);

        // policies
        IList<string> ConvertPolicies(IList<IPolicy> policies);

        // renditions
        IRendition ConvertRendition(string objectId, IRenditionData rendition);

        // content stream
        IContentStream CreateContentStream(string filename, long length, string mimetype, Stream stream);

        // types
        IObjectType ConvertTypeDefinition(ITypeDefinition typeDefinition);
        IObjectType GetTypeFromObjectData(IObjectData objectData);

        // properties
        IProperty CreateProperty<T>(IPropertyDefinition type, IList<T> values);
        IDictionary<string, IProperty> ConvertProperties(IObjectType objectType, IProperties properties);
        IProperties ConvertProperties(IDictionary<string, object> properties, IObjectType type, HashSet<Updatability> updatabilityFilter);
        IList<IPropertyData> ConvertQueryProperties(IProperties properties);

        // objects
        ICmisObject ConvertObject(IObjectData objectData, IOperationContext context);
        IQueryResult ConvertQueryResult(IObjectData objectData);
        IChangeEvent ConvertChangeEvent(IObjectData objectData);
        IChangeEvents ConvertChangeEvents(string changeLogToken, IObjectList objectList);
    }

    /// <summary>
    /// Operation context interface.
    /// </summary>
    public interface IOperationContext
    {
        HashSet<string> Filter { get; set; }
        string FilterString { get; set; }
        bool IncludeAllowableActions { get; set; }
        bool IncludeAcls { get; set; }
        IncludeRelationshipsFlag? IncludeRelationships { get; set; }
        bool IncludePolicies { get; set; }
        HashSet<string> RenditionFilter { get; set; }
        string RenditionFilterString { get; set; }
        bool IncludePathSegments { get; set; }
        string OrderBy { get; set; }
        bool CacheEnabled { get; set; }
        string CacheKey { get; }
        int MaxItemsPerPage { get; set; }
    }

    public interface ITree<T>
    {
        T Item { get; }
        IList<ITree<T>> Children { get; }
    }

    /// <summary>
    /// Base interface for all CMIS types.
    /// </summary>
    public interface IObjectType : ITypeDefinition
    {
        bool IsBaseType { get; }
        IObjectType GetBaseType();
        IObjectType GetParentType();
        IItemEnumerable<IObjectType> GetChildren();
        IList<ITree<IObjectType>> GetDescendants(int depth);
    }

    /// <summary>
    /// Document type interface.
    /// </summary>
    public interface IDocumentType : IObjectType
    {
        bool? IsVersionable { get; }
        ContentStreamAllowed? ContentStreamAllowed { get; }
    }

    /// <summary>
    /// Folder type interface.
    /// </summary>
    public interface IFolderType : IObjectType
    {
    }

    /// <summary>
    /// Relationship type interface.
    /// </summary>
    public interface IRelationshipType : IObjectType
    {
        IList<IObjectType> GetAllowedSourceTypes { get; }
        IList<IObjectType> GetAllowedTargetTypes { get; }
    }

    /// <summary>
    /// Policy type interface.
    /// </summary>
    public interface IPolicyType : IObjectType
    {
    }

    public interface IItemEnumerable<T> : IEnumerable<T>
    {
        IItemEnumerable<T> SkipTo(long position);
        IItemEnumerable<T> GetPage();
        IItemEnumerable<T> GetPage(int maxNumItems);
        long PageNumItems { get; }
        bool HasMoreItems { get; }
        long TotalNumItems { get; }
    }

    public interface IObjectId
    {
        /// <summary>
        /// Gets the object id.
        /// </summary>
        string Id { get; }
    }

    public interface IRendition : IRenditionData
    {
        IDocument GetRenditionDocument();
        IDocument GetRenditionDocument(IOperationContext context);
        IContentStream GetContentStream();
    }

    /// <summary>
    /// Property interface.
    /// </summary>
    public interface IProperty
    {
        string Id { get; }
        string LocalName { get; }
        string DisplayName { get; }
        string QueryName { get; }
        bool IsMultiValued { get; }
        PropertyType? PropertyType { get; }
        IPropertyDefinition PropertyDefinition { get; }
        object Value { get; }
        IList<object> Values { get; }
        object FirstValue { get; }
        string ValueAsString { get; }
        string ValuesAsString { get; }
    }

    /// <summary>
    /// Collection of common CMIS properties.
    /// </summary>
    public interface ICmisObjectProperties
    {
        /// <summary>
        /// Gets a list of all available CMIS properties.
        /// </summary>
        IList<IProperty> Properties { get; }

        /// <summary>
        /// available
        /// </summary>
        /// <param name="propertyId">the property id</param>
        /// <returns>the property or <c>null</c> if the property is not available</returns>
        IProperty this[string propertyId] { get; }

        /// <summary>
        /// Gets the value of the requested property.
        /// </summary>
        /// <param name="propertyId">the property id</param>
        /// <returns>the property value or <c>null</c> if the property is not available or not set</returns>
        object GetPropertyValue(string propertyId);

        /// <summary>
        /// Gets the name of this CMIS object (CMIS property <c>cmis:name</c>).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the user who created this CMIS object (CMIS property <c>cmis:createdBy</c>).
        /// </summary>
        string CreatedBy { get; }

        /// <summary>
        /// Gets the timestamp when this CMIS object has been created (CMIS property <c>cmis:creationDate</c>).
        /// </summary>
        DateTime? CreationDate { get; }

        /// <summary>
        /// Gets the user who modified this CMIS object (CMIS property <c>cmis:lastModifiedBy</c>).
        /// </summary>
        string LastModifiedBy { get; }

        /// <summary>
        /// Gets the timestamp when this CMIS object has been modified (CMIS property <c>cmis:lastModificationDate</c>).
        /// </summary>
        DateTime? LastModificationDate { get; }

        /// <summary>
        /// Gets the id of the base type of this CMIS object (CMIS property <c>cmis:baseTypeId</c>).
        /// </summary>
        BaseTypeId BaseTypeId { get; }

        /// <summary>
        /// Gets the base type of this CMIS object (object type identified by <c>cmis:baseTypeId</c>).
        /// </summary>
        IObjectType BaseType { get; }

        /// <summary>
        /// Gets the type of this CMIS object (object type identified by <c>cmis:objectTypeId</c>).
        /// </summary>
        IObjectType ObjectType { get; }

        /// <summary>
        /// Gets the change token (CMIS property <c>cmis:changeToken</c>).
        /// </summary>
        string ChangeToken { get; }
    }

    public enum ExtensionLevel
    {
        Object, Properties, AllowableActions, Acl, Policies, ChangeEvent
    }

    /// <summary>
    /// Base interface for all CMIS objects.
    /// </summary>
    public interface ICmisObject : IObjectId, ICmisObjectProperties
    {
        // object
        IAllowableActions AllowableActions { get; }
        IList<IRelationship> Relationships { get; }
        IAcl Acl { get; }

        // object service
        void Delete(bool allVersions);
        ICmisObject UpdateProperties(IDictionary<string, object> properties);
        IObjectId UpdateProperties(IDictionary<string, object> properties, bool refresh);

        // renditions
        IList<IRendition> Renditions { get; }

        // policy service
        void ApplyPolicy(params IObjectId[] policyId);
        void RemovePolicy(params IObjectId[] policyId);
        IList<IPolicy> Policies { get; }

        // ACL service
        IAcl ApplyAcl(IList<IAce> AddAces, IList<IAce> removeAces, AclPropagation? aclPropagation);
        IAcl AddAcl(IList<IAce> AddAces, AclPropagation? aclPropagation);
        IAcl RemoveAcl(IList<IAce> RemoveAces, AclPropagation? aclPropagation);

        // extensions
        IList<ICmisExtensionElement> GetExtensions(ExtensionLevel level);

        DateTime RefreshTimestamp { get; }
        void Refresh();
        void RefreshIfOld(long durationInMillis);
    }

    /// <summary>
    /// Base interface for all fileable CMIS objects.
    /// </summary>
    public interface IFileableCmisObject : ICmisObject
    {
        // object service
        IFileableCmisObject Move(IObjectId sourceFolderId, IObjectId targetFolderId);

        // navigation service
        IList<IFolder> Parents { get; }
        IList<string> Paths { get; }

        // multifiling service
        void AddToFolder(IObjectId folderId, bool allVersions);
        void RemoveFromFolder(IObjectId folderId);
    }

    /// <summary>
    /// Document properties.
    /// </summary>
    public interface IDocumentProperties
    {
        bool? IsImmutable { get; }
        bool? IsLatestVersion { get; }
        bool? IsMajorVersion { get; }
        bool? IsLatestMajorVersion { get; }
        string VersionLabel { get; }
        string VersionSeriesId { get; }
        bool? IsVersionSeriesCheckedOut { get; }
        string VersionSeriesCheckedOutBy { get; }
        string VersionSeriesCheckedOutId { get; }
        string CheckinComment { get; }
        long? ContentStreamLength { get; }
        string ContentStreamMimeType { get; }
        string ContentStreamFileName { get; }
        string ContentStreamId { get; }
    }

    /// <summary>
    /// Document interface.
    /// </summary>
    public interface IDocument : IFileableCmisObject, IDocumentProperties
    {
        void DeleteAllVersions();
        IContentStream GetContentStream();
        IContentStream GetContentStream(string streamId);
        IDocument SetContentStream(IContentStream contentStream, bool overwrite);
        IObjectId SetContentStream(IContentStream contentStream, bool overwrite, bool refresh);
        IDocument DeleteContentStream();
        IObjectId DeleteContentStream(bool refresh);
        IObjectId CheckOut();
        void CancelCheckOut();
        IObjectId CheckIn(bool major, IDictionary<string, object> properties, IContentStream contentStream, string checkinComment,
                IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces);
        IObjectId CheckIn(bool major, IDictionary<string, object> properties, IContentStream contentStream, string checkinComment);
        IDocument GetObjectOfLatestVersion(bool major);
        IDocument GetObjectOfLatestVersion(bool major, IOperationContext context);
        IList<IDocument> GetAllVersions();
        IList<IDocument> GetAllVersions(IOperationContext context);
        IDocument Copy(IObjectId targetFolderId);
        IDocument Copy(IObjectId targetFolderId, IDictionary<string, object> properties, VersioningState? versioningState,
                IList<IPolicy> policies, IList<IAce> addACEs, IList<IAce> removeACEs, IOperationContext context);
    }

    /// <summary>
    /// Folder properties.
    /// </summary>
    public interface IFolderProperties
    {
        IList<IObjectType> AllowedChildObjectTypes { get; }
    }

    /// <summary>
    /// Folder interface.
    /// </summary>
    public interface IFolder : IFileableCmisObject, IFolderProperties
    {
        IDocument CreateDocument(IDictionary<string, object> properties, IContentStream contentStream, VersioningState? versioningState,
                IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces, IOperationContext context);
        IDocument CreateDocument(IDictionary<string, object> properties, IContentStream contentStream, VersioningState? versioningState);
        IDocument CreateDocumentFromSource(IObjectId source, IDictionary<string, object> properties, VersioningState? versioningState,
                IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces, IOperationContext context);
        IDocument CreateDocumentFromSource(IObjectId source, IDictionary<string, object> properties, VersioningState? versioningState);
        IFolder CreateFolder(IDictionary<string, object> properties, IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces,
                IOperationContext context);
        IFolder CreateFolder(IDictionary<string, object> properties);
        IPolicy CreatePolicy(IDictionary<string, object> properties, IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces,
                IOperationContext context);
        IPolicy CreatePolicy(IDictionary<string, object> properties);
        IList<string> DeleteTree(bool allversions, UnfileObject? unfile, bool continueOnFailure);
        IList<ITree<IFileableCmisObject>> GetFolderTree(int depth);
        IList<ITree<IFileableCmisObject>> GetFolderTree(int depth, IOperationContext context);
        IList<ITree<IFileableCmisObject>> GetDescendants(int depth);
        IList<ITree<IFileableCmisObject>> GetDescendants(int depth, IOperationContext context);
        IItemEnumerable<ICmisObject> GetChildren();
        IItemEnumerable<ICmisObject> GetChildren(IOperationContext context);
        bool IsRootFolder { get; }
        IFolder FolderParent { get; }
        string Path { get; }
        IItemEnumerable<IDocument> GetCheckedOutDocs();
        IItemEnumerable<IDocument> GetCheckedOutDocs(IOperationContext context);
    }

    /// <summary>
    /// Policy properties.
    /// </summary>
    public interface IPolicyProperties
    {
        string PolicyText { get; }
    }

    /// <summary>
    /// Policy interface.
    /// </summary>
    public interface IPolicy : IFileableCmisObject, IPolicyProperties
    {
    }

    /// <summary>
    /// Relationship properties.
    /// </summary>
    public interface IRelationshipProperties
    {
        IObjectId SourceId { get; }
        IObjectId TargetId { get; }
    }

    /// <summary>
    /// Relationship interface.
    /// </summary>
    public interface IRelationship : ICmisObject, IRelationshipProperties
    {
        ICmisObject GetSource();
        ICmisObject GetSource(IOperationContext context);
        ICmisObject GetTarget();
        ICmisObject GetTarget(IOperationContext context);
    }

    /// <summary>
    /// Query result.
    /// </summary>
    public interface IQueryResult
    {
        IPropertyData this[string queryName] { get; }
        IList<IPropertyData> Properties { get; }
        IPropertyData GetPropertyById(string propertyId);
        object GetPropertyValueByQueryName(string queryName);
        object GetPropertyValueById(string propertyId);
        IList<object> GetPropertyMultivalueByQueryName(string queryName);
        IList<object> GetPropertyMultivalueById(string propertyId);
        IAllowableActions AllowableActions { get; }
        IList<IRelationship> Relationships { get; }
        IList<IRendition> Renditions { get; }
    }

    public interface IChangeEvent : IChangeEventInfo
    {
        string ObjectId { get; }
        IDictionary<string, IList<object>> Properties { get; }
        IList<string> PolicyIds { get; }
        IAcl Acl { get; }
    }

    public interface IChangeEvents
    {
        string LatestChangeLogToken { get; }
        IList<IChangeEvent> ChangeEventList { get; }
        bool? HasMoreItems { get; }
        long? TotalNumItems { get; }
    }
}
