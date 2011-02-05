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
using System.Net;
using DotCMIS.Binding.Impl;
using DotCMIS.Binding.Services;
using DotCMIS.CMISWebServicesReference;

namespace DotCMIS.Binding
{
    public interface ICmisBinding : IDisposable
    {
        IRepositoryService GetRepositoryService();
        INavigationService GetNavigationService();
        IObjectService GetObjectService();
        IVersioningService GetVersioningService();
        IRelationshipService GetRelationshipService();
        IDiscoveryService GetDiscoveryService();
        IMultiFilingService GetMultiFilingService();
        IAclService GetAclService();
        IPolicyService GetPolicyService();
        void ClearAllCaches();
        void ClearRepositoryCache(string repositoryId);
    }

    public interface IBindingSession
    {
        object GetValue(string key);
        object GetValue(string key, object defValue);
        int GetValue(string key, int defValue);
    }

    public abstract class AbstractAuthenticationProvider
    {
        public IBindingSession Session { get; set; }

        public abstract void Authenticate(object connection);

        public string GetUser()
        {
            return Session.GetValue(SessionParameter.User) as string;
        }

        public string GetPassword()
        {
            return Session.GetValue(SessionParameter.Password) as string;
        }
    }

    public class StandardAuthenticationProvider : AbstractAuthenticationProvider
    {
        public override void Authenticate(object connection)
        {
            string user = GetUser();
            string password = GetPassword();
            if (user == null || password == null)
            {
                return;
            }

            if (connection is RepositoryServicePortClient)
            {
                ((RepositoryServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((RepositoryServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is NavigationServicePortClient)
            {
                ((NavigationServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((NavigationServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is ObjectServicePortClient)
            {
                ((ObjectServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((ObjectServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is VersioningServicePortClient)
            {
                ((VersioningServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((VersioningServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is DiscoveryServicePortClient)
            {
                ((DiscoveryServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((DiscoveryServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is RelationshipServicePortClient)
            {
                ((RelationshipServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((RelationshipServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is MultiFilingServicePortClient)
            {
                ((MultiFilingServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((MultiFilingServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is PolicyServicePortClient)
            {
                ((PolicyServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((PolicyServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is ACLServicePortClient)
            {
                ((ACLServicePortClient)connection).ClientCredentials.UserName.UserName = user;
                ((ACLServicePortClient)connection).ClientCredentials.UserName.Password = password;
            }
            else if (connection is WebRequest)
            {
                ((WebRequest)connection).Credentials = new NetworkCredential(user, password);
            }
        }
    }

    public class CmisBindingFactory
    {
        // Default CMIS AtomPub binding SPI implementation
        public const string BindingSpiAtomPub = "DotCMIS.Binding.AtomPub.CmisAtomPubSpi";
        // Default CMIS Web Services binding SPI implementation
        public const string BindingSpiWebServices = "DotCMIS.Binding.WebServices.CmisWebServicesSpi";

        public const string StandardAuthenticationProviderClass = "DotCMIS.Binding.StandardAuthenticationProvider";

        private IDictionary<string, string> defaults;

        private CmisBindingFactory()
        {
            defaults = CreateNewDefaultParameters();
        }

        public static CmisBindingFactory NewInstance()
        {
            return new CmisBindingFactory();
        }

        public IDictionary<string, string> GetDefaultSessionParameters()
        {
            return defaults;
        }

        public void SetDefaultSessionParameters(IDictionary<string, string> sessionParameters)
        {
            if (sessionParameters == null)
            {
                defaults = CreateNewDefaultParameters();
            }
            else
            {
                defaults = sessionParameters;
            }
        }

        public ICmisBinding CreateCmisBinding(IDictionary<string, string> sessionParameters)
        {
            CheckSessionParameters(sessionParameters, true);
            AddDefaultParameters(sessionParameters);

            return new CmisBinding(sessionParameters);
        }

        public ICmisBinding CreateCmisAtomPubBinding(IDictionary<string, string> sessionParameters)
        {
            CheckSessionParameters(sessionParameters, false);
            sessionParameters[SessionParameter.BindingSpiClass] = BindingSpiAtomPub;
            if (!sessionParameters.ContainsKey(SessionParameter.AuthenticationProviderClass))
            {
                sessionParameters[SessionParameter.AuthenticationProviderClass] = StandardAuthenticationProviderClass;
            }

            AddDefaultParameters(sessionParameters);

            Check(sessionParameters, SessionParameter.AtomPubUrl);

            return new CmisBinding(sessionParameters);
        }

        public ICmisBinding CreateCmisWebServicesBinding(IDictionary<string, string> sessionParameters)
        {
            CheckSessionParameters(sessionParameters, false);
            sessionParameters[SessionParameter.BindingSpiClass] = BindingSpiWebServices;
            if (!sessionParameters.ContainsKey(SessionParameter.AuthenticationProviderClass))
            {
                sessionParameters[SessionParameter.AuthenticationProviderClass] = StandardAuthenticationProviderClass;
            }

            AddDefaultParameters(sessionParameters);

            Check(sessionParameters, SessionParameter.WebServicesAclService);
            Check(sessionParameters, SessionParameter.WebServicesDiscoveryService);
            Check(sessionParameters, SessionParameter.WebServicesMultifilingService);
            Check(sessionParameters, SessionParameter.WebServicesNavigationService);
            Check(sessionParameters, SessionParameter.WebServicesObjectService);
            Check(sessionParameters, SessionParameter.WebServicesPolicyService);
            Check(sessionParameters, SessionParameter.WebServicesRelationshipService);
            Check(sessionParameters, SessionParameter.WebServicesRepositoryService);
            Check(sessionParameters, SessionParameter.WebServicesVersioningService);

            return new CmisBinding(sessionParameters);
        }

        // ---- internals ----

        private void CheckSessionParameters(IDictionary<string, string> sessionParameters, bool mustContainSpi)
        {
            // don't accept null
            if (sessionParameters == null)
            {
                throw new ArgumentNullException("sessionParameters");
            }

            // check binding entry
            if (mustContainSpi)
            {
                string spiClass;

                if (sessionParameters.TryGetValue(SessionParameter.BindingSpiClass, out spiClass))
                {
                    throw new ArgumentException("SPI class entry (" + SessionParameter.BindingSpiClass + ") is missing!");
                }

                if ((spiClass == null) || (spiClass.Trim().Length == 0))
                {
                    throw new ArgumentException("SPI class entry (" + SessionParameter.BindingSpiClass + ") is invalid!");
                }
            }
        }

        private void Check(IDictionary<string, string> sessionParameters, String parameter)
        {
            if (!sessionParameters.ContainsKey(parameter))
            {
                throw new ArgumentException("Parameter '" + parameter + "' is missing!");
            }
        }

        private void AddDefaultParameters(IDictionary<string, string> sessionParameters)
        {
            foreach (string key in defaults.Keys)
            {
                if (!sessionParameters.ContainsKey(key))
                {
                    sessionParameters[key] = defaults[key];
                }
            }
        }

        private IDictionary<string, string> CreateNewDefaultParameters()
        {
            IDictionary<string, string> result = new Dictionary<string, string>();

            result[SessionParameter.CacheSizeRepositories] = "10";
            result[SessionParameter.CacheSizeTypes] = "100";
            result[SessionParameter.CacheSizeLinks] = "400";

            return result;
        }
    }
}
