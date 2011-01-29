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
    public interface ISessionFactory
    {
        ISession CreateSession(IDictionary<string, string> parameters);
        IList<IRepository> GetRepositories(IDictionary<string, string> parameters);
    }

    public interface IRepository : IRepositoryInfo
    {
        ISession CreateSession();
    }

    public interface ISession
    {
        void Clear();

        // session context

        ICmisBinding Binding { get; }

        IOperationContext DefaultContext { get; set; }
        IOperationContext CreateOperationContext();
        IOperationContext CreateOperationContext(HashSet<string> filter, bool includeAcls, bool includeAllowableActions, bool includePolicies,
            IncludeRelationshipsFlag includeRelationships, HashSet<string> renditionFilter, bool includePathSegments, string orderBy,
            bool cacheEnabled, int maxItemsPerPage);
        IObjectId CreateObjectId(string id);

        // services

        IRepositoryInfo RepositoryInfo { get; }
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
        void ApplyPolicy(IObjectId objectId, IObjectId policyIds);
        void RemovePolicy(IObjectId objectId, IObjectId policyIds);
    }

    public interface IObjectFactory
    {
        void Initialize(ISession session, IDictionary<string, string> parameters);

        // ACL and ACE
        IAcl ConvertAces(IList<IAce> aces);
        IAcl CreateAcl(IList<IAce> aces);
        IAce CreateAce(string principal, List<string> permissions);

        // policies
        IList<string> ConvertPolicies(IList<IPolicy> policies);

        // renditions
        IRendition ConvertRendition(string objectId, IRenditionData rendition);

        // content stream
        IContentStream CreateContentStream(string filename, long length, string mimetype, Stream stream);
        IContentStream ConvertContentStream(IContentStream contentStream);

        // types
        IObjectType ConvertTypeDefinition(ITypeDefinition typeDefinition);
        IObjectType GetTypeFromObjectData(IObjectData objectData);

        // properties
        IProperty CreateProperty(IPropertyDefinition type, IList<object> values);
        IDictionary<string, IProperty> ConvertProperties(IObjectType objectType, IProperties properties);
        IProperties ConvertProperties(IDictionary<string, object> properties, IObjectType type, HashSet<Updatability> updatabilityFilter);
        IList<IPropertyData> ConvertQueryProperties(IProperties properties);

        // objects
        ICmisObject ConvertObject(IObjectData objectData, IOperationContext context);
        IQueryResult ConvertQueryResult(IObjectData objectData);
        IChangeEvent ConvertChangeEvent(IObjectData objectData);
        IChangeEvents ConvertChangeEvents(String changeLogToken, IObjectList objectList);
    }

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

    public interface IObjectType : ITypeDefinition
    {
        bool IsBaseType { get; }
        IObjectType GetBaseType();
        IObjectType GetParentType();
        IItemEnumerable<IObjectType> GetChildren();
        IList<ITree<IObjectType>> GetDescendants(int depth);
    }

    public interface IDocumentType : IObjectType
    {
        bool? IsVersionable { get; }
        ContentStreamAllowed? ContentStreamAllowed { get; }
    }

    public interface IFolderType : IObjectType
    {
    }

    public interface IRelationshipType : IObjectType
    {
        IList<IObjectType> GetAllowedSourceTypes { get; }
        IList<IObjectType> GetAllowedTargetTypes { get; }
    }

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
        string Id { get; }
    }

    public interface IRendition : IRenditionData
    {
        IDocument GetRenditionDocument();
        IDocument GetRenditionDocument(IOperationContext context);
        IContentStream GetContentStream();
    }

    public interface IProperty : IPropertyData
    {
        bool IsMultiValued { get; }
        PropertyType PropertyType { get; }
        PropertyDefinition PropertyDefinition { get; }
        object Value { get; }
        string ValueAsString { get; }
        string ValuesAsString { get; }
    }

    public interface ICmisObjectProperties
    {
        IList<IProperty> Properties { get; }
        IProperty this[string propertyId] { get; }
        object GetPropertyValue(string propertyId);

        // convenience accessors
        string Name { get; }
        string CreatedBy { get; }
        DateTime? CreationDate { get; }
        string LastModifiedBy { get; }
        DateTime? LastModificationDate { get; }
        BaseTypeId BaseTypeId { get; }
        IObjectType BaseType { get; }
        IObjectType Type { get; }
        string ChangeToken { get; }
    }

    public enum ExtensionLevel
    {

        Object, Properties, AllowableActions, Acl, Policies, ChangeEvent
    }

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
        void ApplyPolicy(IObjectId policyId);
        void RemovePolicy(IObjectId policyId);
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

    public interface IFileableCmisObject : ICmisObject
    {
        // object service
        IFileableCmisObject Move(IObjectId sourceFolderId, IObjectId targetFolderId);

        // navigation service
        IList<IFolder> GetParents();
        IList<string> GetPaths();

        // multifiling service
        void AddToFolder(IObjectId folderId, bool allVersions);
        void RemoveFromFolder(IObjectId folderId);
    }

    public interface IDocumentProperties
    {
        bool? IsImmutable { get; }
        bool? IsLatestVersion { get; }
        bool? IsMajorVersion { get; }
        bool? IsLatestMajorVersion { get; }
        string VersionLabel { get; }
        string VersionSeriesId { get; }
        bool? VersionSeriesCheckedOut { get; }
        string VersionSeriesCheckedOutBy { get; }
        string VersionSeriesCheckedOutId { get; }
        string CheckinComment { get; }
        long ContentStreamLength { get; }
        string ContentStreamMimeType { get; }
        string ContentStreamFileName { get; }
        string ContentStreamId { get; }
    }

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
        IObjectId checkIn(bool major, IDictionary<string, object> properties, IContentStream contentStream, string checkinComment);
        IDocument GetObjectOfLatestVersion(bool major);
        IDocument GetObjectOfLatestVersion(bool major, IOperationContext context);
        IList<IDocument> GetAllVersions();
        IList<IDocument> GetAllVersions(IOperationContext context);
        IDocument Copy(IObjectId targetFolderId);
        IDocument Copy(IObjectId targetFolderId, IDictionary<string, object> properties, VersioningState? versioningState,
                IList<IPolicy> policies, IList<IAce> addACEs, IList<IAce> removeACEs, IOperationContext context);
    }

    public interface IFolderProperties
    {
        IList<IObjectType> AllowedChildObjectTypes { get; }
    }

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
        IPolicy CreatePolicy(IDictionary<string, object> properties, List<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces,
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

    public interface IPolicyProperties
    {
        string PolicyText { get; }
    }

    public interface IPolicy : IFileableCmisObject, IPolicyProperties
    {
    }

    public interface IRelationshipProperties
    {
        IObjectId SourceId { get; }
        IObjectId TargetId { get; }
    }

    public interface IRelationship : ICmisObject, IRelationshipProperties
    {
        ICmisObject GetSource();
        ICmisObject GetSource(IOperationContext context);
        ICmisObject GetTarget();
        ICmisObject GetTarget(IOperationContext context);
    }

    public interface IQueryResult
    {
    }

    public interface IChangeEvent
    {
    }

    public interface IChangeEvents
    {
    }
}
