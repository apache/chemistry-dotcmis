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
using System.Threading;
using DotCMIS.Binding;
using DotCMIS.Client;
using DotCMIS.Data;
using DotCMIS.Data.Extensions;
using DotCMIS.Enums;
using DotCMIS.Exceptions;

namespace DotCMIS.Client
{
    internal abstract class AbstractCmisObject : ICmisObject
    {
        protected ISession Session { get; private set; }
        protected string RepositoryId { get { return Session.RepositoryInfo.Id; } }
        protected ICmisBinding Binding { get { return Session.Binding; } }

        private IObjectType objectType;
        protected IObjectType ObjectType
        {
            get
            {
                Lock();
                try
                {
                    return objectType;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        protected string ObjectId
        {
            get
            {
                string objectId = Id;
                if (objectId == null)
                {
                    throw new CmisRuntimeException("Object Id is unknown!");
                }

                return objectId;
            }
        }

        protected IOperationContext CreationContext { get; private set; }

        private IDictionary<string, IProperty> properties;
        private IAllowableActions allowableActions;
        private IList<IRendition> renditions;
        private IAcl acl;
        private IList<IPolicy> policies;
        private IList<IRelationship> relationships;
        private IDictionary<ExtensionLevel, IList<ICmisExtensionElement>> extensions;


        private object objectLock = new object();

        protected void initialize(ISession session, IObjectType objectType, IObjectData objectData, IOperationContext context)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            if (objectType == null)
            {
                throw new ArgumentNullException("objectType");
            }

            if (objectType.PropertyDefintions == null || objectType.PropertyDefintions.Count < 9)
            {
                // there must be at least the 9 standard properties that all objects have
                throw new ArgumentException("Object type must have property defintions!");
            }

            this.Session = session;
            this.objectType = objectType;
            this.extensions = new Dictionary<ExtensionLevel, IList<ICmisExtensionElement>>();
            this.CreationContext = new OperationContext(context);
            this.RefreshTimestamp = DateTime.UtcNow;


            IObjectFactory of = Session.ObjectFactory;

            if (objectData != null)
            {
                // handle properties
                if (objectData.Properties != null)
                {
                    properties = of.ConvertProperties(objectType, objectData.Properties);
                    extensions[ExtensionLevel.Properties] = objectData.Properties.Extensions;
                }

                // handle allowable actions
                if (objectData.AllowableActions != null)
                {
                    allowableActions = objectData.AllowableActions;
                    extensions[ExtensionLevel.AllowableActions] = objectData.AllowableActions.Extensions;
                }

                // handle renditions
                if (objectData.Renditions != null)
                {
                    renditions = new List<IRendition>();
                    foreach (IRenditionData rd in objectData.Renditions)
                    {
                        renditions.Add(of.ConvertRendition(Id, rd));
                    }
                }

                // handle ACL
                if (objectData.Acl != null)
                {
                    acl = objectData.Acl;
                    extensions[ExtensionLevel.Acl] = objectData.Acl.Extensions;
                }

                // handle policies
                if (objectData.PolicyIds != null && objectData.PolicyIds.PolicyIds != null)
                {
                    policies = new List<IPolicy>();
                    foreach (string pid in objectData.PolicyIds.PolicyIds)
                    {
                        ICmisObject policy = Session.GetObject(Session.CreateObjectId(pid));
                        if (policy is IPolicy)
                        {
                            policies.Add((IPolicy)policy);
                        }
                    }
                    extensions[ExtensionLevel.Policies] = objectData.PolicyIds.Extensions;
                }

                // handle relationships
                if (objectData.Relationships != null)
                {
                    relationships = new List<IRelationship>();
                    foreach (IObjectData rod in objectData.Relationships)
                    {
                        ICmisObject relationship = of.ConvertObject(rod, CreationContext);
                        if (relationship is IRelationship)
                        {
                            relationships.Add((IRelationship)relationship);
                        }
                    }
                }

                extensions[ExtensionLevel.Object] = objectData.Extensions;
            }
        }

        protected string GetPropertyQueryName(string propertyId)
        {
            Lock();
            try
            {
                IPropertyDefinition propDef = objectType[propertyId];
                if (propDef == null)
                {
                    return null;
                }

                return propDef.QueryName;
            }
            finally
            {
                Unlock();
            }
        }

        // --- object ---

        public void Delete(bool allVersions)
        {
            Lock();
            try
            {
                Binding.GetObjectService().DeleteObject(RepositoryId, ObjectId, allVersions, null);
            }
            finally
            {
                Unlock();
            }
        }

        public ICmisObject UpdateProperties(IDictionary<string, object> properties)
        {
            IObjectId objectId = UpdateProperties(properties, true);
            if (objectId == null)
            {
                return null;
            }

            if (ObjectId != objectId.Id)
            {
                return Session.GetObject(objectId, CreationContext);
            }

            return this;
        }

        public IObjectId UpdateProperties(IDictionary<String, object> properties, bool refresh)
        {
            if (properties == null || properties.Count == 0)
            {
                throw new ArgumentException("Properties must not be empty!");
            }

            string newObjectId = null;

            Lock();
            try
            {
                string objectId = ObjectId;
                string changeToken = ChangeToken;

                HashSet<Updatability> updatebility = new HashSet<Updatability>();
                updatebility.Add(Updatability.ReadWrite);

                // check if checked out
                bool? isCheckedOut = GetPropertyValue(PropertyIds.IsVersionSeriesCheckedOut) as bool?;
                if (isCheckedOut.HasValue && isCheckedOut.Value)
                {
                    updatebility.Add(Updatability.WhenCheckedOut);
                }

                // it's time to update
                Binding.GetObjectService().UpdateProperties(RepositoryId, ref objectId, ref changeToken,
                        Session.ObjectFactory.ConvertProperties(properties, this.objectType, updatebility), null);

                newObjectId = objectId;
            }
            finally
            {
                Unlock();
            }

            if (refresh)
            {
                Refresh();
            }

            if (newObjectId == null)
            {
                return null;
            }

            return Session.CreateObjectId(newObjectId);
        }

        // --- properties ---

        public IObjectType BaseType { get { return Session.GetTypeDefinition(BaseTypeId.GetCmisValue()); } }

        public BaseTypeId BaseTypeId
        {
            get
            {
                string baseType = GetPropertyValue(PropertyIds.BaseTypeId) as string;
                if (baseType == null) { throw new CmisRuntimeException("Base type not set!"); }

                return baseType.GetCmisEnum<BaseTypeId>();
            }
        }

        public string Id { get { return GetPropertyValue(PropertyIds.ObjectId) as string; } }

        public string Name { get { return GetPropertyValue(PropertyIds.Name) as string; } }

        public string CreatedBy { get { return GetPropertyValue(PropertyIds.CreatedBy) as string; } }

        public DateTime? CreationDate { get { return GetPropertyValue(PropertyIds.CreationDate) as DateTime?; } }

        public string LastModifiedBy { get { return GetPropertyValue(PropertyIds.LastModifiedBy) as string; } }

        public DateTime? LastModificationDate { get { return GetPropertyValue(PropertyIds.LastModificationDate) as DateTime?; } }

        public string ChangeToken { get { return GetPropertyValue(PropertyIds.ChangeToken) as string; } }

        public IObjectType Type { get { return ObjectType; } }

        public IList<IProperty> Properties
        {
            get
            {
                Lock();
                try
                {
                    return new List<IProperty>(properties.Values);
                }
                finally
                {
                    Unlock();
                }
            }
        }

        public IProperty this[string propertyId]
        {
            get
            {
                Lock();
                try
                {
                    IProperty property;
                    if (properties.TryGetValue(propertyId, out property))
                    {
                        return property;
                    }
                    return null;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        public object GetPropertyValue(string propertyId)
        {
            IProperty property = this[propertyId];
            if (property == null) { return null; }

            return property.Value;
        }

        // --- allowable actions ---

        public IAllowableActions AllowableActions
        {
            get
            {
                Lock();
                try
                {
                    return allowableActions;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        // --- renditions ---

        public IList<IRendition> Renditions
        {
            get
            {
                Lock();
                try
                {
                    return renditions;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        // --- ACL ---

        public IAcl getAcl(bool onlyBasicPermissions)
        {
            return Binding.GetAclService().GetAcl(RepositoryId, ObjectId, onlyBasicPermissions, null);
        }

        public IAcl ApplyAcl(IList<IAce> addAces, IList<IAce> removeAces, AclPropagation? aclPropagation)
        {
            IAcl result = Session.ApplyAcl(this, addAces, removeAces, aclPropagation);

            Refresh();

            return result;
        }

        public IAcl AddAcl(IList<IAce> addAces, AclPropagation? aclPropagation)
        {
            return ApplyAcl(addAces, null, aclPropagation);
        }

        public IAcl RemoveAcl(IList<IAce> removeAces, AclPropagation? aclPropagation)
        {
            return ApplyAcl(null, removeAces, aclPropagation);
        }

        public IAcl Acl
        {
            get
            {
                Lock();
                try
                {
                    return acl;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        // --- policies ---

        public void ApplyPolicy(IObjectId policyId)
        {
            Lock();
            try
            {
                Session.ApplyPolicy(this, policyId);
            }
            finally
            {
                Unlock();
            }

            Refresh();
        }

        public void RemovePolicy(IObjectId policyId)
        {
            Lock();
            try
            {
                Session.RemovePolicy(this, policyId);
            }
            finally
            {
                Unlock();
            }

            Refresh();
        }

        public IList<IPolicy> Policies
        {
            get
            {
                Lock();
                try
                {
                    return policies;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        // --- relationships ---

        public IList<IRelationship> Relationships
        {
            get
            {
                Lock();
                try
                {
                    return relationships;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        // --- extensions ---

        public IList<ICmisExtensionElement> GetExtensions(ExtensionLevel level)
        {
            IList<ICmisExtensionElement> ext;
            if (extensions.TryGetValue(level, out ext))
            {
                return ext;
            }

            return null;
        }

        // --- other ---

        public DateTime RefreshTimestamp { get; private set; }

        public void Refresh()
        {
            Lock();
            try
            {
                IOperationContext oc = CreationContext;

                // get the latest data from the repository
                IObjectData objectData = Binding.GetObjectService().GetObject(RepositoryId, ObjectId, oc.FilterString, oc.IncludeAllowableActions,
                    oc.IncludeRelationships, oc.RenditionFilterString, oc.IncludePolicies, oc.IncludeAcls, null);

                // reset this object
                initialize(Session, ObjectType, objectData, CreationContext);
            }
            finally
            {
                Unlock();
            }
        }

        public void RefreshIfOld(long durationInMillis)
        {
            Lock();
            try
            {
                if (((DateTime.UtcNow - RefreshTimestamp).Ticks / 10000) > durationInMillis)
                {
                    Refresh();
                }
            }
            finally
            {
                Unlock();
            }
        }

        protected void Lock()
        {
            Monitor.Enter(objectLock);
        }

        protected void Unlock()
        {
            Monitor.Exit(objectLock);
        }
    }
}
