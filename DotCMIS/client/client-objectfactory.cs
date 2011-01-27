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
using System.IO;
using DotCMIS.Enums;
using DotCMIS.Exceptions;

namespace DotCMIS.Client
{
    public class ObjectFactory : IObjectFactory
    {
        private ISession session;

        public void Initialize(ISession session, IDictionary<string, string> parameters)
        {
            this.session = session;
        }

        // ACL and ACE
        public IAcl ConvertAces(IList<IAce> aces) { return null; }
        public IAcl CreateAcl(IList<IAce> aces) { return null; }
        public IAce CreateAce(string principal, List<string> permissions) { return null; }

        // policies
        public IList<string> ConvertPolicies(IList<IPolicy> policies) { return null; }

        // renditions
        public IRendition ConvertRendition(string objectId, IRenditionData rendition) { return null; }

        // content stream
        public IContentStream CreateContentStream(string filename, long length, string mimetype, Stream stream) { return null; }
        public IContentStream ConvertContentStream(IContentStream contentStream) { return null; }

        // types
        public IObjectType ConvertTypeDefinition(ITypeDefinition typeDefinition)
        {
            if (typeDefinition is IDocumentTypeDefinition)
            {
                return new DocumentType(session, (IDocumentTypeDefinition)typeDefinition);
            }
            else if (typeDefinition is IFolderTypeDefinition)
            {
                return new FolderType(session, (IFolderTypeDefinition)typeDefinition);
            }
            else if (typeDefinition is IRelationshipTypeDefinition)
            {
                return new RelationshipType(session, (IRelationshipTypeDefinition)typeDefinition);
            }
            else if (typeDefinition is IPolicyTypeDefinition)
            {
                return new PolicyType(session, (IPolicyTypeDefinition)typeDefinition);
            }
            else
            {
                throw new CmisRuntimeException("Unknown base type!");
            }
        }

        public IObjectType GetTypeFromObjectData(IObjectData objectData) { return null; }

        // properties
        public IProperty CreateProperty(IPropertyDefinition type, IList<object> values) { return null; }
        public IDictionary<string, IProperty> ConvertProperties(IObjectType objectType, IProperties properties) { return null; }
        public IProperties ConvertProperties(IDictionary<string, object> properties, IObjectType type, HashSet<Updatability> updatabilityFilter) { return null; }
        public IList<IPropertyData> ConvertQueryProperties(IProperties properties) { return null; }

        // objects
        public ICmisObject ConvertObject(IObjectData objectData, IOperationContext context) { return null; }
        public IQueryResult ConvertQueryResult(IObjectData objectData) { return null; }
        public IChangeEvent ConvertChangeEvent(IObjectData objectData) { return null; }
        public IChangeEvents ConvertChangeEvents(String changeLogToken, IObjectList objectList) { return null; }
    }
}
