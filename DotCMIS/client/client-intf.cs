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
using DotCMIS.Data;
using DotCMIS.Enums;
using DotCMIS.Data.Extensions;

namespace DotCMIS.Client
{
    public interface IOperationContext { }

    public interface ITree<T>
    {
        T Item { get; }
        IList<ITree<T>> GetChildren();
    }

    public interface IObjectType : ITypeDefinition
    {
        bool IsBaseType { get; }
        IObjectType BaseType { get; }
        IObjectType ParentType { get; }
        IItemIterable<IObjectType> GetChildren();
        IList<ITree<IObjectType>> GetDescendants(int depth);
    }

    public interface IItemIterable<T>
    {
        IItemIterable<T> SkipTo(long position);
        IItemIterable<T> GetPage();
        IItemIterable<T> GetPage(int maxNumItems);
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
        V getValue<V>();
        string GetValueAsString();
        string getValuesAsString();
    }

    public interface ICmisObjectProperties
    {
        IList<IProperty> Properties { get; }
        IProperty GetProperty(string id);
        T getPropertyValue<T>(string id);

        // convenience accessors
        string Name { get; }
        string CreatedBy { get; }
        DateTime CreationDate { get; }
        string LastModifiedBy { get; }
        DateTime LastModificationDate { get; }
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
        IAllowableActions getAllowableActions();
        IList<IRelationship> getRelationships();
        IAcl getAcl();

        // object service
        void delete(bool allVersions);
        ICmisObject updateProperties(IDictionary<string, object> properties);
        IObjectId updateProperties(IDictionary<string, object> properties, bool refresh);

        // renditions
        IList<IRendition> getRenditions();

        // policy service
        void applyPolicy(IObjectId policyId);
        void removePolicy(IObjectId policyIds);
        IList<IPolicy> getPolicies();

        // ACL service
        IAcl applyAcl(IList<Ace> addAces, IList<Ace> removeAces, AclPropagation? aclPropagation);
        IAcl addAcl(IList<Ace> addAces, AclPropagation? aclPropagation);
        IAcl removeAcl(IList<Ace> removeAces, AclPropagation? aclPropagation);

        // extensions
        IList<ICmisExtensionElement> getExtensions(ExtensionLevel level);

        long getRefreshTimestamp();
        void refresh();
        void refreshIfOld(long durationInMillis);
    }

    public interface IFileableCmisObject : ICmisObject
    {
        // object service
        IFileableCmisObject move(IObjectId sourceFolderId, IObjectId targetFolderId);

        // navigation service
        IList<IFolder> GetParents();
        IList<string> GetPaths();

        // multifiling service
        void addToFolder(IObjectId folderId, bool allVersions);
        void removeFromFolder(IObjectId folderId);
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
        IDocument createDocument(IDictionary<string, object> properties, IContentStream contentStream, VersioningState? versioningState,
                IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces, IOperationContext context);
        IDocument createDocument(IDictionary<string, object> properties, IContentStream contentStream, VersioningState? versioningState);
        IDocument createDocumentFromSource(IObjectId source, IDictionary<string, object> properties, VersioningState? versioningState,
                IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces, IOperationContext context);
        IDocument createDocumentFromSource(IObjectId source, IDictionary<string, object> properties, VersioningState? versioningState);
        IFolder createFolder(IDictionary<string, object> properties, IList<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces,
                IOperationContext context);
        IFolder createFolder(IDictionary<string, object> properties);
        IPolicy createPolicy(IDictionary<string, object> properties, List<IPolicy> policies, IList<IAce> addAces, IList<IAce> removeAces,
                IOperationContext context);
        IPolicy createPolicy(IDictionary<string, object> properties);
        IList<string> deleteTree(bool allversions, UnfileObject? unfile, bool continueOnFailure);
        IList<ITree<IFileableCmisObject>> GetFolderTree(int depth);
        IList<ITree<IFileableCmisObject>> GetFolderTree(int depth, IOperationContext context);
        IList<ITree<IFileableCmisObject>> GetDescendants(int depth);
        IList<ITree<IFileableCmisObject>> GetDescendants(int depth, IOperationContext context);
        IItemIterable<ICmisObject> GetChildren();
        IItemIterable<ICmisObject> GetChildren(IOperationContext context);
        bool IsRootFolder { get; }
        IFolder FolderParent { get; }
        string Path { get; }
        IItemIterable<IDocument> GetCheckedOutDocs();
        IItemIterable<IDocument> GetCheckedOutDocs(IOperationContext context);
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
}
